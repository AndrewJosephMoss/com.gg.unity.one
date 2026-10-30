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

        private TaskCompletionSource<bool> fetchTcs;
        private string fetchError;
        private string connectFailure;
        private IReadOnlyList<ProductInfo> products = Array.Empty<ProductInfo>();

        public event Action<string> Disconnected;
        public event Action<BackendOrder> PurchasePending;
        public event Action<BackendOrder> PurchaseConfirmed;
        public event Action<BackendOrder> PurchaseFailed;
        public event Action<BackendOrder> PurchaseDeferred;
        public event Action<IReadOnlyList<string>> OwnedProductsFetched;

        public IReadOnlyList<ProductInfo> Products => products;

        public UnityIapBackend(IAPCatalogConfig config)
        {
            this.config = config;
        }

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

        public async Task FetchProductsAsync(float timeoutSeconds)
        {
            EnsureServices();

            fetchError = null;
            fetchTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // No Unity-side retries: IAPManager owns the retry policy via BackOffTimer.
            catalogProvider.FetchProducts(
                productService.FetchProductsWithNoRetries,
                DefaultStoreHelper.GetDefaultStoreName());

            var finished = await Task.WhenAny(fetchTcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));

            if (finished != fetchTcs.Task)
                throw new TimeoutException($"Product fetch timed out after {timeoutSeconds:0.#}s.");

            if (!fetchTcs.Task.Result)
                throw new InvalidOperationException("Product fetch failed: " + (fetchError ?? "unknown reason"));
        }

        public void FetchPurchases()
        {
            purchaseService?.FetchPurchases();
        }

        // ---------------------------------------------------------------- purchasing

        public bool CheckProductExists(string productId)
        {
            var product = FindProduct(productId);
            if (product == null)
                return false;
            return true;
        }

        public void Purchase(string productId)
        {
            var product = FindProduct(productId);
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

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            fetchTcs?.TrySetCanceled();

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

        // ---------------------------------------------------------------- setup

        private void EnsureServices()
        {
            if (servicesCreated)
                return;

            servicesCreated = true;

            // TODO What are the default stores?
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
                    { Pick(entry.googlePlayId, entry.id), UnityEngine.Purchasing.GooglePlay.Name },
                    { Pick(entry.appleAppStoreId, entry.id), UnityEngine.Purchasing.AppleAppStore.Name }
                };
            }

            catalogProvider.AddProducts(definitions, storeSpecific);
        }

        // ---------------------------------------------------------------- store / product events

        private void HandleStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            var reason = failure?.ToString() ?? "unknown";
            connectFailure = reason;
            Disconnected?.Invoke(reason);
        }

        private void HandleProductsFetched(List<Product> fetched)
        {
            products = BuildSnapshot();

            var present = new HashSet<string>(products.Select(p => p.Id));
            var missing = config.Products
                .Where(e => !string.IsNullOrWhiteSpace(e.id) && !present.Contains(e.id))
                .Select(e => e.id)
                .ToList();

            if (missing.Count > 0)
                IAPLog.Warn("Configured products not returned by the store: " + string.Join(", ", missing));

            fetchTcs?.TrySetResult(true);
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failure)
        {
            fetchError = failure?.ToString();
            fetchTcs?.TrySetResult(false);
        }

        // ---------------------------------------------------------------- purchase events

        private void HandlePurchasesFetched(Orders orders)
        {
            var owned = orders.ConfirmedOrders
                .SelectMany(o => o.CartOrdered.Items().Select(i => i.Product.definition.id))
                .Distinct()
                .ToList();

            OwnedProductsFetched?.Invoke(owned);
        }

        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            IAPLog.Warn("Fetching existing purchases failed: " + failure);
        }

        private void HandlePurchasePending(PendingOrder order)
        {
            PurchasePending?.Invoke(Map(order));
        }

        private void HandlePurchaseConfirmed(Order order)
        {
            // Per the package docs, OnPurchaseConfirmed can deliver a FailedOrder if confirmation fails.
            if (order is FailedOrder failed)
            {
                HandlePurchaseFailed(failed);
                return;
            }

            PurchaseConfirmed?.Invoke(Map(order));
        }

        private void HandlePurchaseFailed(FailedOrder failed)
        {
            var mapped = Map(failed);
            mapped.FailureStatus = failed.FailureReason == PurchaseFailureReason.UserCancelled
                ? PurchaseStatus.Cancelled
                : PurchaseStatus.Failed;
            mapped.Error = $"{failed.FailureReason}: {failed.Details}";
            PurchaseFailed?.Invoke(mapped);
        }

        private void HandlePurchaseDeferred(DeferredOrder deferred)
        {
            PurchaseDeferred?.Invoke(Map(deferred));
        }

        // ---------------------------------------------------------------- mapping

        private Product FindProduct(string productId)
        {
            return productService?.GetProducts()?.FirstOrDefault(p => p.definition.id == productId);
        }

        private IReadOnlyList<ProductInfo> BuildSnapshot()
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

        private static BackendOrder Map(Order order)
        {
            return new BackendOrder
            {
                ProductIds = order.CartOrdered.Items().Select(i => i.Product.definition.id).ToList(),
                TransactionId = order.Info?.TransactionID,
                Receipt = order.Info?.Receipt,
                NativeOrder = order
            };
        }

        private static string Pick(string overrideId, string fallback) =>
            string.IsNullOrWhiteSpace(overrideId) ? fallback : overrideId;

        private static ProductType ToProductType(IAPProductKind kind)
        {
            switch (kind)
            {
                case IAPProductKind.Consumable: return ProductType.Consumable;
                case IAPProductKind.NonConsumable: return ProductType.NonConsumable;
                case IAPProductKind.Subscription: return ProductType.Subscription;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static IAPProductKind ToKind(ProductType type)
        {
            switch (type)
            {
                case ProductType.NonConsumable: return IAPProductKind.NonConsumable;
                case ProductType.Subscription: return IAPProductKind.Subscription;
                default: return IAPProductKind.Consumable;
            }
        }
    }
}
