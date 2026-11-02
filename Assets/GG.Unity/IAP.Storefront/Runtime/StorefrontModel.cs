using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GG.Unity.IAP.Storefront
{
    public enum StoreItemState
    {
        /// <summary>IAP not ready, product not returned by the store, or not purchasable.</summary>
        Unavailable,
        Available,
        /// <summary>A purchase of this product, started through this model, is in progress.</summary>
        Purchasing,
        /// <summary>Deferred (e.g. Ask to Buy). Session-scoped: cleared when the order is confirmed.</summary>
        AwaitingApproval,
        /// <summary>Non-consumable or active subscription, per IOwnershipProvider.</summary>
        Owned
    }

    /// <summary>
    /// UI-facing store state, independent of any layout or panel. One instance can back several
    /// storefront panels and stand-alone buy buttons, and it outlives them, so a purchase started
    /// in a closed panel still shows as in progress when the panel is reopened.
    /// </summary>
    /// <remarks>Main thread only.</remarks>
    public sealed class StorefrontModel : IDisposable
    {
        private readonly IIAPService iap;
        private readonly IOwnershipProvider ownership;
        private readonly HashSet<string> awaitingApproval = new HashSet<string>();

        private string purchasingId;
        private bool restoring;
        private bool disposed;

        /// <summary>Raised whenever any item's state, product data or the IAP state may have changed.</summary>
        public event Action Changed;

        public StorefrontModel(IIAPService iap, IOwnershipProvider ownership = null)
        {
            this.iap = iap ?? throw new ArgumentNullException(nameof(iap));
            this.ownership = ownership;

            iap.StateChanged += OnStateChanged;
            iap.ProductsUpdated += RaiseChanged;
            iap.OwnedProductsFetched += OnOwnedProductsFetched;
            iap.PurchaseConfirmed += OnPurchaseConfirmed;

            if (ownership != null)
                ownership.OwnershipChanged += RaiseChanged;
        }

        public IAPState IapState => iap.State;
        public bool IsReady => iap.IsReady;

        /// <summary>True while a purchase or restore started through this model is running.</summary>
        public bool IsBusy => purchasingId != null || restoring;
        public bool IsRestoring => restoring;

        public bool TryGetProduct(string productId, out ProductInfo product) =>
            iap.TryGetProduct(productId, out product);

        public StoreItemState GetState(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return StoreItemState.Unavailable;

            if (productId == purchasingId)
                return StoreItemState.Purchasing;

            // Checked before the store: ownership comes from save data and is valid offline.
            if (ownership != null && ownership.IsOwned(productId))
                return StoreItemState.Owned;

            if (awaitingApproval.Contains(productId))
                return StoreItemState.AwaitingApproval;

            if (!iap.IsReady || !iap.TryGetProduct(productId, out var product) || !product.AvailableToPurchase)
                return StoreItemState.Unavailable;

            return StoreItemState.Available;
        }

        public async Task<PurchaseResult> PurchaseAsync(string productId)
        {
            if (disposed)
                return new PurchaseResult(PurchaseStatus.NotReady, productId);

            if (IsBusy)
                return new PurchaseResult(PurchaseStatus.Busy, productId);

            if (GetState(productId) == StoreItemState.Owned)
                return new PurchaseResult(PurchaseStatus.Failed, productId, error: "Already owned.");

            purchasingId = productId;
            RaiseChanged();

            PurchaseResult result;
            try
            {
                result = await iap.PurchaseAsync(productId);
            }
            finally
            {
                purchasingId = null;
            }

            if (result.Status == PurchaseStatus.Deferred)
                awaitingApproval.Add(productId);

            RaiseChanged();
            return result;
        }

        public async Task<RestoreResult> RestoreAsync()
        {
            if (disposed || !iap.IsReady)
                return new RestoreResult(false, "IAP is not ready.");

            if (IsBusy)
                return new RestoreResult(false, "Another store operation is in progress.");

            restoring = true;
            RaiseChanged();

            try
            {
                return await iap.RestoreAsync();
            }
            finally
            {
                restoring = false;
                RaiseChanged();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            iap.StateChanged -= OnStateChanged;
            iap.ProductsUpdated -= RaiseChanged;
            iap.OwnedProductsFetched -= OnOwnedProductsFetched;
            iap.PurchaseConfirmed -= OnPurchaseConfirmed;

            if (ownership != null)
                ownership.OwnershipChanged -= RaiseChanged;

            Changed = null;
        }

        // ---------------------------------------------------------------- IAP events

        private void OnStateChanged(IAPState _) => RaiseChanged();

        // The grant handler re-grants on fetch/restore; ownership may have changed even if
        // the provider doesn't raise OwnershipChanged.
        private void OnOwnedProductsFetched(IReadOnlyList<string> _) => RaiseChanged();

        // Covers unsolicited confirmations too: approved Ask-to-Buy, interrupted purchases, etc.
        private void OnPurchaseConfirmed(PurchaseContext context)
        {
            foreach (var id in context.ProductIds)
                awaitingApproval.Remove(id);

            RaiseChanged();
        }

        private void RaiseChanged()
        {
            if (!disposed)
                Changed?.Invoke();
        }
    }
}
