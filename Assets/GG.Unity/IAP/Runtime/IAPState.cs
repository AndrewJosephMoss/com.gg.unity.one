using System.Collections.Generic;
using System.Threading.Tasks;

namespace GG.Unity.IAP
{
    public enum IAPState
    {
        Uninitialised,
        Initialising,
        WaitingToRetry,
        Ready
    }

    public enum IAPProductKind
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    public enum PurchaseStatus
    {
        Success,
        Cancelled,
        Failed,
        Deferred,
        NotReady,
        UnknownProduct,
        Busy
    }

    /// <summary>Immutable snapshot of a store product. Safe to cache and bind to UI.</summary>
    public sealed class ProductInfo
    {
        public string Id { get; }
        public IAPProductKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public string LocalisedPrice { get; }
        public decimal Price { get; }
        public string CurrencyCode { get; }
        public bool AvailableToPurchase { get; }

        public ProductInfo(
            string id,
            IAPProductKind kind,
            string title,
            string description,
            string localisedPrice,
            decimal price,
            string currencyCode,
            bool availableToPurchase)
        {
            Id = id;
            Kind = kind;
            Title = title;
            Description = description;
            LocalisedPrice = localisedPrice;
            Price = price;
            CurrencyCode = currencyCode;
            AvailableToPurchase = availableToPurchase;
        }
    }

    public readonly struct PurchaseResult
    {
        public PurchaseStatus Status { get; }
        public string ProductId { get; }
        public string TransactionId { get; }
        public string Error { get; }

        public bool IsSuccess => Status == PurchaseStatus.Success;

        public PurchaseResult(PurchaseStatus status, string productId, string transactionId = null, string error = null)
        {
            Status = status;
            ProductId = productId;
            TransactionId = transactionId;
            Error = error;
        }

        public override string ToString() =>
            $"{Status} (product: {ProductId}, txn: {TransactionId ?? "-"}{(Error != null ? ", error: " + Error : string.Empty)})";
    }

    public readonly struct RestoreResult
    {
        public bool Success { get; }
        public string Error { get; }

        public RestoreResult(bool success, string error)
        {
            Success = success;
            Error = error;
        }
    }

    /// <summary>
    /// Describes a paid order that is waiting for the game to deliver it.
    /// </summary>
    public sealed class PurchaseContext
    {
        public IReadOnlyList<string> ProductIds { get; }
        public string TransactionId { get; }
        public string Receipt { get; }

        /// <summary>
        /// True when this order answers a PurchaseAsync call made in this session.
        /// False for restored, interrupted, deferred-then-approved, or externally made purchases.
        /// </summary>
        public bool IsUserInitiated { get; }

        public PurchaseContext(IReadOnlyList<string> productIds, string transactionId, string receipt, bool isUserInitiated)
        {
            ProductIds = productIds;
            TransactionId = transactionId;
            Receipt = receipt;
            IsUserInitiated = isUserInitiated;
        }
    }

    /// <summary>
    /// Grant the entitlement and PERSIST it, then return true. Returning true makes the wrapper
    /// confirm the purchase with the store. Return false (or throw) to leave it unconfirmed so
    /// Unity IAP re-delivers it later.
    /// Must be idempotent: the same transaction can be delivered more than once.
    /// </summary>
    public delegate Task<bool> PurchaseGrantHandler(PurchaseContext context);
}
