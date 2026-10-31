using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    public enum PanelState
    {
        Opening,  // in the hierarchy, open transition pending
        Open,
        Closing,  // logically closed, close transition playing
        Closed    // removed from the hierarchy
    }

    /// <summary>One open instance of a <see cref="PanelDefinition"/>.</summary>
    public sealed class Panel
    {
        public const string HostClass = "panel-host";
        public const string ModalClass = "panel-host--modal";
        public const string VisibleClass = "panel-host--visible";
        public const string CloseButtonClass = "panel-close";
        public const string InitialFocusClass = "panel-initial-focus";

        private readonly PanelManagingService owner;
        private readonly List<Button> closeButtons = new();
        private readonly TaskCompletionSource<object> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PanelDefinition Definition { get; }

        /// <summary>The service that owns this panel, so controllers can open child panels (dialogs, toasts).</summary>
        public IPanelService Service => owner;

        /// <summary>The instantiated template (a TemplateContainer).</summary>
        public VisualElement Root { get; }

        /// <summary>Runtime arguments passed to Open. Also set as <c>Root.dataSource</c> for runtime bindings.</summary>
        public object Args { get; }

        public IPanelController Controller { get; internal set; }
        public PanelState State { get; internal set; } = PanelState.Opening;

        /// <summary>The value passed to <see cref="Close"/>. Null when closed by Back, a close button or the backdrop.</summary>
        public object Result { get; private set; }

        public bool IsOpen => State == PanelState.Opening || State == PanelState.Open;

        /// <summary>Completes with <see cref="Result"/> when the panel closes (including on service disposal).</summary>
        public Task<object> Completion => completion.Task;

        /// <summary>Raised once when the panel closes.</summary>
        public event Action<Panel> Closed;

        internal Focusable PreviousFocus { get; set; }

        internal Panel(PanelDefinition definition, PanelManagingService owner, object args)
        {
            Definition = definition;
            Args = args;
            this.owner = owner;

            Root = definition.Tree.Instantiate();
            Root.name = $"{definition.name}-host";
            Root.AddToClassList(HostClass);
            Stretch(Root);

            if (args != null)
                Root.dataSource = args;

            if (definition.Modal)
            {
                Root.AddToClassList(ModalClass);
                Root.pickingMode = PickingMode.Position;  // swallows clicks on empty areas

                if (definition.CloseOnBackdropClick)
                    Root.RegisterCallback<PointerDownEvent>(OnBackdropPointerDown);
            }
            else
            {
                Root.pickingMode = PickingMode.Ignore;    // children still receive input
            }

            Root.Query<Button>(className: CloseButtonClass).ForEach(button =>
            {
                button.clicked += OnCloseClicked;
                closeButtons.Add(button);
            });
        }

        public void Close(object result = null) => owner.Close(this, result);

        internal void BeginClose(object result)
        {
            Result = result;
            State = PanelState.Closing;
        }

        internal void NotifyClosed()
        {
            var handler = Closed;
            Closed = null;
            PanelManagingService.SafeInvoke(handler, this);
            completion.TrySetResult(Result);
        }

        internal void Release()
        {
            foreach (var button in closeButtons)
                button.clicked -= OnCloseClicked;
            closeButtons.Clear();

            Root.UnregisterCallback<PointerDownEvent>(OnBackdropPointerDown);
            Root.RemoveFromHierarchy();
            State = PanelState.Closed;
        }

        private void OnCloseClicked() => Close();

        private void OnBackdropPointerDown(PointerDownEvent evt)
        {
            // Only clicks that land on the host itself, not on the panel's content.
            if (evt.target != Root || !IsOpen)
                return;

            evt.StopPropagation();
            Close();
        }

        internal static void Stretch(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
        }
    }
}
