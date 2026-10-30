using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    [RequireComponent(typeof(PanelRenderer))]
    public class PanelManager : MonoBehaviour
    {
        [SerializeField]
        private PanelRenderer panelRenderer;

        private int rendererVersion = 0;

        public PanelManagingService Service { get; private set; }

        private void Awake()
        {
            Service = new PanelManagingService();
        }

        private void OnEnable()
        {
            // Fires immediately if the UI is already loaded, and again on every reload.
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        public void OpenPanel(PanelDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError($"{name}: no PanelDefinition to open.", this);
                return;
            }
            if (Service == null)
            {
                Debug.LogError($"{name}: PanelService not found or not initialised.", this);
                return;
            }
            Service.Open(definition);
        }

        private void OnDisable()
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            Service?.Detach();
        }

        private void OnDestroy()
        {
            Service?.Dispose();
            Service = null;
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (version == rendererVersion)
                return;

            rendererVersion = version;
            Service.Attach(root);
        }

        #region Validation
        void OnValidate()
        {
            if (panelRenderer == null)
            {
                if (gameObject.TryGetComponent<PanelRenderer>(out var panelRenderer))
                    this.panelRenderer = panelRenderer;
                else
                    Debug.LogWarning($"{name} has null panelRenderer, couldn't assign it.");
            }
        }
        #endregion
    }
}
