using System;
using System.Threading;
using GG.Unity.UserInterface.Buttons;
using GG.Unity.UserInterface.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Samples
{
    /// <summary>
    /// Behaviour for one open sample menu. Shows plain Debug.Log buttons, StatusButtons driven
    /// through data binding, a nested awaited confirm dialog, and toasts opened from a controller.
    /// </summary>
    internal sealed class SampleMenuController : IPanelController
    {
        private readonly SampleMenuPanelDefinition definition;
        private readonly SampleMenuModel model = new();
        private readonly CancellationTokenSource lifetime = new();
        private Panel panel;

        public SampleMenuController(SampleMenuPanelDefinition definition) => this.definition = definition;

        public void OnOpen(Panel panel)
        {
            this.panel = panel;
            panel.Root.dataSource = model; // StatusButtons bind to SaveStatus/SaveText etc.

            Wire("play", () => Debug.Log("[Menu] Play clicked"));
            Wire("settings", () => Debug.Log("[Menu] Settings clicked"));
            Wire("help", () => Debug.Log("[Menu] Help clicked"));
            Wire("save", Save);
            Wire("sync", Sync);
            Wire("delete-save", DeleteSave);

            Debug.Log("[Menu] Opened");
        }

        public void OnClose(Panel panel)
        {
            lifetime.Cancel(); // stops any in-flight save/sync/confirm
            Debug.Log($"[Menu] Closed (result: {panel.Result ?? "none"})");
        }

        private async void Save()
        {
            // The StatusButton blocks input while Loading, so this can't overlap itself.
            model.SaveStatus = ButtonStatus.Loading;
            model.SaveText = "Saving…";
            Debug.Log("[Menu] Save started");

            if (!await Delay(definition.SaveDuration))
                return;

            model.SaveStatus = ButtonStatus.None;
            model.SaveText = "Save";
            Debug.Log("[Menu] Save finished");
            ShowToast("Game saved");
        }

        private async void Sync()
        {
            model.SyncStatus = ButtonStatus.Loading;
            model.SyncText = "Syncing…";
            Debug.Log("[Menu] Sync started");

            if (!await Delay(definition.SyncDuration))
                return;

            model.SyncStatus = ButtonStatus.None;
            model.SyncText = "Sync to cloud";
            Debug.Log("[Menu] Sync finished");
            ShowToast("Cloud sync complete");
        }

        private async void DeleteSave()
        {
            if (definition.ConfirmPanel == null)
            {
                Debug.LogWarning("[Menu] No confirm panel assigned.");
                return;
            }

            Debug.Log("[Menu] Asking for confirmation…");
            try
            {
                bool confirmed = await panel.Service.OpenAsync<bool>(
                    definition.ConfirmPanel,
                    new ConfirmArgs
                    {
                        Title = "Delete save?",
                        Message = "This removes your save file and can't be undone.",
                        ConfirmText = "Delete",
                        CancelText = "Keep it"
                    },
                    lifetime.Token);

                Debug.Log(confirmed ? "[Menu] Save deleted" : "[Menu] Delete cancelled");
                if (confirmed)
                    ShowToast("Save deleted");
            }
            catch (OperationCanceledException)
            {
                // Menu closed while the dialog was open; the dialog was closed with it.
            }
        }

        private void ShowToast(string text)
        {
            if (definition.ToastPanel != null && panel.IsOpen)
                panel.Service.Open(definition.ToastPanel, text);
        }

        private async Awaitable<bool> Delay(float seconds)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(seconds, lifetime.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false; // panel closed mid-operation
            }
        }

        private void Wire(string elementName, Action onClick)
        {
            var button = panel.Root.Q<Button>(elementName);
            if (button == null)
            {
                Debug.LogWarning($"[Menu] Button '{elementName}' not found in UXML.");
                return;
            }
            button.clicked += onClick;
        }
    }
}
