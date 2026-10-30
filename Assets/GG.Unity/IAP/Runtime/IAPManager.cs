using GG.Unity.Timers;
using UnityEngine;

namespace GG.Unity.IAP
{
    [DisallowMultipleComponent]
    public sealed class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        [Header("Setup")]
        [SerializeField] private IAPCatalogConfig catalog;

        [Tooltip("Leave off if you need to set BeforeInitialise / handlers from code first.")]
        [SerializeField] private bool initialiseOnStart;

        [SerializeField] private IAPSettings settings = new IAPSettings();

        [SerializeField] private MonoBehaviour grantHandler;

        public IAPManagingService Service { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                IAPLog.Warn("Duplicate IAPService found. Destroying it. Keep this on a dedicated GameObject.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (transform.parent != null)
            {
                IAPLog.Warn("IAPService must be a root object for DontDestroyOnLoad. Detaching from parent.");
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);

            if (catalog == null)
            {
                IAPLog.Error("No IAPCatalogConfig assigned. IAP is disabled.");
                return;
            }

            // Created in Awake so other scripts can call Manager.SetPurchaseHandler(...) from their Start.
            // TaskBackOffTimer captures the main-thread SynchronizationContext here, so it must be
            // constructed on the main thread (it is).
            Service = new IAPManagingService(
                new UnityIapBackend(catalog),
                onRetryDue => new TaskBackOffTimer(
                    onRetryDue,
                    settings.BaseRetryDelaySeconds,
                    settings.MaxRetryDelaySeconds,
                    settings.JitterFraction),
                settings);

            var handler = grantHandler as IPurchaseGrantHandler ?? GetComponent<IPurchaseGrantHandler>();
            if (handler != null)
            {
                Service.SetPurchaseHandler(handler.GrantAsync);
                Service.OwnedProductsFetched += handler.OnOwnedProductsFetched;
            }
        }

        private void Start()
        {
            if (initialiseOnStart && Instance == this)
                Service?.Initialise();
        }

        private void OnDestroy()
        {
            // Dispose cancels the Task-based retry timer, which would otherwise outlive this object.
            Service?.Dispose();
            Service = null;

            if (Instance == this)
                Instance = null;
        }

        #region Validation
        private void OnValidate()
        {
            if (grantHandler == null)
            {
                Debug.LogError($"{gameObject.name}:{GetType()} requires a grant handler to award IAP entitlements");
            }
            if (grantHandler != null && grantHandler is not IPurchaseGrantHandler)
            {
                Debug.LogError($"{grantHandler.GetType().Name} does not implement IPurchaseGrantHandler.", this);
                grantHandler = null;
            }
        }
        #endregion
    }
}
