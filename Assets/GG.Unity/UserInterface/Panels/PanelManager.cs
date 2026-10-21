using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    public sealed class PanelManager : IDisposable
    {
        private const string ContainerName = "panel-container";
        private readonly VisualElement container;
        private readonly Dictionary<PanelDefinition, Panel> singletons = new();
        private readonly List<Panel> stack = new();

        public event Action<Panel> PanelOpened;
        public event Action<Panel> PanelClosed;

        public bool IsAttached => container.parent != null;

        public PanelManager()
        {
            container = new VisualElement { name = ContainerName, pickingMode = PickingMode.Ignore };
            Panel.Stretch(container);
            container.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        /// <summary>Call whenever the PanelRenderer provides a (new) root.</summary>
        public void Attach(VisualElement root)
        {
            container.RemoveFromHierarchy(); // no-op if not parented; handles re-attach to a new root
            root.Add(container);
        }

        public void Detach() => container.RemoveFromHierarchy();

        public Panel Open(PanelDefinition def, Action<Panel> onOpened = null)
        {
            if (!IsAttached)
            {
                Debug.LogWarning($"Cannot open '{def.name}': PanelManager has no root yet.");
                return null;
            }
            if (def.Singleton && singletons.TryGetValue(def, out var existing))
            {
                BringToFront(existing);
                return existing;
            }

            var panel = new Panel(def, this)
            {
                PreviousFocus = container.panel?.focusController?.focusedElement
            };

            container.Add(panel.Root);
            stack.Add(panel);
            if (def.Singleton)
                singletons[def] = panel;

            onOpened?.Invoke(panel);
            PanelOpened?.Invoke(panel);

            // Focus needs the element to be attached and laid out, so defer it.
            panel.Root.schedule.Execute(() => FocusFirst(panel.Root));

            return panel;
        }

        public void Close(Panel panel)
        {
            if (panel == null || !stack.Remove(panel))
                return;

            if (panel.Definition.Singleton)
                singletons.Remove(panel.Definition);

            panel.Root.RemoveFromHierarchy();

            // Restore focus only if the previous element is still in a panel.
            if (panel.PreviousFocus is VisualElement prev && prev.panel != null)
                prev.Focus();

            PanelClosed?.Invoke(panel);
        }

        public bool CloseTopPanel()
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                if (stack[i].Definition.CloseOnBack)
                {
                    Close(stack[i]);
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            container.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            container.RemoveFromHierarchy();
            stack.Clear();
            singletons.Clear();
        }

        private void BringToFront(Panel panel)
        {
            panel.Root.BringToFront();
            stack.Remove(panel);   // keep the logical stack in sync with draw order
            stack.Add(panel);
        }

        private void OnNavigationCancel(NavigationCancelEvent evt)
        {
            if (CloseTopPanel())
                evt.StopPropagation();
        }

        private static void FocusFirst(VisualElement host)
        {
            host.Query<VisualElement>()
                .Where(e => e.focusable && e.canGrabFocus)
                .First()?
                .Focus();
        }
    }
}
