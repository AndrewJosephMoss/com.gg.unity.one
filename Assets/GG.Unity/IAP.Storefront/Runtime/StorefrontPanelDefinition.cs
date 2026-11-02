using System;
using GG.Unity.UserInterface.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.IAP.Storefront
{
    public enum RestoreButtonMode { Never, ApplePlatformsOnly, Always }

    /// <summary>
    /// Storefront panel. Open with <see cref="StorefrontArgs"/> (StorefrontManager does this for you).
    /// Expected element names in the panel UXML: "sections" (ideally a ScrollView), and optionally
    /// "status", "restore", "close". The item template is described on <see cref="StoreItemView"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "StorefrontPanel", menuName = "GG.Unity/UserInterface/Panel Definitions/Storefront Panel")]
    public class StorefrontPanelDefinition : PanelDefinition
    {
        [Header("Storefront")]
        [SerializeField] private StorefrontLayout layout;
        [SerializeField] private VisualTreeAsset itemTemplate;
        [SerializeField] private RestoreButtonMode restoreButton = RestoreButtonMode.ApplePlatformsOnly;

        [Tooltip("Optional one-button dialog for purchase/restore feedback, e.g. a ConfirmPanelDefinition " +
                 "whose UXML has no 'cancel' button. Opened with ConfirmArgs.")]
        [SerializeField] private PanelDefinition messagePanel;

        [SerializeField] private StorefrontTexts texts = new StorefrontTexts();

        public StorefrontLayout Layout => layout;
        public VisualTreeAsset ItemTemplate => itemTemplate;
        public RestoreButtonMode RestoreButton => restoreButton;
        public PanelDefinition MessagePanel => messagePanel;
        public StorefrontTexts Texts => texts;

        public override IPanelController CreateController() => new StorefrontPanelController(this);
    }

    /// <summary>All user-facing strings in one place, ready to swap for LocalizedStrings later.</summary>
    [Serializable]
    public sealed class StorefrontTexts
    {
        public string connecting = "Connecting to store…";
        public string unavailable = "Store unavailable. Retrying…";
        public string priceUnknown = "–";
        public string owned = "Owned";
        public string awaitingApproval = "Pending approval";

        [Tooltip("Leave empty to show nothing on success (e.g. if a reward panel listens to PurchaseConfirmed).")]
        public string purchaseSuccessTitle = "Thank you!";
        public string purchaseSuccessMessage = "Your purchase is complete.";
        public string purchaseDeferredTitle = "Awaiting approval";
        public string purchaseDeferredMessage = "Your purchase will complete once it has been approved.";
        public string purchaseFailedTitle = "Purchase failed";
        public string purchaseFailedMessage = "The purchase could not be completed. You have not been charged.";
        public string restoreSuccessTitle = "Purchases restored";
        public string restoreSuccessMessage = "Your previous purchases have been restored.";
        public string restoreFailedTitle = "Restore failed";
        public string restoreFailedMessage = "Your purchases could not be restored. Please try again later.";
    }
}
