using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    [CreateAssetMenu(fileName = "MessagePanelDefinition", menuName = "GG.Unity/UserInterface/MessagePanelDefinition")]
    public class MessagePanelDefinition : PanelDefinition, IPanelOpenHandler
    {
        private const string BodyName = "body";
        private const string TitleName = "title";

        [SerializeField] private string title;


        [SerializeField, TextArea(2, 6)] private string message;


        public void OnPanelOpened(Panel panel)
        {
            AssignText(panel, BodyName, message);
            AssignText(panel, TitleName, title);
        }

        private static void AssignText(Panel panel, string elementName, string text)
        {
            var label = panel.Root.Q<Label>(elementName);
            if (label == null)
                return;

            bool hasText = !string.IsNullOrEmpty(text);
            label.text = hasText ? text : string.Empty;
            label.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (string.IsNullOrEmpty(message))
                Debug.LogWarning($"{name} MessagePanelDefinition has empty message", this);
        }
    }
}
