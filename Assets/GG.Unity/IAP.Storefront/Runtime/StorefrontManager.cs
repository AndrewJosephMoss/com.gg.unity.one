using System.Threading;
using System.Threading.Tasks;
using GG.Unity.UserInterface.Panels;
using UnityEngine;

namespace GG.Unity.IAP.Storefront
{
    /// <summary>
    /// Scene entry point: owns the StorefrontModel (one per scene, shared by every storefront panel)
    /// and opens storefront panels on the PanelManager.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StorefrontManager : MonoBehaviour
    {
        [SerializeField] private PanelManager panelManager;
        [SerializeField] private StorefrontPanelDefinition defaultStorefront;

        [Tooltip("Optional. A component implementing IOwnershipProvider (often your grant handler). " +
                 "Without it, non-consumables never show as owned.")]
        [SerializeField] private MonoBehaviour ownershipProvider;

        private StorefrontModel model;

        /// <summary>Created lazily so it doesn't depend on Awake order relative to IAPManager.</summary>
        public StorefrontModel Model
        {
            get
            {
                if (model != null)
                    return model;

                var iap = IAPManager.Instance != null ? IAPManager.Instance.Service : null;
                if (iap == null)
                {
                    Debug.LogError("[GG.Storefront] No IAPManager / IAP service available.", this);
                    return null;
                }

                model = new StorefrontModel(iap, ownershipProvider as IOwnershipProvider);
                return model;
            }
        }

        public Task OpenAsync(string sectionId = null, CancellationToken cancellationToken = default) =>
            OpenAsync(defaultStorefront, sectionId, cancellationToken);

        /// <summary>Completes when the panel closes.</summary>
        public Task OpenAsync(StorefrontPanelDefinition definition, string sectionId = null,
            CancellationToken cancellationToken = default)
        {
            var m = Model;
            if (m == null || panelManager == null || definition == null)
            {
                Debug.LogError("[GG.Storefront] Cannot open storefront: missing model, PanelManager or definition.", this);
                return Task.CompletedTask;
            }

            var args = new StorefrontArgs(m, panelManager.Service, sectionId);
            return panelManager.OpenPanelAsync<object>(definition, args, cancellationToken);
        }

        private void OnDestroy()
        {
            model?.Dispose();
            model = null;
        }

        private void OnValidate()
        {
            if (ownershipProvider != null && ownershipProvider is not IOwnershipProvider)
            {
                Debug.LogError($"{ownershipProvider.GetType().Name} does not implement IOwnershipProvider.", this);
                ownershipProvider = null;
            }
        }
    }
}
