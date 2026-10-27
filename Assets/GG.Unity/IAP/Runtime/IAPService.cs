using GG.Unity.Timers;
using UnityEngine;

namespace GG.Unity.IAP
{
    /// <summary>
    /// Unity glue for IAPManager: scene-root singleton, DontDestroyOnLoad, serialised config.
    /// Put this on a dedicated root GameObject in your first scene and assign a catalog.
    /// All purchase logic lives in IAPManager (plain C#), reached through <see cref="Manager"/>.
    ///
    ///   IAPService.Instance.Manager.SetPurchaseHandler(...);
    ///   IAPService.Instance.Manager.Initialise();   // or tick "Initialise On Start"
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IAPService : MonoBehaviour
    {
        public static IAPService Instance { get; private set; }

        [Header("Setup")]
        [SerializeField] private IAPCatalogConfig catalog;
        [Tooltip("Leave off if you need to set BeforeInitialise / handlers from code first.")]
        [SerializeField] private bool initialiseOnStart;

        [SerializeField] private IAPSettings settings = new IAPSettings();

        /// <summary>Null if setup failed (no catalog assigned) or on a destroyed duplicate.</summary>
        public IAPManager Manager { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Needed when Enter Play Mode Options has domain reload disabled.
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
            Manager = new IAPManager(
                new UnityIapBackend(catalog),
                onRetryDue => new TaskBackOffTimer(
                    onRetryDue,
                    settings.BaseRetryDelaySeconds,
                    settings.MaxRetryDelaySeconds,
                    settings.JitterFraction),
                settings);
        }

        private void Start()
        {
            if (initialiseOnStart && Instance == this)
                Manager?.Initialise();
        }

        private void OnDestroy()
        {
            // Dispose cancels the Task-based retry timer, which would otherwise outlive this object.
            Manager?.Dispose();
            Manager = null;

            if (Instance == this)
                Instance = null;
        }
    }
}
