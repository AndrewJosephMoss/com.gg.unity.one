using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GG.Unity.IAP.Samples
{
    /// <summary>
    /// Test-scene grant handler. Persists grants to PlayerPrefs (one JSON blob), is idempotent per
    /// TransactionId, and can be told to decline, throw or stall the next order so the wrapper's
    /// failure paths can be exercised from the IAPSampleController inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SampleGrantHandler : MonoBehaviour, IPurchaseGrantHandler
    {
        public enum GrantBehaviour
        {
            Grant,
            Decline,
            Throw
        }

        [Tooltip("Applies to the next order only, then resets to Grant.")]
        [SerializeField] private GrantBehaviour nextOrder = GrantBehaviour.Grant;

        [Tooltip("Simulates a slow save or server round trip before the grant completes.")]
        [SerializeField, Min(0f)] private float grantDelaySeconds = 0.5f;

        public GrantBehaviour NextOrder
        {
            get => nextOrder;
            set => nextOrder = value;
        }

        public float GrantDelaySeconds
        {
            get => grantDelaySeconds;
            set => grantDelaySeconds = Mathf.Max(0f, value);
        }

        /// <summary>Raised whenever the saved state changes.</summary>
        public static event Action Changed;

        // ---------------------------------------------------------------- IPurchaseGrantHandler

        public async Task<bool> GrantAsync(PurchaseContext context)
        {
            // Consume the switch up front so a re-delivery of the same order behaves normally.
            var behaviour = nextOrder;
            nextOrder = GrantBehaviour.Grant;

            if (grantDelaySeconds > 0f)
                await Task.Delay(TimeSpan.FromSeconds(grantDelaySeconds));

            switch (behaviour)
            {
                case GrantBehaviour.Decline:
                    Debug.Log($"[IAP Sample] Declining {Describe(context)} (test switch).");
                    return false;

                case GrantBehaviour.Throw:
                    throw new InvalidOperationException("Grant handler threw (test switch).");
            }

            var save = SampleSave.Current;

            if (!string.IsNullOrEmpty(context.TransactionId) && save.transactions.Contains(context.TransactionId))
            {
                Debug.Log($"[IAP Sample] Already granted {Describe(context)}. Acknowledging re-delivery.");
                return true;
            }

            foreach (var productId in context.ProductIds)
            {
                save.AddGrant(productId);

                if (IsDurable(productId))
                    save.MarkOwned(productId);
            }

            if (!string.IsNullOrEmpty(context.TransactionId))
                save.transactions.Add(context.TransactionId);
            else
                Debug.LogWarning("[IAP Sample] Order has no TransactionId, so it cannot be de-duplicated.");

            // Persist BEFORE returning true: true means "safe to confirm with the store".
            save.Save();

            Debug.Log($"[IAP Sample] Granted {Describe(context)}.");
            Changed?.Invoke();
            return true;
        }

        public void OnOwnedProductsFetched(IReadOnlyList<string> productIds)
        {
            var save = SampleSave.Current;
            foreach (var productId in productIds)
                save.MarkOwned(productId);
            save.Save();

            Debug.Log($"[IAP Sample] Owned products fetched: [{string.Join(", ", productIds)}]");
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- queries for the sample inspector

        public static int GetGrantCount(string productId) => SampleSave.Current.GetCount(productId);

        public static bool IsOwned(string productId) => SampleSave.Current.owned.Contains(productId);

        public static int TransactionCount => SampleSave.Current.transactions.Count;

        public static void ResetSave()
        {
            SampleSave.Delete();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- helpers

        private static bool IsDurable(string productId)
        {
            var manager = IAPManager.Instance;
            if (manager == null || manager.Service == null)
                return false;

            return manager.Service.TryGetProduct(productId, out var product)
                   && product.Kind != IAPProductKind.Consumable;
        }

        private static string Describe(PurchaseContext context) =>
            $"[{string.Join(", ", context.ProductIds)}] txn {context.TransactionId ?? "-"} " +
            $"(user-initiated: {context.IsUserInitiated})";
    }

    /// <summary>All sample state in one PlayerPrefs key, so it can be wiped in one go.</summary>
    [Serializable]
    internal sealed class SampleSave
    {
        private const string Key = "GG.IAP.Sample.Save";

        [Serializable]
        public sealed class GrantCount
        {
            public string productId;
            public int grants;
        }

        public List<string> transactions = new List<string>();
        public List<GrantCount> counts = new List<GrantCount>();
        public List<string> owned = new List<string>();

        private static SampleSave cached;

        public static SampleSave Current
        {
            get
            {
                if (cached != null)
                    return cached;

                var json = PlayerPrefs.GetString(Key, string.Empty);
                cached = string.IsNullOrEmpty(json) ? new SampleSave() : JsonUtility.FromJson<SampleSave>(json);
                return cached;
            }
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            cached = null;
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public int GetCount(string productId)
        {
            foreach (var c in counts)
                if (c.productId == productId)
                    return c.grants;
            return 0;
        }

        public void AddGrant(string productId)
        {
            foreach (var c in counts)
            {
                if (c.productId != productId)
                    continue;
                c.grants++;
                return;
            }

            counts.Add(new GrantCount { productId = productId, grants = 1 });
        }

        public void MarkOwned(string productId)
        {
            if (!owned.Contains(productId))
                owned.Add(productId);
        }
    }
}
