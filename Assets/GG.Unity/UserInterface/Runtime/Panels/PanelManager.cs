using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    /// <summary>
    /// Scene entry point: owns a <see cref="PanelManagingService"/> and keeps it attached to the PanelRenderer's root.
    /// Runs early so other scripts can open panels from their own Awake/Start.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    [DefaultExecutionOrder(-100)]
    public class PanelManager : MonoBehaviour
    {
        [SerializeField] private PanelRenderer panelRenderer;

        [SerializeField, Tooltip("Optional base styles for panel hosts, the modal backdrop and transitions (e.g. PanelSystem.uss).")]
        private StyleSheet styleSheet;

        private PanelManagingService service;
        private int rendererVersion = -1;

        public IPanelService Service => service;

        private void Awake()
        {
            if (panelRenderer == null)
                TryGetComponent(out panelRenderer);

            service = new PanelManagingService(styleSheet);
        }

        private void OnEnable()
        {
            if (panelRenderer == null)
            {
                Debug.LogError($"{name}: no PanelRenderer found; panels will not be shown.", this);
                return;
            }

            // Fires immediately if the UI is already loaded, and again on every reload.
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            if (panelRenderer != null)
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);

            service?.Detach();
        }

        private void OnDestroy()
        {
            service?.Dispose();
            service = null;
        }

        public Panel OpenPanel(PanelDefinition definition, object args = null)
        {
            if (service == null)
            {
                Debug.LogError($"{name}: panel service is not initialised.", this);
                return null;
            }
            return service.Open(definition, args);
        }

        public Task<T> OpenPanelAsync<T>(PanelDefinition definition, object args = null,
            CancellationToken cancellationToken = default)
        {
            if (service == null)
            {
                Debug.LogError($"{name}: panel service is not initialised.", this);
                return Task.FromResult(default(T));
            }
            return service.OpenAsync<T>(definition, args, cancellationToken);
        }

        /// <summary>
        /// Back/Cancel entry point. Returns true if the request was consumed
        /// (a panel closed, or a modal blocked it); false means nothing handled it.
        /// </summary>
        public bool CloseTopPanel() => service != null && service.CloseTopPanel();

        public void CloseAllPanels() => service?.CloseAll();

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (service == null || root == null)
                return;
            if (version == rendererVersion && service.IsAttached)
                return;

            rendererVersion = version;
            service.Attach(root);
        }

        private void Reset() => TryGetComponent(out panelRenderer);

        private void OnValidate()
        {
            if (panelRenderer == null)
                TryGetComponent(out panelRenderer);
        }
    }
}
