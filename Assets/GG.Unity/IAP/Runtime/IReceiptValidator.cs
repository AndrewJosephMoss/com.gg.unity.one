using System.Threading.Tasks;

namespace GG.Unity.IAP
{
    /// <summary>
    /// Runs before the grant handler. Return false to reject the order: it is neither granted
    /// nor confirmed. Plug in client-side (CrossPlatformValidator) or server-side validation here.
    /// </summary>
    public interface IReceiptValidator
    {
        Task<bool> ValidateAsync(PurchaseContext context);
    }

    /// <summary>Default. Accepts everything. Replace before shipping anything valuable.</summary>
    public sealed class AcceptAllReceiptValidator : IReceiptValidator
    {
        public Task<bool> ValidateAsync(PurchaseContext context) => Task.FromResult(true);
    }
}
