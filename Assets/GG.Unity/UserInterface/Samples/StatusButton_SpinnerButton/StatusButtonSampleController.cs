using System;
using GG.Unity.UserInterface.Buttons;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Samples
{
    /// <summary>
    /// Wires the sample UI. State lives in <see cref="StatusButtonSampleData"/> and reaches the UI
    /// through the DataBindings declared in the UXML; this class only assigns the data source
    /// and hooks up clicks each time the PanelRenderer provides a (new) root.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class StatusButtonSampleController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float saveDuration = 2f;

        private readonly StatusButtonSampleData data = new();
        private PanelRenderer panelRenderer;

        public StatusButtonSampleData Data => data;

        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDestroy()
        {
            if (panelRenderer != null)
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            // Children inherit the data source; the new elements pull current values immediately,
            // so a Save in progress still shows as Loading after a rebuild.
            root.dataSource = data;

            // Clicks are events, not data, so they're wired per rebuild.
            // No unsubscribe needed: the old elements and their handlers are discarded together.
            Wire(root, "save-button", OnSaveClicked);
            Wire(root, "counter-button", () => data.ClickCount++);
            Wire(root, "reset-button", () => data.ClickCount = 0);

            // Shown for contrast: :disabled is UI Toolkit's own state, not a status.
            root.Q<Button>("disabled-button")?.SetEnabled(false);
        }

        private async void OnSaveClicked()
        {
            // The button blocks input while Loading, so this can't run twice at once.
            data.SaveStatus = ButtonStatus.Loading;
            data.SaveMessage = "Saving…";

            try
            {
                await Awaitable.WaitForSecondsAsync(saveDuration, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return; // Controller destroyed mid-save
            }

            data.SaveStatus = ButtonStatus.None;
            data.SaveMessage = $"Saved at {DateTime.Now:HH:mm:ss}";
        }

        private void Wire(VisualElement root, string elementName, Action onClick)
        {
            var button = root.Q<Button>(elementName);
            if (button == null)
            {
                Debug.LogWarning($"Button '{elementName}' not found in UXML", this);
                return;
            }
            button.clicked += onClick;
        }
    }
}
