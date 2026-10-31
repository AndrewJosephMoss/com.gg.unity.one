using GG.Unity.UserInterface.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Samples
{
    /// <summary>Runtime overrides for a <see cref="ConfirmPanelDefinition"/>. Null fields fall back to the asset.</summary>
    public sealed class ConfirmArgs
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string ConfirmText { get; set; }
        public string CancelText { get; set; }
    }

    internal sealed class ConfirmPanelController : IPanelController
    {
        private readonly ConfirmPanelDefinition definition;

        public ConfirmPanelController(ConfirmPanelDefinition definition) => this.definition = definition;

        public void OnOpen(Panel panel)
        {
            var args = panel.Args as ConfirmArgs;

            SetText(panel.Root.Q<Label>("title"), args?.Title ?? definition.DefaultTitle);
            SetText(panel.Root.Q<Label>("body"), args?.Message ?? definition.DefaultMessage);

            var confirm = panel.Root.Q<Button>("confirm");
            var cancel = panel.Root.Q<Button>("cancel");

            if (confirm != null)
            {
                confirm.text = args?.ConfirmText ?? definition.DefaultConfirmText;
                confirm.clicked += () => panel.Close(true);
            }
            if (cancel != null)
            {
                cancel.text = args?.CancelText ?? definition.DefaultCancelText;
                cancel.clicked += () => panel.Close(false);
            }
        }

        public void OnClose(Panel panel) =>
            Debug.Log($"[Confirm] Closed with result: {panel.Result ?? "null (Back or backdrop)"}");

        private static void SetText(Label label, string text)
        {
            if (label == null)
                return;
            label.text = text ?? string.Empty;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
