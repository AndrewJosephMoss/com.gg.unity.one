using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    /// <summary>Runtime overrides for a <see cref="MessagePanelDefinition"/>. Null fields fall back to the asset.</summary>
    public sealed class MessageArgs
    {
        public string Title { get; set; }
        public string Message { get; set; }

        public MessageArgs(string message = null, string title = null)
        {
            Message = message;
            Title = title;
        }
    }

    internal sealed class MessagePanelController : IPanelController
    {
        private const string BodyName = "body";
        private const string TitleName = "title";

        private readonly MessagePanelDefinition definition;

        public MessagePanelController(MessagePanelDefinition definition) => this.definition = definition;

        public void OnOpen(Panel panel)
        {
            var args = panel.Args as MessageArgs;
            AssignText(panel, TitleName, args?.Title ?? definition.DefaultTitle);
            AssignText(panel, BodyName, args?.Message ?? definition.DefaultMessage);
        }

        public void OnClose(Panel panel) { }

        private static void AssignText(Panel panel, string elementName, string text)
        {
            var label = panel.Root.Q<Label>(elementName);
            if (label == null)
                return;

            bool hasText = !string.IsNullOrEmpty(text);
            label.text = hasText ? text : string.Empty;
            label.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
