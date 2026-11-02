using System;
using System.Collections.Generic;
using GG.Unity.IAP;
using GG.Unity.UserInterface.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.IAP.Storefront
{
    /// <summary>Runtime arguments for a storefront panel.</summary>
    public sealed class StorefrontArgs
    {
        public StorefrontArgs(StorefrontModel model, IPanelService panels = null, string sectionId = null)
        {
            Model = model;
            Panels = panels;
            SectionId = sectionId;
        }

        public StorefrontModel Model { get; }

        /// <summary>Used to open the feedback dialog. Optional.</summary>
        public IPanelService Panels { get; }

        /// <summary>Show only this section. Null shows all.</summary>
        public string SectionId { get; }
    }

    internal sealed class StorefrontPanelController : IPanelController
    {
        private readonly StorefrontPanelDefinition definition;
        private readonly List<StoreItemView> items = new List<StoreItemView>();

        private Panel panel;
        private StorefrontArgs args;
        private Label status;
        private Button restore;

        private StorefrontTexts Texts => definition.Texts;

        public StorefrontPanelController(StorefrontPanelDefinition definition) => this.definition = definition;

        public void OnOpen(Panel panel)
        {
            this.panel = panel;
            args = panel.Args as StorefrontArgs;

            if (args?.Model == null || definition.Layout == null || definition.ItemTemplate == null)
            {
                Debug.LogError($"[GG.Storefront] '{definition.name}' needs StorefrontArgs with a model, " +
                               "a layout and an item template.", definition);
                panel.Close();
                return;
            }

            var root = panel.Root;
            status = root.Q<Label>("status");

            var close = root.Q<Button>("close");
            if (close != null)
                close.clicked += () => panel.Close();

            restore = root.Q<Button>("restore");
            if (restore != null)
            {
                restore.style.display = ShowRestoreButton() ? DisplayStyle.Flex : DisplayStyle.None;
                restore.clicked += OnRestoreClicked;
            }

            BuildSections(root.Q("sections") ?? root);

            args.Model.Changed += Refresh;
            Refresh();
        }

        public void OnClose(Panel panel)
        {
            if (args?.Model != null)
                args.Model.Changed -= Refresh;
            // A purchase still awaiting completion carries on in the model and IAP service;
            // the click handler checks panel.IsOpen before touching UI.
        }

        // ---------------------------------------------------------------- building

        private void BuildSections(VisualElement container)
        {
            foreach (var section in definition.Layout.Sections)
            {
                if (!string.IsNullOrEmpty(args.SectionId) && section.id != args.SectionId)
                    continue;

                var sectionRoot = new VisualElement();
                sectionRoot.AddToClassList("store-section");

                if (!string.IsNullOrEmpty(section.title))
                {
                    var header = new Label(section.title);
                    header.AddToClassList("store-section__title");
                    sectionRoot.Add(header);
                }

                var list = new VisualElement();
                list.AddToClassList("store-section__items");
                sectionRoot.Add(list);

                foreach (var item in section.items)
                {
                    var view = new StoreItemView(definition.ItemTemplate.Instantiate(), item, Texts);
                    view.BuyClicked += OnBuyClicked;
                    list.Add(view.Root);
                    items.Add(view);
                }

                container.Add(sectionRoot);
            }
        }

        private bool ShowRestoreButton()
        {
            switch (definition.RestoreButton)
            {
                case RestoreButtonMode.Always: return true;
                case RestoreButtonMode.ApplePlatformsOnly:
                    return Application.platform == RuntimePlatform.IPhonePlayer
                        || Application.platform == RuntimePlatform.OSXPlayer
                        || Application.platform == RuntimePlatform.tvOS
                        || Application.platform == RuntimePlatform.OSXEditor;
                default: return false;
            }
        }

        // ---------------------------------------------------------------- refresh

        private void Refresh()
        {
            if (panel == null || !panel.IsOpen)
                return;

            var model = args.Model;

            foreach (var view in items)
                view.Refresh(model);

            switch (model.IapState)
            {
                case IAPState.Ready: SetStatus(null); break;
                case IAPState.WaitingToRetry: SetStatus(Texts.unavailable); break;
                default: SetStatus(Texts.connecting); break;
            }

            restore?.SetEnabled(model.IsReady && !model.IsBusy);
        }

        private void SetStatus(string text)
        {
            if (status == null)
                return;

            status.text = text ?? string.Empty;
            status.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ---------------------------------------------------------------- actions

        private async void OnBuyClicked(string productId)
        {
            try
            {
                var result = await args.Model.PurchaseAsync(productId);
                if (panel.IsOpen)
                    ShowPurchaseOutcome(result);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async void OnRestoreClicked()
        {
            try
            {
                var result = await args.Model.RestoreAsync();
                if (!panel.IsOpen)
                    return;

                if (result.Success)
                    ShowMessage(Texts.restoreSuccessTitle, Texts.restoreSuccessMessage);
                else
                    ShowMessage(Texts.restoreFailedTitle, Texts.restoreFailedMessage);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ShowPurchaseOutcome(PurchaseResult result)
        {
            switch (result.Status)
            {
                case PurchaseStatus.Success:
                    ShowMessage(Texts.purchaseSuccessTitle, Texts.purchaseSuccessMessage);
                    break;

                case PurchaseStatus.Deferred:
                    ShowMessage(Texts.purchaseDeferredTitle, Texts.purchaseDeferredMessage);
                    break;

                case PurchaseStatus.Cancelled:
                case PurchaseStatus.Busy:
                    break; // user's own choice / button was already disabled: say nothing

                default:
                    Debug.LogWarning($"[GG.Storefront] Purchase of '{result.ProductId}' ended with " +
                                     $"{result.Status}: {result.Error}");
                    ShowMessage(Texts.purchaseFailedTitle, Texts.purchaseFailedMessage);
                    break;
            }
        }

        private void ShowMessage(string title, string message)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(message))
                return;

            if (definition.MessagePanel != null && args.Panels != null)
            {
                args.Panels.Open(definition.MessagePanel, new ConfirmArgs { Title = title, Message = message });
                return;
            }

            Debug.Log($"[GG.Storefront] {title}: {message}");
        }
    }
}
