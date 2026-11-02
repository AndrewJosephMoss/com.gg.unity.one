using System;
using System.Collections.Generic;
using System.Linq;
using GG.Unity.UserInterface.Buttons;
using UnityEngine.UIElements;

namespace GG.Unity.IAP.Storefront
{
    /// <summary>
    /// One product card, built from the item template. All elements are optional:
    ///   "icon" (VisualElement, background image), "title", "description", "badge" (Labels),
    ///   "buy" (Button; a StatusButton gets the Loading status while purchasing).
    /// The root gets "store-item" plus one state class, e.g. "store-item--owned".
    /// </summary>
    public sealed class StoreItemView
    {
        public const string BaseClass = "store-item";

        private static readonly Dictionary<StoreItemState, string> StateClasses =
            Enum.GetValues(typeof(StoreItemState))
                .Cast<StoreItemState>()
                .ToDictionary(s => s, s => $"{BaseClass}--{ToKebab(s.ToString())}");

        private readonly StorefrontLayout.Item item;
        private readonly StorefrontTexts texts;

        private readonly Label title;
        private readonly Label description;
        private readonly Label badge;
        private readonly Button buy;
        private readonly StatusButton statusButton;

        private StoreItemState? appliedState;

        public VisualElement Root { get; }
        public string ProductId => item.productId;

        public event Action<string> BuyClicked;

        public StoreItemView(VisualElement root, StorefrontLayout.Item item, StorefrontTexts texts)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            this.item = item ?? throw new ArgumentNullException(nameof(item));
            this.texts = texts ?? new StorefrontTexts();

            Root.AddToClassList(BaseClass);
            if (!string.IsNullOrWhiteSpace(item.styleClass))
                Root.AddToClassList(item.styleClass);

            title = Root.Q<Label>("title");
            description = Root.Q<Label>("description");
            badge = Root.Q<Label>("badge");
            buy = Root.Q<Button>("buy");
            statusButton = buy as StatusButton;

            var icon = Root.Q("icon");
            if (icon != null)
            {
                if (item.icon != null)
                    icon.style.backgroundImage = new StyleBackground(item.icon);
                else
                    icon.style.display = DisplayStyle.None;
            }

            SetText(badge, item.badge);

            if (buy != null)
                buy.clicked += () => BuyClicked?.Invoke(item.productId);
        }

        public void Refresh(StorefrontModel model)
        {
            var state = model.GetState(item.productId);
            model.TryGetProduct(item.productId, out var product);

            // Hide items the store didn't return once we know (IAP ready), and owned ones if asked.
            bool hidden = (state == StoreItemState.Owned && item.hideWhenOwned)
                       || (model.IsReady && product == null && state != StoreItemState.Owned);

            Root.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
            if (hidden)
                return;

            SetText(title, Pick(item.title, product?.Title));
            SetText(description, Pick(item.description, product?.Description));

            if (buy != null)
            {
                buy.text = state switch
                {
                    StoreItemState.Owned => texts.owned,
                    StoreItemState.AwaitingApproval => texts.awaitingApproval,
                    _ => Pick(product?.LocalisedPrice, texts.priceUnknown)
                };

                if (statusButton != null)
                    statusButton.Status = state == StoreItemState.Purchasing ? ButtonStatus.Loading : ButtonStatus.None;

                // A StatusButton blocks its own input while Loading, so leave it enabled to keep the
                // loading look. Everything else is disabled while any store operation runs.
                bool enabled = (state == StoreItemState.Available && !model.IsBusy)
                            || (state == StoreItemState.Purchasing && statusButton != null);
                buy.SetEnabled(enabled);
            }

            ApplyStateClass(state);
        }

        private void ApplyStateClass(StoreItemState state)
        {
            if (appliedState == state)
                return;

            if (appliedState.HasValue)
                Root.RemoveFromClassList(StateClasses[appliedState.Value]);

            Root.AddToClassList(StateClasses[state]);
            appliedState = state;
        }

        private static void SetText(Label label, string text)
        {
            if (label == null)
                return;

            label.text = text ?? string.Empty;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static string Pick(string preferred, string fallback) =>
            string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

        private static string ToKebab(string pascal) =>
            string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + char.ToLowerInvariant(c)
                                                                              : char.ToLowerInvariant(c).ToString()));
    }
}
