using UnityEngine;

namespace GG.Unity.UserInterface.Panels
{
    /// <summary>
    /// A simple title + body message panel. Default text comes from the asset and can be
    /// overridden per open by passing <see cref="MessageArgs"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "MessagePanelDefinition", menuName = "GG.Unity/UserInterface/MessagePanelDefinition")]
    public class MessagePanelDefinition : PanelDefinition
    {
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 6)] private string message;

        public string DefaultTitle => title;
        public string DefaultMessage => message;

        public override IPanelController CreateController() => new MessagePanelController(this);
    }
}
