using System;
using GG.Unity.UserInterface.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Samples
{
    /// <summary>
    /// Wires the launcher HUD (the PanelRenderer's own UXML) to the panel system and logs
    /// panel lifecycle events. Re-wires on every UI reload, like the StatusButton sample.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class PanelSampleController : MonoBehaviour
    {
        [SerializeField] private PanelManager panelManager;

        [Header("Panels")]
        [SerializeField] private PanelDefinition menuPanel;
        [SerializeField] private MessagePanelDefinition messagePanel;
        [SerializeField] private PanelDefinition confirmPanel;
        [SerializeField] private PanelDefinition toastPanel;

        private PanelRenderer panelRenderer;
        private IPanelService service;
        private Label resultLabel;
        private int toastCount;

        private void Awake()
        {
            panelRenderer = GetComponent<PanelRenderer>();
            if (panelManager == null)
                TryGetComponent(out panelManager);
        }

        private void OnEnable() => panelRenderer.RegisterUIReloadCallback(OnUIReload);

        private void OnDisable() => panelRenderer.UnregisterUIReloadCallback(OnUIReload);

        private void Start()
        {
            // PanelManager runs at execution order -100, so its service exists by now.
            service = panelManager != null ? panelManager.Service : null;
            if (service == null)
            {
                Debug.LogError("[PanelSample] No PanelManager service found.", this);
                return;
            }
            service.PanelOpened += OnPanelOpened;
            service.PanelClosed += OnPanelClosed;
        }

        private void OnDestroy()
        {
            if (service == null)
                return;
            service.PanelOpened -= OnPanelOpened;
            service.PanelClosed -= OnPanelClosed;
        }

        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            resultLabel = root.Q<Label>("last-result");

            Wire(root, "open-menu", () => Open(menuPanel));
            Wire(root, "show-message", () => Open(messagePanel));
            Wire(root, "show-message-args", () => Open(messagePanel,
                new MessageArgs($"This text was passed at runtime ({DateTime.Now:HH:mm:ss}).", "Runtime arguments")));
            Wire(root, "ask-confirm", AskConfirm);
            Wire(root, "show-toast", () => Open(toastPanel, $"Toast #{++toastCount}"));
            Wire(root, "back", () => Report(panelManager.CloseTopPanel()
                ? "Back: consumed by the panel stack"
                : "Back: nothing to close"));
            Wire(root, "close-all", () =>
            {
                panelManager.CloseAllPanels();
                Report("Closed all panels");
            });
        }

        /// <summary>Hooked to PanelBackInput's "unhandled back" event by the scene builder.</summary>
        public void OnBackUnhandled() =>
            Report("Back pressed with no closable panel. A game would open its pause menu here.");

        private async void AskConfirm()
        {
            try
            {
                bool confirmed = await panelManager.OpenPanelAsync<bool>(
                    confirmPanel,
                    new ConfirmArgs { Title = "Quit to title?", Message = "Unsaved progress will be lost.", ConfirmText = "Quit" },
                    destroyCancellationToken);

                Report(confirmed ? "Confirm dialog returned: true" : "Confirm dialog returned: false");
            }
            catch (OperationCanceledException)
            {
                // Scene unloaded while the dialog was open.
            }
        }

        private void Open(PanelDefinition definition, object args = null)
        {
            if (definition == null)
            {
                Report("That panel definition isn't assigned on PanelSampleController.");
                return;
            }
            panelManager.OpenPanel(definition, args);
        }

        private void OnPanelOpened(Panel panel) =>
            Debug.Log($"[PanelSample] Opened '{panel.Definition.name}' on {panel.Definition.Layer} (open: {service.OpenPanels.Count})");

        private void OnPanelClosed(Panel panel) =>
            Debug.Log($"[PanelSample] Closed '{panel.Definition.name}' (open: {service.OpenPanels.Count})");

        private void Report(string message)
        {
            Debug.Log($"[PanelSample] {message}");
            if (resultLabel != null)
                resultLabel.text = message;
        }

        private void Wire(VisualElement root, string elementName, Action onClick)
        {
            var button = root.Q<Button>(elementName);
            if (button == null)
            {
                Debug.LogWarning($"[PanelSample] Button '{elementName}' not found in UXML.", this);
                return;
            }
            button.clicked += onClick;
        }
    }
}
