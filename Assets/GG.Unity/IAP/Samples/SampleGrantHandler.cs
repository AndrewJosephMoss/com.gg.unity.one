using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GG.Unity.IAP.Samples
{
    public sealed class SampleGrantHandler : MonoBehaviour, IPurchaseGrantHandler
    {
        private const string GrantedKeyPrefix = "GG.IAP.Sample.Granted.";
        private const string OwnedKeyPrefix = "GG.IAP.Sample.Owned.";

        public Task<bool> GrantAsync(PurchaseContext context)
        {
            var key = GrantedKeyPrefix + TransactionKey(context);

            if (PlayerPrefs.HasKey(key))
            {
                // Already granted in an earlier session. Returning true lets the order be confirmed.
                Debug.Log($"[SampleGrantHandler] Transaction {Describe(context)} was already granted. Skipping.");
                return Task.FromResult(true);
            }

            foreach (var productId in context.ProductIds)
                Debug.Log($"[SampleGrantHandler] GRANT {productId} " +
                          $"(transaction: {Describe(context)}, user initiated: {context.IsUserInitiated})");

            // Record the grant and flush to disk BEFORE returning true, so it survives a crash
            // between here and the store confirmation.
            PlayerPrefs.SetString(key, System.DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();

            return Task.FromResult(true);
        }

        public void OnOwnedProductsFetched(IReadOnlyList<string> productIds)
        {
            // Called after a purchases fetch or a restore with products from already-confirmed orders.
            // Treat these as the source of truth for non-consumables/subscriptions and (re)apply
            // them idempotently. There is no transaction id here, so the key is the product id.
            bool changed = false;

            foreach (var productId in productIds)
            {
                var key = OwnedKeyPrefix + productId;
                if (PlayerPrefs.HasKey(key))
                {
                    Debug.Log($"[SampleGrantHandler] Owned {productId} (already entitled).");
                    continue;
                }

                Debug.Log($"[SampleGrantHandler] RESTORE {productId}");
                PlayerPrefs.SetInt(key, 1);
                changed = true;
            }

            if (changed)
                PlayerPrefs.Save();
        }

        // Mirrors IAPManagingService.KeyOf: fall back to product ids if the store gave no transaction id.
        private static string TransactionKey(PurchaseContext context) =>
            !string.IsNullOrEmpty(context.TransactionId)
                ? context.TransactionId
                : string.Join("|", context.ProductIds);

        private static string Describe(PurchaseContext context) =>
            string.IsNullOrEmpty(context.TransactionId) ? "<no transaction id>" : context.TransactionId;

#if UNITY_EDITOR
        [ContextMenu("Clear Sample Grant Records")]
        private void ClearRecords()
        {
            // PlayerPrefs can't enumerate keys, so this clears everything. Fine for a sample scene.
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[SampleGrantHandler] Cleared all PlayerPrefs.");
        }
#endif
    }
}
