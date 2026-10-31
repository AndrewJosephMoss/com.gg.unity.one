using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    /// <summary>
    /// Owns the panel stack, layering, modality and focus.
    /// </summary>
    /// <remarks>
    /// Main thread only. All bookkeeping is final before any callback runs, so controllers and
    /// event handlers may open or close panels (including their own) from inside callbacks.
    /// Exceptions thrown by callbacks are logged and never corrupt the stack.
    /// </remarks>
    public sealed class PanelManagingService : IPanelService, IDisposable
    {
        private const string ContainerName = "panel-container";
        private const string LayerPrefix = "panel-layer--";

        private readonly VisualElement container;
        private readonly Dictionary<PanelLayer, VisualElement> layers = new();
        private readonly Dictionary<PanelDefinition, Panel> singletons = new();
        private readonly List<Panel> stack = new();       // bottom → top, by layer then open order
        private readonly HashSet<Panel> closing = new();  // logically closed, transition still playing

        private VisualElement attachedRoot;
        private bool disposed;

        public event Action<Panel> PanelOpened;
        public event Action<Panel> PanelClosed;

        public IReadOnlyList<Panel> OpenPanels => stack;
        public Panel Top => stack.Count > 0 ? stack[^1] : null;
        public bool IsAttached => attachedRoot != null && container.parent == attachedRoot;

        private VisualElement FocusedElement =>
            container.panel?.focusController?.focusedElement as VisualElement;

        public PanelManagingService(StyleSheet styleSheet = null)
        {
            container = new VisualElement { name = ContainerName, pickingMode = PickingMode.Ignore };
            Panel.Stretch(container);

            if (styleSheet != null)
                container.styleSheets.Add(styleSheet);

            foreach (var layer in Enum.GetValues(typeof(PanelLayer)).Cast<PanelLayer>().OrderBy(l => (int)l))
            {
                string layerName = LayerPrefix + layer.ToString().ToLowerInvariant();
                var element = new VisualElement { name = layerName, pickingMode = PickingMode.Ignore };
                element.AddToClassList(layerName);
                Panel.Stretch(element);
                container.Add(element);
                layers[layer] = element;
            }
        }

        #region Attachment

        /// <summary>Call whenever the PanelRenderer provides a (new) root.</summary>
        public void Attach(VisualElement root)
        {
            ThrowIfDisposed();
            if (root == null)
            {
                Debug.LogError("PanelManagingService.Attach: root is null.");
                return;
            }
            if (IsAttached && attachedRoot == root)
                return;

            Detach();
            attachedRoot = root;
            root.Add(container);
            root.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
        }

        public void Detach()
        {
            if (attachedRoot != null)
            {
                attachedRoot.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
                attachedRoot = null;
            }
            container.RemoveFromHierarchy();
        }

        #endregion

        #region Open / Close

        public Panel Open(PanelDefinition definition, object args = null)
        {
            ThrowIfDisposed();

            if (definition == null)
            {
                Debug.LogError("Cannot open panel: definition is null.");
                return null;
            }
            if (definition.Tree == null)
            {
                Debug.LogError($"Cannot open '{definition.name}': no VisualTreeAsset assigned.", definition);
                return null;
            }

            if (definition.Singleton && singletons.TryGetValue(definition, out var existing))
            {
                BringToFront(existing);
                return existing;
            }

            Panel panel;
            try
            {
                panel = new Panel(definition, this, args);
            }
            catch (Exception e)
            {
                Debug.LogException(e, definition);
                return null;
            }

            panel.PreviousFocus = FocusedElement;
            Insert(panel);
            GetLayerElement(definition.Layer).Add(panel.Root);
            if (definition.Singleton)
                singletons[definition] = panel;

            try
            {
                panel.Controller = definition.CreateController();
                if (panel.Controller == null && args != null)
                    panel.Root.dataSource = args;
                panel.Controller?.OnOpen(panel);
            }
            catch (Exception e)
            {
                Debug.LogException(e, definition);
            }

            if (!panel.IsOpen)
                return panel; // closed by its own controller

            SafeInvoke(PanelOpened, panel);
            if (!panel.IsOpen)
                return panel;

            // Runs on the next scheduler tick once attached: styles are resolved, so the
            // visible class triggers USS transitions, and canGrabFocus is reliable.
            panel.Root.schedule.Execute(() =>
            {
                if (!panel.IsOpen)
                    return;

                panel.Root.AddToClassList(Panel.VisibleClass);
                panel.State = PanelState.Open;

                if (panel.Definition.FocusOnOpen && IsFocusAllowed(panel.Root))
                    FocusFirst(panel.Root);
            });

            return panel;
        }

        public async Task<T> OpenAsync<T>(PanelDefinition definition, object args = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var panel = Open(definition, args);
            if (panel == null)
                return default;

            object result;
            // useSynchronizationContext: cancellation closes the panel on the Unity main thread.
            using (cancellationToken.Register(() => Close(panel), useSynchronizationContext: true))
                result = await panel.Completion;

            cancellationToken.ThrowIfCancellationRequested();
            return result is T typed ? typed : default;
        }

        public void Close(Panel panel, object result = null)
        {
            if (panel == null || !panel.IsOpen)
                return;

            int index = stack.IndexOf(panel);
            if (index < 0)
                return;

            bool wasTop = index == stack.Count - 1;
            bool hadFocus = IsInside(panel.Root, FocusedElement);

            stack.RemoveAt(index);
            var definition = panel.Definition;
            if (definition.Singleton && singletons.TryGetValue(definition, out var single) && single == panel)
                singletons.Remove(definition);
            panel.BeginClose(result);

            // Stop the panel taking input while it animates out.
            panel.Root.pickingMode = PickingMode.Ignore;
            panel.Root.SetEnabled(false);
            panel.Root.RemoveFromClassList(Panel.VisibleClass);

            if (hadFocus || FocusedElement == null)
                RestoreFocus(panel, wasTop);

            try
            {
                panel.Controller?.OnClose(panel);
            }
            catch (Exception e)
            {
                Debug.LogException(e, definition);
            }

            panel.NotifyClosed();
            SafeInvoke(PanelClosed, panel);

            // Remove after the close transition. The scheduler is paused while detached,
            // so release immediately in that case.
            if (definition.CloseDuration <= 0f || !IsAttached)
            {
                Release(panel);
            }
            else
            {
                closing.Add(panel);
                panel.Root.schedule
                    .Execute(() => Release(panel))
                    .ExecuteLater((long)(definition.CloseDuration * 1000f));
            }
        }

        public bool CloseTopPanel()
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                var panel = stack[i];
                if (panel.Definition.CloseOnBack)
                {
                    Close(panel);
                    return true;
                }
                if (panel.Definition.Modal)
                    return true; // a modal that ignores Back still consumes it
            }
            return false;
        }

        /// <summary>Closes every open panel, top first. Panels opened by close callbacks stay open.</summary>
        public void CloseAll()
        {
            var snapshot = stack.ToArray();
            for (int i = snapshot.Length - 1; i >= 0; i--)
                Close(snapshot[i]);
        }

        #endregion

        #region Queries

        public bool IsOpen(PanelDefinition definition) => TryGetOpen(definition, out _);

        public bool TryGetOpen(PanelDefinition definition, out Panel panel)
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                if (stack[i].Definition == definition)
                {
                    panel = stack[i];
                    return true;
                }
            }
            panel = null;
            return false;
        }

        #endregion

        public void Dispose()
        {
            if (disposed)
                return;

            CloseAll(); // fires close callbacks and completes pending OpenAsync calls
            disposed = true;

            foreach (var panel in closing.ToArray())
                Release(panel);
            closing.Clear();

            Detach();
            stack.Clear();
            singletons.Clear();
            PanelOpened = null;
            PanelClosed = null;
        }

        #region Internals

        private void Insert(Panel panel)
        {
            int layer = (int)panel.Definition.Layer;
            int index = stack.Count;
            while (index > 0 && (int)stack[index - 1].Definition.Layer > layer)
                index--;
            stack.Insert(index, panel);
        }

        private void BringToFront(Panel panel)
        {
            stack.Remove(panel);
            Insert(panel);              // keep the logical stack in sync with draw order
            panel.Root.BringToFront();  // within its layer element

            if (panel.State == PanelState.Open && panel.Definition.FocusOnOpen && IsFocusAllowed(panel.Root))
                FocusFirst(panel.Root);
        }

        private void Release(Panel panel)
        {
            if (panel.State == PanelState.Closed)
                return;

            closing.Remove(panel);
            panel.Release();
        }

        private VisualElement GetLayerElement(PanelLayer layer)
        {
            if (layers.TryGetValue(layer, out var element))
                return element;

            Debug.LogWarning($"Unknown PanelLayer value {(int)layer}; using {PanelLayer.Window}.");
            return layers[PanelLayer.Window];
        }

        private int TopModalIndex() => stack.FindLastIndex(p => p.Definition.Modal);

        /// <summary>Focus is allowed in the topmost modal and anything stacked above it (or anywhere if no modal).</summary>
        private bool IsFocusAllowed(VisualElement target)
        {
            if (target == null)
                return true;

            int modalIndex = TopModalIndex();
            if (modalIndex < 0)
                return true;

            for (int i = modalIndex; i < stack.Count; i++)
                if (IsInside(stack[i].Root, target))
                    return true;

            return false;
        }

        private void RestoreFocus(Panel closed, bool wasTop)
        {
            if (wasTop && closed.PreviousFocus is VisualElement previous && CanFocus(previous))
            {
                previous.Focus();
                return;
            }

            if (FocusTopmost())
                return;

            // Nothing to move to: don't leave focus stranded inside a closing panel.
            var focused = FocusedElement;
            if (IsInside(closed.Root, focused))
                focused.Blur();
        }

        private bool CanFocus(VisualElement element) =>
            element.panel != null && element.canGrabFocus && IsFocusAllowed(element);

        /// <summary>Focus the topmost panel that can take focus, never going below the top modal.</summary>
        private bool FocusTopmost()
        {
            int floor = Math.Max(0, TopModalIndex());
            for (int i = stack.Count - 1; i >= floor; i--)
            {
                if (stack[i].Definition.FocusOnOpen && FocusFirst(stack[i].Root))
                    return true;
            }
            return false;
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            if (evt.target is not VisualElement target || IsFocusAllowed(target))
                return;

            // Focus trap: keyboard/gamepad navigation tried to leave the modal.
            // Don't change focus inside the focus event itself; correct it on the next tick.
            attachedRoot?.schedule.Execute(() =>
            {
                var focused = FocusedElement;
                if (focused == null || IsFocusAllowed(focused))
                    return;
                if (!FocusTopmost())
                    focused.Blur();
            });
        }

        private static bool FocusFirst(VisualElement host)
        {
            var initial = host.Q(className: Panel.InitialFocusClass);
            if (initial != null && initial.focusable && initial.canGrabFocus)
            {
                initial.Focus();
                return true;
            }

            var first = host.Query<VisualElement>()
                .Where(e => e.focusable && e.canGrabFocus && e.tabIndex >= 0)
                .First();

            if (first == null)
                return false;

            first.Focus();
            return true;
        }

        private static bool IsInside(VisualElement host, VisualElement element) =>
            element != null && (element == host || host.Contains(element));

        internal static void SafeInvoke(Action<Panel> handler, Panel panel)
        {
            if (handler == null)
                return;

            foreach (var callback in handler.GetInvocationList())
            {
                try
                {
                    ((Action<Panel>)callback)(panel);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(PanelManagingService));
        }

        #endregion
    }
}
