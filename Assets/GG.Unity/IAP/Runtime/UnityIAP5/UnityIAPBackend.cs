using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace GG.Unity.IAP
{
    /// <summary>
    /// Adapter over Unity IAP 5 (IStoreService / IProductService / IPurchaseService).
    /// </summary>
    internal sealed class UnityIapBackend : IIapBackend
    {
        private readonly IAPCatalogConfig config;
        private readonly ICatalogProvider catalogProvider = new CatalogProvider();

        private IStoreService store;
        private IProductService productService;
        private IPurchaseService purchaseService;

        private bool servicesCreated;
        private bool disposed;

        private string connectFailure;
        private TaskCompletionSource<bool> productFetchTcs;
        private string productFetchError;
        private TaskCompletionSource<bool> purchasesFetchTcs;
        private string purchasesFetchError;

        private IReadOnlyList<ProductInfo> products = Array.Empty<ProductInfo>();

        public event Action<string> Disconnected;
        public event Action<BackendOrder> PurchasePending;
        public event Action<BackendOrder> PurchaseConfirmed;
        public event Action<BackendOrder> PurchaseFailed;
        public event Action<BackendOrder> PurchaseDeferred;
        public event Action<IReadOnlyList<string>> PurchasesFetched;
        public IReadOnlyList<ProductInfo> Products => products;

        public UnityIapBackend(IAPCatalogConfig config)
        {
            this.config = config;
        }

        #region Connecting
        public async Task ConnectAsync()
        {
            EnsureServices();

            // A failed connection raises OnStoreDisconnected rather than (necessarily) throwing,
            // so we record it and surface it as an exception ourselves.
            connectFailure = null;
            await store.Connect();

            if (connectFailure != null)
                throw new InvalidOperationException("Store connection failed: " + connectFailure);
        }

        private void HandleStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            var reason = failure?.ToString() ?? "unknown";
            connectFailure = reason;
            Disconnected?.Invoke(reason);
        }
        #endregion

        #region Product fetching
        public async Task FetchProductsAsync(float timeoutSeconds)
        {
            EnsureServices();

            productFetchError = null;
            productFetchTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // No retries, handled by caller
            catalogProvider.FetchProducts(
                productService.FetchProductsWithNoRetries,
                DefaultStoreHelper.GetDefaultStoreName());

            var finished = await Task.WhenAny(productFetchTcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));

            if (finished != productFetchTcs.Task)
                throw new TimeoutException($"Product fetch timed out after {timeoutSeconds:0.#}s.");

            if (!productFetchTcs.Task.Result)
                throw new InvalidOperationException("Product fetch failed: " + (productFetchError ?? "unknown reason"));
        }

        private void HandleProductsFetched(List<Product> fetched)
        {
            products = GetProductInfos();

            var present = new HashSet<string>(products.Select(p => p.Id));
            var missing = config.Products
                .Where(e => !string.IsNullOrWhiteSpace(e.id) && !present.Contains(e.id))
                .Select(e => e.id)
                .ToList();

            if (missing.Count > 0)
                IAPLog.Warn("Configured products not returned by the store: " + string.Join(", ", missing));

            productFetchTcs?.TrySetResult(true);
        }

        private IReadOnlyList<ProductInfo> GetProductInfos()
        {
            var list = new List<ProductInfo>();

            foreach (var p in productService.GetProducts())
            {
                var m = p.metadata;
                list.Add(new ProductInfo(
                    p.definition.id,
                    ToKind(p.definition.type),
                    m?.localizedTitle,
                    m?.localizedDescription,
                    m?.localizedPriceString,
                    m != null ? m.localizedPrice : 0m,
                    m?.isoCurrencyCode,
                    p.availableToPurchase));
            }

            return list;
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failure)
        {
            productFetchError = failure?.ToString();
            productFetchTcs?.TrySetResult(false);
        }
        #endregion

        #region Purchases fetching
        public async Task FetchPurchasesAsync(float timeoutSeconds)
        {
            EnsureServices();

            purchasesFetchError = null;
            purchasesFetchTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            purchaseService.FetchPurchases();

            var finished = await Task.WhenAny(purchasesFetchTcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));

            if (finished != purchasesFetchTcs.Task)
                throw new TimeoutException($"Purchases fetch timed out after {timeoutSeconds:0.#}s.");

            if (!purchasesFetchTcs.Task.Result)
                throw new InvalidOperationException("Purchases fetch failed: " + (purchasesFetchError ?? "unknown reason"));
        }

        private void HandlePurchasesFetched(Orders orders)
        {
            var owned = orders.ConfirmedOrders
                .SelectMany(o => o.CartOrdered.Items().Select(i => i.Product.definition.id))
                .Distinct()
                .ToList();

            PurchasesFetched?.Invoke(owned);
        }

        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            purchasesFetchError = failure?.ToString();
            purchasesFetchTcs.TrySetResult(false);
        }
        #endregion

        #region Setup / Teardown
        private void EnsureServices()
        {
            if (servicesCreated)
                return;

            servicesCreated = true;

            store = UnityIAPServices.DefaultStore();
            productService = UnityIAPServices.DefaultProduct();
            purchaseService = UnityIAPServices.DefaultPurchase();

            store.OnStoreDisconnected += HandleStoreDisconnected;
            productService.OnProductsFetched += HandleProductsFetched;
            productService.OnProductsFetchFailed += HandleProductsFetchFailed;
            purchaseService.OnPurchasesFetched += HandlePurchasesFetched;
            purchaseService.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
            purchaseService.OnPurchasePending += HandlePurchasePending;
            purchaseService.OnPurchaseConfirmed += HandlePurchaseConfirmed;
            purchaseService.OnPurchaseFailed += HandlePurchaseFailed;
            purchaseService.OnPurchaseDeferred += HandlePurchaseDeferred;

            BuildCatalog();
        }

        private void BuildCatalog()
        {
            var definitions = new List<ProductDefinition>();
            var storeSpecific = new Dictionary<string, StoreSpecificIds>();

            foreach (var entry in config.Products)
            {
                if (string.IsNullOrWhiteSpace(entry.id))
                    continue;

                definitions.Add(new ProductDefinition(entry.id, ToProductType(entry.kind)));

                bool hasOverride =
                    !string.IsNullOrWhiteSpace(entry.googlePlayId) ||
                    !string.IsNullOrWhiteSpace(entry.appleAppStoreId);

                if (!hasOverride)
                    continue;

                // Same shape as the sample's subscription: one id per store, falling back to 'id'.
                storeSpecific[entry.id] = new StoreSpecificIds
                {
                    { PickId(entry.googlePlayId, entry.id), UnityEngine.Purchasing.GooglePlay.Name },
                    { PickId(entry.appleAppStoreId, entry.id), UnityEngine.Purchasing.AppleAppStore.Name }
                };
            }

            catalogProvider.AddProducts(definitions, storeSpecific);
        }

        private static string PickId(string overrideId, string fallback) =>
            string.IsNullOrWhiteSpace(overrideId) ? fallback : overrideId;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            productFetchTcs?.TrySetCanceled();

            if (!servicesCreated)
                return;

            store.OnStoreDisconnected -= HandleStoreDisconnected;
            productService.OnProductsFetched -= HandleProductsFetched;
            productService.OnProductsFetchFailed -= HandleProductsFetchFailed;
            purchaseService.OnPurchasesFetched -= HandlePurchasesFetched;
            purchaseService.OnPurchasesFetchFailed -= HandlePurchasesFetchFailed;
            purchaseService.OnPurchasePending -= HandlePurchasePending;
            purchaseService.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
            purchaseService.OnPurchaseFailed -= HandlePurchaseFailed;
            purchaseService.OnPurchaseDeferred -= HandlePurchaseDeferred;
        }
        #endregion

        public void Purchase(string productId)
        {
            if (purchaseService == null)
                throw new InvalidOperationException("Purchase service not created. Connect first.");

            // Fail loudly here rather than passing null into Unity IAP.
            var product = productService?
                .GetProducts()?
                .FirstOrDefault(p => p.definition.id == productId)
                          ?? throw new InvalidOperationException($"Product '{productId}' is not in the fetched catalog.");

            purchaseService.PurchaseProduct(product);
        }

        public void Confirm(BackendOrder order)
        {
            if (order.NativeOrder is PendingOrder pending)
                purchaseService.ConfirmPurchase(pending);
            else
                IAPLog.Error("Confirm called with an order that is not a PendingOrder.");
        }

        public void Restore(Action<bool, string> done)
        {
            purchaseService.RestoreTransactions((success, error) => done(success, error));
        }

        private void HandlePurchasePending(PendingOrder order)
        {
            PurchasePending?.Invoke(ConvertUnityOrderToBackendOrder(order));
        }

        private void HandlePurchaseConfirmed(Order order)
        {
            // Per the package docs, OnPurchaseConfirmed can deliver a FailedOrder if confirmation fails.
            if (order is FailedOrder failed)
            {
                HandlePurchaseFailed(failed);
                return;
            }

            PurchaseConfirmed?.Invoke(ConvertUnityOrderToBackendOrder(order));
        }

        private void HandlePurchaseFailed(FailedOrder failed)
        {
            var mapped = ConvertUnityOrderToBackendOrder(failed);
            mapped.FailureStatus = failed.FailureReason == PurchaseFailureReason.UserCancelled
                ? PurchaseStatus.Cancelled
                : PurchaseStatus.Failed;
            mapped.Error = $"{failed.FailureReason}: {failed.Details}";
            PurchaseFailed?.Invoke(mapped);
        }

        private void HandlePurchaseDeferred(DeferredOrder deferred)
        {
            PurchaseDeferred?.Invoke(ConvertUnityOrderToBackendOrder(deferred));
        }

        #region Mapping
        private static BackendOrder ConvertUnityOrderToBackendOrder(Order order)
        {
            return new BackendOrder
            {
                ProductIds = order.CartOrdered.Items().Select(i => i.Product.definition.id).ToList(),
                TransactionId = order.Info?.TransactionID,
                Receipt = order.Info?.Receipt,
                NativeOrder = order
            };
        }

        private static ProductType ToProductType(IAPProductKind kind)
        {
            switch (kind)
            {
                case IAPProductKind.Consumable: 
                    return ProductType.Consumable;
                case IAPProductKind.NonConsumable: 
                    return ProductType.NonConsumable;
                case IAPProductKind.Subscription: 
                    return ProductType.Subscription;
                default: 
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static IAPProductKind ToKind(ProductType type)
        {
            switch (type)
            {
                case ProductType.NonConsumable: 
                    return IAPProductKind.NonConsumable;
                case ProductType.Subscription: 
                    return IAPProductKind.Subscription;
                default: 
                    return IAPProductKind.Consumable;
            }
        }
        #endregion
    }
}
