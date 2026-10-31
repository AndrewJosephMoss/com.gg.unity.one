using System;
using System.Threading;
using GG.Unity.UserInterface.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Samples
{
    internal sealed class ToastPanelController : IPanelController
    {
        private const float BaseOffset = 24f;
        private const float StackOffset = 64f;

        private readonly ToastPanelDefinition definition;
        private readonly CancellationTokenSource lifetime = new();

        public ToastPanelController(ToastPanelDefinition definition) => this.definition = definition;

        public void OnOpen(Panel panel)
        {
            var label = panel.Root.Q<Label>("text");
            if (label != null)
                label.text = panel.Args as string ?? definition.DefaultText;

            // Stack visible toasts upwards instead of drawing them on top of each other.
            var toast = panel.Root.Q("toast");
            if (toast != null && panel.Root.parent != null)
                toast.style.bottom = BaseOffset + StackOffset * panel.Root.parent.IndexOf(panel.Root);

            AutoClose(panel);
        }

        public void OnClose(Panel panel) => lifetime.Cancel();

        private async void AutoClose(Panel panel)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(definition.Duration, lifetime.Token);
            }
            catch (OperationCanceledException)
            {
                return; // closed early (e.g. Close all)
            }
            panel.Close();
        }
    }
}
