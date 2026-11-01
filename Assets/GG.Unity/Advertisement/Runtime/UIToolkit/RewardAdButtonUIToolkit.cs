using System;
using GG.Unity.UserInterface.Buttons;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.Advertisement.UIToolkit
{
    [RequireComponent(typeof(PanelRenderer))]
    public class PanelRendererRewardAdButton : RewardAdButton, INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        [Tooltip("The 'name' attribute of the StatusButton in your UXML")]
        [SerializeField] private string buttonName;
        [SerializeField] private PanelRenderer panelRenderer;

        private StatusButton button;

        private ButtonStatus rewardButtonStatus;
        [CreateProperty]
        public ButtonStatus RewardButtonStatus
        {
            get => rewardButtonStatus;
            private set
            {
                if (value == rewardButtonStatus)
                    return;
                rewardButtonStatus = value;
                Notify(nameof(RewardButtonStatus));
            }
        }

        private void Awake() => panelRenderer.RegisterUIReloadCallback(OnUIReload);

        private void OnDestroy()
        {
            Unbind();
            if (panelRenderer != null)
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RewardButtonStatus = ButtonStatus.None;
        }

        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            Unbind();

            button = root.Q<StatusButton>(buttonName);
            if (button == null)
            {
                Debug.LogWarning($"StatusButton '{buttonName}' not found in {panelRenderer.name}", this);
                return;
            }

            button.dataSource = this;
            button.SetBinding(StatusButton.StatusProperty, new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(RewardButtonStatus)),
                bindingMode = BindingMode.ToTarget
            });
            button.clicked += ShowRewardAd;
        }

        private void Unbind()
        {
            if (button == null)
                return;

            button.clicked -= ShowRewardAd;
            button.ClearBinding(StatusButton.StatusProperty);
            button.dataSource = null;
            button = null;
        }

        protected override void SetAdLoadingState(bool isLoading) =>
            RewardButtonStatus = isLoading ? ButtonStatus.Loading : ButtonStatus.None;

        private void Notify(string property) =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));

        private void OnValidate()
        {
            if (panelRenderer == null && TryGetComponent<PanelRenderer>(out var renderer))
                panelRenderer = renderer;
        }
    }
}
