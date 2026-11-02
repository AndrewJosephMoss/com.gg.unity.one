using System.Collections.Generic;
using System.Threading.Tasks;

namespace GG.Unity.IAP
{
    public interface IPurchaseGrantHandler
    {
        /// Grant the content for this order. Return true only once it is durably granted (saved).
        /// Must be idempotent per TransactionId: the same order can be delivered more than once.
        Task<bool> GrantAsync(PurchaseContext context);

        /// Re-grant non-consumables/subscriptions after a restore or purchases fetch.
        void OnOwnedProductsFetched(IReadOnlyList<string> productIds);
    }
}
