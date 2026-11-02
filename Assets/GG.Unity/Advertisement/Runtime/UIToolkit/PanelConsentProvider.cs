using System.Threading;
using System.Threading.Tasks;
using GG.Unity.UserInterface.Panels;
using UnityEngine;

namespace GG.Unity.Advertisement.UIToolkit
{
    /// <summary>
    /// Asks for consent with your own popup, through the panel system.
    /// Not a Google-certified CMP: see the notes on EEA/UK/Swiss traffic.
    /// </summary>
    public class PanelConsentProvider : ConsentProvider
    {
        [SerializeField, Tooltip("Optional. If empty or destroyed (e.g. after a scene change), the PanelManager in the current scene is used.")]
        private PanelManager panelManager;

        [SerializeField] private ConsentPanelDefinition consentPanel;

        public override async Task<bool?> RequestConsentAsync(CancellationToken cancellationToken)
        {
            if (panelManager == null)
                panelManager = FindAnyObjectByType<PanelManager>();

            if (panelManager == null || consentPanel == null)
            {
                Debug.LogError($"{name}:{GetType().Name} needs a PanelManager in the scene and a ConsentPanelDefinition.", this);
                return null;
            }

            object result = await panelManager.OpenPanelAsync<object>(consentPanel, null, cancellationToken);
            return result is bool choice ? choice : (bool?)null;
        }
    }
}
