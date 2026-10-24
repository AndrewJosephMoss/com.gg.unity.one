using UnityEngine;
using UnityEngine.UIElements;
using GG.Unity.UserInterface.Buttons;

namespace GG.Unity.Advertisement.UIToolkit
{
    [RequireComponent(typeof(PanelRenderer))]
    public class PanelRendererRewardAdButton : RewardAdButton
    {
        [Tooltip("The 'name' attribute of the LoadingButton in your UXML")]
        [SerializeField]
        private string buttonName;

        [SerializeField]
        private PanelRenderer panelRenderer;

        private LoadingButton _button;

        private void Awake()
        {
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDestroy()
        {
            Unbind();
            if (panelRenderer != null)
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // CancelWait() doesn't touch visuals, so clear the spinner explicitly.
            _button?.SetLoading(false);
        }

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            Unbind();

            _button = root.Q<LoadingButton>(buttonName);
            if (_button == null)
            {
                Debug.LogWarning($"LoadingButton '{buttonName}' not found in {panelRenderer.name}", this);
                return;
            }

            _button.clicked += ShowRewardAd;

            // The UI may reload while an ad request is in flight.
            _button.SetLoading(IsWaitingForAd);
        }

        private void Unbind()
        {
            if (_button == null)
                return;

            _button.clicked -= ShowRewardAd;
            _button = null;
        }

        protected override void SetAdLoadingState(bool isLoading)
        {
            _button?.SetLoading(isLoading);
        }

        #region Validation
        private void OnValidate()
        {
            if (panelRenderer == null)
            {
                if (gameObject.TryGetComponent<PanelRenderer>(out var renderer))
                    panelRenderer = renderer; 
            }
        }
        #endregion
    }
}
