using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GG.Unity.IAP.Samples
{
    /// <summary>
    /// Test-scene driver for IAPManager. Holds the test switches and an event log; its CustomEditor
    /// (IAPSampleControllerEditor) draws the controls, so no storefront UI is needed.
    /// Talks only to the public IAPManagingService surface, so it exercises the same paths game code will.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IAPSampleController : MonoBehaviour
    {
        private const int MaxLogLines = 200;

        [Tooltip("Optional. Defaults to IAPManager.Instance.")]
        [SerializeField] private IAPManager manager;

        [Tooltip("Optional. Defaults to the SampleGrantHandler on the IAPManager's GameObject.")]
        [SerializeField] private SampleGrantHandler grantHandler;

        [Header("Failure injection")]
        [Tooltip("Swaps in a validator that rejects every order.")]
        [SerializeField] private bool rejectReceipts;

        [SerializeField] private string unknownProductId = "does_not_exist";

        [Header("Logging")]
        [SerializeField] private bool mirrorToConsole = true;

        private readonly List<string> log = new List<string>();
        private IAPManagingService subscribed;
        private bool appliedRejectReceipts;

        public IReadOnlyList<string> LogLines => log;
        public int PendingCalls { get; private set; }

        public IAPManagingService Service
        {
            get
            {
                var m = manager != null ? manager : IAPManager.Instance;
                return m != null ? m.Service : null;
            }
        }

        public SampleGrantHandler GrantHandler
        {
            get
            {
                if (grantHandler != null)
                    return grantHandler;

                var m = manager != null ? manager : IAPManager.Instance;
                return m != null ? m.GetComponent<SampleGrantHandler>() : null;
            }
        }

        public bool RejectReceipts
        {
            get => rejectReceipts;
            set
            {
                rejectReceipts = value;
                ApplyReceiptValidator();
            }
        }

        public string UnknownProductId
        {
            get => unknownProductId;
            set => unknownProductId = value;
        }

        /// <summary>Raised when the log or pending count changes. The editor repaints on it.</summary>
        public event Action Changed;

        // ---------------------------------------------------------------- lifecycle

        private void Start()
        {
            // IAPManager creates its Service in Awake, so it exists by now.
            Attach(Service);
        }

        private void Update()
        {
            // Re-attach if the service was replaced (e.g. the manager was recreated).
            if (!ReferenceEquals(Service, subscribed))
                Attach(Service);
        }

        private void OnDestroy() => Detach();

        // Lets the inspector toggle take effect when changed in Play Mode.
        private void OnValidate()
        {
            if (Application.isPlaying)
                ApplyReceiptValidator();
        }

        // ---------------------------------------------------------------- actions (called by the editor)

        public void Initialise()
        {
            var service = Service;
            if (service == null)
                return;

            Log("Initialise()");
            service.Initialise();
        }

        public void Purchase(string productId) => Run(PurchaseAsync(productId));

        /// <summary>Fires two purchases back to back. The second should return Busy.</summary>
        public void PurchaseTwice(string productId)
        {
            Run(PurchaseAsync(productId));
            Run(PurchaseAsync(productId));
        }

        public void PurchaseUnknown() => Run(PurchaseAsync(unknownProductId));

        public void Restore() => Run(RestoreAsync());

        public void ResetSave()
        {
            SampleGrantHandler.ResetSave();
            Log("Sample save cleared.");
        }

        public void ClearLog()
        {
            log.Clear();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- async plumbing

        private async Task PurchaseAsync(string productId)
        {
            var service = Service;
            if (service == null)
            {
                Log($"PurchaseAsync({productId}) skipped: no service.");
                return;
            }

            Log($"PurchaseAsync({productId})");
            var result = await service.PurchaseAsync(productId);
            Log($"  → {result}");
        }

        private async Task RestoreAsync()
        {
            var service = Service;
            if (service == null)
                return;

            Log("RestoreAsync()");
            var result = await service.RestoreAsync();
            Log($"  → Restore {(result.Success ? "succeeded" : "failed")}" +
                (result.Error != null ? ": " + result.Error : string.Empty));
        }

        /// <summary>Fire-and-forget with error reporting and a pending counter.</summary>
        private async void Run(Task task)
        {
            PendingCalls++;
            Changed?.Invoke();
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Log("  → EXCEPTION " + e.Message);
                Debug.LogException(e, this);
            }
            finally
            {
                PendingCalls--;
                Changed?.Invoke();
            }
        }

        // ---------------------------------------------------------------- service events

        private void Attach(IAPManagingService service)
        {
            Detach();
            if (service == null)
                return;

            subscribed = service;
            service.StateChanged += OnStateChanged;
            service.ProductsUpdated += OnProductsUpdated;
            service.PurchaseConfirmed += OnPurchaseConfirmed;
            service.OwnedProductsFetched += OnOwnedProductsFetched;

            appliedRejectReceipts = !rejectReceipts; // force the validator to be applied
            ApplyReceiptValidator();

            Log($"Attached to service (state {service.State}).");
        }

        private void Detach()
        {
            if (subscribed == null)
                return;

            subscribed.StateChanged -= OnStateChanged;
            subscribed.ProductsUpdated -= OnProductsUpdated;
            subscribed.PurchaseConfirmed -= OnPurchaseConfirmed;
            subscribed.OwnedProductsFetched -= OnOwnedProductsFetched;
            subscribed = null;
        }

        private void ApplyReceiptValidator()
        {
            var service = Service;
            if (service == null || appliedRejectReceipts == rejectReceipts)
                return;

            appliedRejectReceipts = rejectReceipts;
            service.ReceiptValidator = rejectReceipts ? new RejectAllReceiptValidator() : null;
            Log(rejectReceipts ? "Receipt validator: reject all" : "Receipt validator: accept all");
        }

        private void OnStateChanged(IAPState state) => Log($"[event] StateChanged → {state}");
        private void OnProductsUpdated() => Log("[event] ProductsUpdated");

        private void OnPurchaseConfirmed(PurchaseContext ctx) =>
            Log($"[event] PurchaseConfirmed [{string.Join(", ", ctx.ProductIds)}] " +
                $"txn {ctx.TransactionId ?? "-"} user-initiated: {ctx.IsUserInitiated}");

        private void OnOwnedProductsFetched(IReadOnlyList<string> ids) =>
            Log($"[event] OwnedProductsFetched [{string.Join(", ", ids)}]");

        // ---------------------------------------------------------------- helpers

        private void Log(string message)
        {
            log.Add($"{DateTime.Now:HH:mm:ss.fff}  {message}");
            if (log.Count > MaxLogLines)
                log.RemoveAt(0);

            if (mirrorToConsole)
                Debug.Log("[IAP Sample] " + message, this);

            Changed?.Invoke();
        }

        private sealed class RejectAllReceiptValidator : IReceiptValidator
        {
            public Task<bool> ValidateAsync(PurchaseContext context) => Task.FromResult(false);
        }
    }
}
