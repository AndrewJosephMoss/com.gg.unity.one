using UnityEngine;
using UnityEngine.UIElements;
using GG.Unity.UserInterface.Buttons;
using JetBrains.Annotations;

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

        private StatusButton _button;
        private int version;

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
            if (_button != null)
                _button.Status = ButtonStatus.None;
        }

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement root, int version)
        {
            if (this.version != version)
                return;

            Unbind();

            _button = root.Q<StatusButton>(buttonName);
            if (_button == null)
            {
                Debug.LogWarning($"LoadingButton '{buttonName}' not found in {panelRenderer.name}", this);
                return;
            }

            _button.clicked += ShowRewardAd;

            // The UI may reload while an ad request is in flight.
            _button.Status = IsWaitingForAd ? ButtonStatus.Loading : ButtonStatus.None;
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
            _button.Status = isLoading ? ButtonStatus.Loading : ButtonStatus.None;
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
