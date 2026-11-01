using GG.Unity.UserInterface.Panels;
using UnityEngine;

namespace GG.Unity.Advertisement.UIToolkit
{
    /// <summary>
    /// The ad consent popup. Closes with <c>true</c> (accept personalised ads), <c>false</c>
    /// (decline), or <c>null</c> if dismissed some other way. Keep "Close On Back" and
    /// "Close On Backdrop Click" off so the player has to make a choice.
    /// </summary>
    [CreateAssetMenu(fileName = "ConsentPanel", menuName = "GG.Unity/Advertisement/Consent Panel")]
    public class ConsentPanelDefinition : PanelDefinition
    {
        [Header("Text overrides (leave empty to use the text in the UXML)")]
        [SerializeField] private string title;
        [SerializeField, TextArea(3, 10)] private string message;
        [SerializeField] private string acceptText;
        [SerializeField] private string declineText;
        [SerializeField] private string privacyPolicyText;

        [Header("Privacy policy")]
        [SerializeField, Tooltip("Opened in the browser. Leave empty to hide the link.")]
        private string privacyPolicyUrl = "https://example.com/privacy";

        public string Title => title;
        public string Message => message;
        public string AcceptText => acceptText;
        public string DeclineText => declineText;
        public string PrivacyPolicyText => privacyPolicyText;
        public string PrivacyPolicyUrl => privacyPolicyUrl;

        public override IPanelController CreateController() => new ConsentPanelController(this);

        protected override void OnValidate()
        {
            base.OnValidate();

            if (CloseOnBack || CloseOnBackdropClick)
                Debug.LogWarning($"ConsentPanelDefinition '{name}': turn off Close On Back and Close On Backdrop Click " +
                                 "so the player makes an explicit choice. Dismissing counts as 'no choice' and they'll be asked again next launch.", this);

            if (!Modal)
                Debug.LogWarning($"ConsentPanelDefinition '{name}' should be modal.", this);

            if (string.IsNullOrWhiteSpace(privacyPolicyUrl) || privacyPolicyUrl.Contains("example.com"))
                Debug.LogWarning($"ConsentPanelDefinition '{name}' needs your real privacy policy URL. Stores and GDPR expect one.", this);
        }
    }
}
