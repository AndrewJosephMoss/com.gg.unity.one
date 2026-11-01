using System;
using GG.Unity.UserInterface.Panels;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace GG.Unity.Advertisement.Samples
{
    [RequireComponent(typeof(PanelRenderer))]
    public class AdvertisementSampleController : MonoBehaviour
    {
        [SerializeField] private PanelManager panelManager;

        [Header("Panels")]
        [SerializeField] private MessagePanelDefinition noInternetPanel;
        [SerializeField] private MessagePanelDefinition rewardGrantedPanel;
        [SerializeField] private MessagePanelDefinition rewardedDueToFailedAdAttempsPanel;
        [SerializeField] private MessagePanelDefinition adFailed;

        [Header("Banner Ad Opportunity Dummy Objects")]
        [SerializeField] private BannerAdOpportunity topBannerOpportunity;
        [SerializeField] private BannerAdOpportunity bottomBannerOpportunity;
        [SerializeField] private BannerAdOpportunity bothBannerOpportunity;

        private PanelRenderer panelRenderer;
        private IPanelService service;

        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
            if (panelManager == null)
                TryGetComponent(out panelManager);
        }

        private void OnEnable()
        {
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        private void Start()
        {
            // PanelManager runs at execution order -100, so its service exists by now.
            service = panelManager != null ? panelManager.Service : null;
            if (service == null)
            {
                Debug.LogError("[AdvertisementSample] No PanelManager service found.", this);
                return;
            }
            service.PanelOpened += OnPanelOpened;
            service.PanelClosed += OnPanelClosed;
        }

        private void OnDestroy()
        {
            if (service == null)
                return;
            service.PanelOpened -= OnPanelOpened;
            service.PanelClosed -= OnPanelClosed;
        }

        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            WireButton(root, "show-interstitial", () => AdsManager.Instance.InterstitialAds.ShowInterstitialAd());
            WireToggle(root, "top-banner-opportunity", (bool x) => topBannerOpportunity.gameObject.SetActive(x));
            WireToggle(root, "bottom-banner-opportunity", (bool x) => bottomBannerOpportunity.gameObject.SetActive(x));
            WireToggle(root, "both-banner-opportunity", (bool x) => bothBannerOpportunity.gameObject.SetActive(x));
        }

        public void OnBackUnhandled() =>
            Report("Back pressed with no closable panel. A game would open its pause menu here.");

        public void Open(PanelDefinition definition)
        {
            if (definition == null)
            {
                Report("That panel definition isn't assigned on PanelSampleController.");
                return;
            }
            panelManager.OpenPanel(definition);
        }

        private void OnPanelOpened(Panel panel) =>
            Debug.Log($"[AdvertisementSample] Opened '{panel.Definition.name}' on {panel.Definition.Layer} (open: {service.OpenPanels.Count})");

        private void OnPanelClosed(Panel panel) =>
            Debug.Log($"[AdvertisementSample] Closed '{panel.Definition.name}' (open: {service.OpenPanels.Count})");

        private void Report(string message)
        {
            Debug.Log($"[AdvertisementSample] {message}");
        }

        private void WireButton(VisualElement root, string elementName, Action onClick)
        {
            var button = root.Q<Button>(elementName);
            if (button == null)
            {
                Debug.LogWarning($"[AdvertisementSample] Button '{elementName}' not found in UXML.", this);
                return;
            }
            button.clicked += onClick;
        }

        private void WireToggle(VisualElement root, string elementName, Action<bool> onToggleChanged)
        {
            var toggle = root.Q<Toggle>(elementName);
            if (toggle == null)
            {
                Debug.LogWarning($"[AdvertisementSample] Toggle '{elementName}' not found in UXML.", this);
                return;
            }
            toggle.RegisterValueChangedCallback(evt => onToggleChanged(evt.newValue));
        }
    }
}
