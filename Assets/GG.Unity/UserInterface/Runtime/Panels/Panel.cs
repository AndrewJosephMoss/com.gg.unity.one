using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    public sealed class Panel
    {
        public const string HostClass = "panel-host";
        public const string ModalClass = "panel-host--modal";
        public const string CloseButtonClass = "panel-close";

        private readonly PanelManager manager;

        public PanelDefinition Definition { get; }

        /// <summary>
        /// The instantiated template (a TemplateContainer)
        /// </summary>
        public VisualElement Root { get; }

        internal Focusable PreviousFocus { get; set; }

        internal Panel(PanelDefinition definition, PanelManager manager)
        {
            Definition = definition;
            this.manager = manager;

            Root = definition.Tree.Instantiate();
            Root.name = $"{definition.name}-host";
            Root.AddToClassList(HostClass);
            Stretch(Root);

            if (definition.Modal)
            {
                Root.AddToClassList(ModalClass);
                Root.pickingMode = PickingMode.Position;  // swallows clicks on empty areas
            }
            else
            {
                Root.pickingMode = PickingMode.Ignore;    // children still receive input
            }

            Root.Query<Button>(className: CloseButtonClass)
                .ForEach(b => b.clicked += Close);
        }

        public void Close() => manager.Close(this);

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
