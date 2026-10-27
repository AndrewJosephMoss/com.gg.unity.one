using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GG.Unity.IAP
{
    /// <summary>
    /// Everything IAPManager needs from the underlying IAP package, expressed in our own types.
    /// UnityIapBackend is the only implementation that touches UnityEngine.Purchasing.
    /// </summary>
    internal interface IIapBackend : IDisposable
    {
        /// <summary>Raised when the store connection drops. Argument is a human-readable reason.</summary>
        event Action<string> Disconnected;

        event Action<BackendOrder> PurchasePending;
        event Action<BackendOrder> PurchaseConfirmed;
        event Action<BackendOrder> PurchaseFailed;
        event Action<BackendOrder> PurchaseDeferred;

        /// <summary>Product ids of already-confirmed orders returned by a purchases fetch or restore.</summary>
        event Action<IReadOnlyList<string>> OwnedProductsFetched;

        IReadOnlyList<ProductInfo> Products { get; }

        /// <summary>Connects to the store. Throws if the connection fails.</summary>
        Task ConnectAsync();

        /// <summary>Fetches the configured catalog. Throws on failure or timeout.</summary>
        Task FetchProductsAsync(float timeoutSeconds);

        void FetchPurchases();

        bool CheckProductExists(string productId);

        void Purchase(string productId);

        void Confirm(BackendOrder order);

        void Restore(Action<bool, string> done);
    }

    internal sealed class BackendOrder
    {
        public IReadOnlyList<string> ProductIds { get; set; } = Array.Empty<string>();
        public string TransactionId { get; set; }
        public string Receipt { get; set; }

        /// <summary>The package's own order object. Opaque to everything except the backend.</summary>
        public object NativeOrder { get; set; }

        // Only meaningful for failed orders.
        public PurchaseStatus FailureStatus { get; set; } = PurchaseStatus.Failed;
        public string Error { get; set; }
    }
}