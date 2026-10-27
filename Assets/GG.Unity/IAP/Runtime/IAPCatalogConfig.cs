using System;
using System.Collections.Generic;
using UnityEngine;

namespace GG.Unity.IAP
{
    [CreateAssetMenu(menuName = "GG.Unity/IAP/Catalog Config", fileName = "IAPCatalogConfig")]
    public sealed class IAPCatalogConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Cross-store product id your game code uses.")]
            public string id;

            public IAPProductKind kind;

            [Tooltip("Optional. Leave empty to use 'id' on Google Play.")]
            public string googlePlayId;

            [Tooltip("Optional. Leave empty to use 'id' on the Apple App Store.")]
            public string appleAppStoreId;
        }

        [SerializeField] private List<Entry> products = new List<Entry>();

        public IReadOnlyList<Entry> Products => products;

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();
            foreach (var entry in products)
            {
                if (string.IsNullOrWhiteSpace(entry.id))
                {
                    Debug.LogWarning("[GG.IAP] Catalog entry with an empty id will be ignored.", this);
                    continue;
                }

                if (!seen.Add(entry.id))
                    Debug.LogWarning($"[GG.IAP] Duplicate catalog id '{entry.id}'.", this);
            }
        }
#endif
    }
}
