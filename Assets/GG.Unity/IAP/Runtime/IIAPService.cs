using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GG.Unity.IAP
{
    /// <summary>
    /// Public surface of the IAP service that UI layers (e.g. the Storefront) depend on.
    /// Implemented by IAPManagingService; tests and editor tooling can supply a fake.
    /// </summary>
    /// <remarks>
    /// Only change needed in existing code:
    ///     public sealed class IAPManagingService : IIAPService, IDisposable
    /// Every member below already exists on IAPManagingService with this exact signature.
    /// </remarks>
    public interface IIAPService
    {
        IAPState State { get; }
        bool IsReady { get; }
        IReadOnlyList<ProductInfo> Products { get; }

        event Action<IAPState> StateChanged;
        event Action ProductsUpdated;
        event Action<IReadOnlyList<string>> OwnedProductsFetched;
        event Action<PurchaseContext> PurchaseConfirmed;

        bool TryGetProduct(string productId, out ProductInfo product);
        Task<PurchaseResult> PurchaseAsync(string productId);
        Task<RestoreResult> RestoreAsync();
    }
}
