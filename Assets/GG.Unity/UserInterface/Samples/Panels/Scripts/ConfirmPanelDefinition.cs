using GG.Unity.UserInterface.Panels;
using UnityEngine;

namespace GG.Unity.UserInterface.Samples
{
    /// <summary>
    /// Yes/no dialog. Closes with <c>true</c> (confirm), <c>false</c> (cancel) or <c>null</c>
    /// (Back / backdrop), so <c>OpenAsync&lt;bool&gt;</c> treats anything but Confirm as "no".
    /// </summary>
    [CreateAssetMenu(fileName = "ConfirmPanel", menuName = "GG.Unity/UserInterface/Samples/Confirm Panel")]
    public class ConfirmPanelDefinition : PanelDefinition
    {
        [Header("Defaults (overridable with ConfirmArgs)")]
        [SerializeField] private string title = "Are you sure?";
        [SerializeField, TextArea(2, 6)] private string message;
        [SerializeField] private string confirmText = "Confirm";
        [SerializeField] private string cancelText = "Cancel";

        public string DefaultTitle => title;
        public string DefaultMessage => message;
        public string DefaultConfirmText => confirmText;
        public string DefaultCancelText => cancelText;

        public override IPanelController CreateController() => new ConfirmPanelController(this);
    }
}
