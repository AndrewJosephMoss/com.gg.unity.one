using GG.Unity.UserInterface.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.Advertisement.UIToolkit
{
    /// <summary>
    /// Wires the consent popup. Expected element names in the UXML:
    /// Label "title", Label "body", Button "accept", Button "decline", Button "privacy-policy".
    /// Text set on the <see cref="ConsentPanelDefinition"/> overrides the UXML; empty fields keep it.
    /// </summary>
    internal sealed class ConsentPanelController : IPanelController
    {
        private readonly ConsentPanelDefinition definition;

        private Button accept;
        private Button decline;
        private Button privacyPolicy;
        private Panel panel;

        public ConsentPanelController(ConsentPanelDefinition definition) => this.definition = definition;

        public void OnOpen(Panel panel)
        {
            this.panel = panel;
            var root = panel.Root;

            Override(root.Q<Label>("title"), definition.Title);
            Override(root.Q<Label>("body"), definition.Message);

            accept = root.Q<Button>("accept");
            decline = root.Q<Button>("decline");
            privacyPolicy = root.Q<Button>("privacy-policy");

            if (accept == null || decline == null)
                Debug.LogError($"Consent panel '{definition.name}' needs Buttons named 'accept' and 'decline'.", definition);

            if (accept != null)
            {
                Override(accept, definition.AcceptText);
                accept.clicked += OnAccept;
            }
            if (decline != null)
            {
                Override(decline, definition.DeclineText);
                decline.clicked += OnDecline;
            }
            if (privacyPolicy != null)
            {
                bool hasUrl = !string.IsNullOrWhiteSpace(definition.PrivacyPolicyUrl);
                Override(privacyPolicy, definition.PrivacyPolicyText);
                privacyPolicy.style.display = hasUrl ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasUrl)
                    privacyPolicy.clicked += OnPrivacyPolicy;
            }
        }

        public void OnClose(Panel panel)
        {
            if (accept != null) accept.clicked -= OnAccept;
            if (decline != null) decline.clicked -= OnDecline;
            if (privacyPolicy != null) privacyPolicy.clicked -= OnPrivacyPolicy;
            this.panel = null;
        }

        private void OnAccept() => panel?.Close(true);

        private void OnDecline() => panel?.Close(false);

        private void OnPrivacyPolicy() => Application.OpenURL(definition.PrivacyPolicyUrl);

        /// <summary>Replace the element's UXML text only when the asset provides some.</summary>
        private static void Override(TextElement element, string text)
        {
            if (element != null && !string.IsNullOrEmpty(text))
                element.text = text;
        }
    }
}
