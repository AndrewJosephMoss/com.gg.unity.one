using System;
using System.Collections.Generic;
using System.Linq;
using GG.Unity.IAP;
using UnityEngine;

namespace GG.Unity.IAP.Storefront
{
    /// <summary>
    /// What a storefront shows and how: sections, order and presentation data. Kept separate from
    /// IAPCatalogConfig so the catalog stays a pure store definition, and so several storefronts
    /// (main shop, starter-pack popup, "remove ads" offer) can present the same catalog differently.
    /// </summary>
    [CreateAssetMenu(menuName = "GG.Unity/Storefront/Storefront Layout", fileName = "StorefrontLayout")]
    public sealed class StorefrontLayout : ScriptableObject
    {
        [Serializable]
        public sealed class Item
        {
            [Tooltip("Must match an id in the IAP catalog.")]
            public string productId;

            [Tooltip("Optional. Overrides the store's localized title (store titles are often unsuitable for display).")]
            public string title;

            [Tooltip("Optional. Overrides the store's localized description.")]
            [TextArea(1, 4)] public string description;

            public Sprite icon;

            [Tooltip("Optional ribbon text, e.g. 'Best value'.")]
            public string badge;

            [Tooltip("Hide the item once owned (e.g. 'Remove ads'). Otherwise it stays, marked as owned.")]
            public bool hideWhenOwned;

            [Tooltip("Optional extra USS class on the item root, e.g. 'store-item--featured'.")]
            public string styleClass;
        }

        [Serializable]
        public sealed class Section
        {
            [Tooltip("Used by StorefrontArgs.SectionId to open the store on just this section.")]
            public string id;
            public string title;
            public List<Item> items = new List<Item>();
        }

        [Tooltip("Editor validation only: product ids are checked against this catalog.")]
        [SerializeField] private IAPCatalogConfig catalog;

        [SerializeField] private List<Section> sections = new List<Section>();

        public IReadOnlyList<Section> Sections => sections;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (catalog == null)
                return;

            var known = new HashSet<string>(catalog.Products
                .Where(e => !string.IsNullOrWhiteSpace(e.id))
                .Select(e => e.id));

            foreach (var section in sections)
            foreach (var item in section.items)
            {
                if (string.IsNullOrWhiteSpace(item.productId))
                    Debug.LogWarning($"[GG.Storefront] Section '{section.id}' has an item with no product id.", this);
                else if (!known.Contains(item.productId))
                    Debug.LogWarning($"[GG.Storefront] '{item.productId}' is not in catalog '{catalog.name}'.", this);
            }
        }
#endif
    }
}
