using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GG.Unity.IAP
{
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
