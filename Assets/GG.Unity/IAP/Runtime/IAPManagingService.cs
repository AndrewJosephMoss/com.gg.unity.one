using GG.Unity.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GG.Unity.IAP
{
    /// <summary>
    /// Setup (via IAPManager.Instance.Service):
    ///   1. Optionally set BeforeInitialise (e.g. UGS init + sign-in) and ReceiptValidator.
    ///   2. Call SetPurchaseHandler(...) before Initialise().
    /// </summary>
    public sealed class IAPManagingService : IDisposable
    {
        public IAPState State { get; private set; } = IAPState.Uninitialised;
        public bool IsReady => State == IAPState.Ready;

        public IReadOnlyList<ProductInfo> Products => backend.Products;

        public event Action<IAPState> StateChanged;
        public event Action ProductsUpdated;

        /// <summary>
        /// Product ids of already-confirmed orders, after a purchases fetch or a restore.
        /// Use this to re-grant non-consumables/subscriptions on restore (idempotently).
        /// </summary>
        public event Action<IReadOnlyList<string>> OwnedProductsFetched;

        /// <summary>Raised after ANY purchase was confirmed with the store (user-initiated or not).</summary>
        public event Action<PurchaseContext> PurchaseConfirmed;

        /// <summary>
        /// Runs at the start of every init attempt. Put UGS initialisation / sign-in here if you use it.
        /// Unity requires UGS to be initialised before IAP when Authentication/Analytics are used.
        /// Must be safe to call repeatedly.
        /// </summary>
        public Func<Task> BeforeInitialise { get; set; }

        public IReceiptValidator ReceiptValidator
        {
            get => receiptValidator;
            set => receiptValidator = value ?? new AcceptAllReceiptValidator();
        }

        private readonly IIapBackend backend;
        private readonly IAPSettings settings;
        private readonly IBackOffTimer initRetryTimer;

        private IReceiptValidator receiptValidator = new AcceptAllReceiptValidator();
        private PurchaseGrantHandler grantHandler;

        private bool isInitialised;
        private bool attemptRunning;
        private bool disposed;

        // Only set by PurchaseAsync, for the current session's user-initiated purchase.
        private TaskCompletionSource<PurchaseResult> inFlight;
        private string inFlightProductId;

        //TODO inFlight state event for UI to hook on to?

        // Transactions currently being validated/granted/confirmed. Guards against re-delivery.
        private readonly HashSet<string> inProgress = new HashSet<string>();

        // Pending orders that arrived before a grant handler was set.
        private readonly List<BackendOrder> awaitingHandler = new List<BackendOrder>();

        /// <param name="backOffFactory">
        /// Builds the retry timer around the callback to run when a retry is due. Production passes
        /// a TaskBackOffTimer (or the coroutine BackOffTimer); tests pass a fake.
        /// </param>
        internal IAPManagingService(IIapBackend backend, Func<Action, IBackOffTimer> backOffFactory, IAPSettings settings)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (backOffFactory == null)
                throw new ArgumentNullException(nameof(backOffFactory));

            initRetryTimer = backOffFactory(OnInitRetryDue);

            // Subscribe now, so handlers are attached before anything can connect.
            backend.Disconnected += OnBackendDisconnected;
            backend.PurchasePending += OnBackendPending;
            backend.PurchaseConfirmed += OnBackendConfirmed;
            backend.PurchaseFailed += OnBackendFailed;
            backend.PurchaseDeferred += OnBackendDeferred;
            backend.OwnedProductsFetched += OnBackendOwnedProducts;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            // A Task-based timer outlives GameObjects, so it must be cancelled explicitly.
            initRetryTimer.Cancel();

            backend.Disconnected -= OnBackendDisconnected;
            backend.PurchasePending -= OnBackendPending;
            backend.PurchaseConfirmed -= OnBackendConfirmed;
            backend.PurchaseFailed -= OnBackendFailed;
            backend.PurchaseDeferred -= OnBackendDeferred;
            backend.OwnedProductsFetched -= OnBackendOwnedProducts;
            backend.Dispose();

            FailInFlight("IAPManager disposed.");
        }

        // ---------------------------------------------------------------- initialisation

        /// <summary>Idempotent. Starts connecting; retries with exponential backoff until Ready.</summary>
        public void Initialise()
        {
            if (disposed || isInitialised)
                return;

            isInitialised = true;
            _ = RunInitAttemptAsync();
        }

        private void OnInitRetryDue()
        {
            _ = RunInitAttemptAsync();
        }

        private async Task RunInitAttemptAsync()
        {
            if (attemptRunning || disposed || State == IAPState.Ready)
                return;

            attemptRunning = true;
            SetState(IAPState.Initialising);

            try
            {
                if (BeforeInitialise != null)
                    await BeforeInitialise();

                await backend.ConnectAsync();
                await backend.FetchProductsAsync(settings.ProductFetchTimeoutSeconds);
            }
            catch (Exception e)
            {
                attemptRunning = false;
                if (disposed)
                    return;

                IAPLog.Warn($"Init attempt {initRetryTimer.AttemptCount + 1} failed: {e.Message}");
                SetState(IAPState.WaitingToRetry);
                initRetryTimer.Schedule();
                return;
            }

            attemptRunning = false;
            if (disposed)
                return;

            initRetryTimer.Reset();
            SetState(IAPState.Ready);
            ProductsUpdated?.Invoke();

            // Deliver existing/pending orders. Pending ones arrive via OnPurchasePending.
            backend.FetchPurchases();
        }

        private void OnBackendDisconnected(string reason)
        {
            // While an attempt is running it reports the failure itself (ConnectAsync throws).
            if (State != IAPState.Ready || disposed)
                return;

            IAPLog.Warn("Store disconnected: " + reason);
            SetState(IAPState.WaitingToRetry);

            // The user may still complete this order later; it will then arrive as an unsolicited pending order.
            FailInFlight("Store disconnected.");

            initRetryTimer.Schedule();
        }

        // ---------------------------------------------------------------- purchasing API

        /// <summary>
        /// Sets the delivery handler. Pending orders received before this is set are held
        /// (unconfirmed) and processed as soon as it is.
        /// </summary>
        public void SetPurchaseHandler(PurchaseGrantHandler handler)
        {
            grantHandler = handler;

            if (handler == null || awaitingHandler.Count == 0)
                return;

            var queued = awaitingHandler.ToList();
            awaitingHandler.Clear();

            foreach (var order in queued)
                _ = HandlePendingAsync(order);
        }

        public bool TryGetProduct(string productId, out ProductInfo product)
        {
            product = Products.FirstOrDefault(p => p.Id == productId);
            return product != null;
        }

        /// <summary>
        /// Starts a purchase and completes when it succeeds (granted AND confirmed), fails, is
        /// cancelled, or is deferred. Orders that arrive without a PurchaseAsync call (restored,
        /// interrupted, approved later) go through the same handler and PurchaseConfirmed event.
        /// </summary>
        public Task<PurchaseResult> PurchaseAsync(string productId)
        {
            if (disposed || !IsReady)
                return Task.FromResult(new PurchaseResult(PurchaseStatus.NotReady, productId));

            if (grantHandler == null)
                return Task.FromResult(new PurchaseResult(
                    PurchaseStatus.Failed, productId, error: "SetPurchaseHandler must be called before purchasing."));

            // Only one user-initiated purchase may be in progress at a time.
            if (inFlight != null)
                return Task.FromResult(new PurchaseResult(PurchaseStatus.Busy, productId));

            // Check the product BEFORE touching inFlight, so there is nothing to undo.
            if (!TryGetProduct(productId, out _))
                return Task.FromResult(new PurchaseResult(PurchaseStatus.UnknownProduct, productId));

            // Set up the completion source BEFORE calling the store: resolution events may fire immediately.
            var tcs = new TaskCompletionSource<PurchaseResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            inFlight = tcs;
            inFlightProductId = productId;

            backend.Purchase(productId);

            return tcs.Task;
        }

        public Task<RestoreResult> RestoreAsync()
        {
            if (disposed || !IsReady)
                return Task.FromResult(new RestoreResult(false, "IAP is not ready."));

            var tcs = new TaskCompletionSource<RestoreResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            backend.Restore((success, error) => tcs.TrySetResult(new RestoreResult(success, error)));
            return tcs.Task;
        }

        // ---------------------------------------------------------------- order pipeline

        private void OnBackendPending(BackendOrder order)
        {
            _ = HandlePendingAsync(order);
        }

        private async Task HandlePendingAsync(BackendOrder order)
        {
            var key = KeyOf(order);
            if (!inProgress.Add(key))
                return; // Already being handled: Unity re-delivers pending orders on every fetch until confirmed.

            bool confirmRequested = false;

            try
            {
                if (grantHandler == null)
                {
                    awaitingHandler.Add(order);
                    IAPLog.Warn("Pending order received but no purchase handler set. Holding it unconfirmed.");
                    return;
                }

                var context = ToContext(order);

                if (!await receiptValidator.ValidateAsync(context))
                {
                    IAPLog.Warn($"Receipt validation rejected order {order.TransactionId}. Not granting or confirming.");
                    CompleteInFlight(order.ProductIds, new PurchaseResult(
                        PurchaseStatus.Failed, FirstId(order), order.TransactionId, "Receipt validation failed."));
                    return;
                }

                if (disposed)
                    return;

                if (!await grantHandler(context))
                {
                    IAPLog.Warn($"Purchase handler declined order {order.TransactionId}. Leaving it unconfirmed.");
                    CompleteInFlight(order.ProductIds, new PurchaseResult(
                        PurchaseStatus.Failed, FirstId(order), order.TransactionId, "Purchase handler declined."));
                    return;
                }

                if (disposed)
                    return;

                backend.Confirm(order);
                confirmRequested = true; // Stays "in progress" until the confirmed/failed event arrives.
            }
            catch (Exception e)
            {
                IAPLog.Error($"Exception while processing order {order.TransactionId}: {e}");
                CompleteInFlight(order.ProductIds, new PurchaseResult(
                    PurchaseStatus.Failed, FirstId(order), order.TransactionId, e.Message));
            }
            finally
            {
                if (!confirmRequested)
                    inProgress.Remove(key);
            }
        }

        private void OnBackendConfirmed(BackendOrder order)
        {
            inProgress.Remove(KeyOf(order));

            PurchaseConfirmed?.Invoke(ToContext(order));

            CompleteInFlight(order.ProductIds, new PurchaseResult(
                PurchaseStatus.Success, FirstId(order), order.TransactionId));
        }

        private void OnBackendFailed(BackendOrder order)
        {
            inProgress.Remove(KeyOf(order));

            CompleteInFlight(order.ProductIds, new PurchaseResult(
                order.FailureStatus, FirstId(order), order.TransactionId, order.Error));
        }

        private void OnBackendDeferred(BackendOrder order)
        {
            IAPLog.Info($"Purchase deferred for {FirstId(order)} (e.g. awaiting approval).");

            CompleteInFlight(order.ProductIds, new PurchaseResult(
                PurchaseStatus.Deferred, FirstId(order), order.TransactionId));
        }

        private void OnBackendOwnedProducts(IReadOnlyList<string> productIds)
        {
            OwnedProductsFetched?.Invoke(productIds);
        }

        // ---------------------------------------------------------------- helpers

        private void CompleteInFlight(IReadOnlyList<string> productIds, PurchaseResult result)
        {
            if (inFlight == null)
                return;

            // If the order names products, it must include the one we are waiting for.
            if (productIds != null && productIds.Count > 0 && !productIds.Contains(inFlightProductId))
                return;

            var tcs = inFlight;
            inFlight = null;
            inFlightProductId = null;
            tcs.TrySetResult(result);
        }

        private void FailInFlight(string error)
        {
            if (inFlight == null)
                return;

            var tcs = inFlight;
            var productId = inFlightProductId;
            inFlight = null;
            inFlightProductId = null;
            tcs.TrySetResult(new PurchaseResult(PurchaseStatus.Failed, productId, error: error));
        }

        private PurchaseContext ToContext(BackendOrder order)
        {
            bool userInitiated = inFlightProductId != null && order.ProductIds.Contains(inFlightProductId);
            return new PurchaseContext(order.ProductIds, order.TransactionId, order.Receipt, userInitiated);
        }

        private static string KeyOf(BackendOrder order) =>
            !string.IsNullOrEmpty(order.TransactionId) ? order.TransactionId : string.Join("|", order.ProductIds);

        private static string FirstId(BackendOrder order) =>
            order.ProductIds.Count > 0 ? order.ProductIds[0] : null;

        private void SetState(IAPState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
