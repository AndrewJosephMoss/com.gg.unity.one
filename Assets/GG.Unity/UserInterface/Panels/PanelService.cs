using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    [RequireComponent(typeof(PanelRenderer))]
    public class PanelService : MonoBehaviour
    {
        [SerializeField]
        private PanelRenderer panelRenderer;

        public PanelManager Manager { get; private set; }

        private void Awake()
        {
            Manager = new PanelManager();
        }

        private void OnEnable()
        {
            // Fires immediately if the UI is already loaded, and again on every reload.
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            Manager.Detach();
        }

        private void OnDestroy()
        {
            Manager?.Dispose();
            Manager = null;
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            Manager.Attach(root);
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
