using System;
using System.Runtime.CompilerServices;
using GG.Unity.UserInterface.Buttons;
using Unity.Properties;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Samples
{
    /// <summary>
    /// Data source for the sample. This is the source of truth: the UI only displays it,
    /// so a rebuilt tree shows the right state as soon as the new root gets this dataSource.
    /// </summary>
    public sealed class StatusButtonSampleData : INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        // --- Save demo -------------------------------------------------------

        private ButtonStatus saveStatus;
        [CreateProperty]
        public ButtonStatus SaveStatus
        {
            get => saveStatus;
            set => Set(ref saveStatus, value);
        }

        private string saveMessage = "Not saved yet";
        [CreateProperty]
        public string SaveMessage
        {
            get => saveMessage;
            set => Set(ref saveMessage, value);
        }

        // --- Blocking demo ---------------------------------------------------

        private bool counterBusy;
        /// <summary>Two-way bound to a Toggle; drives CounterStatus.</summary>
        [CreateProperty]
        public bool CounterBusy
        {
            get => counterBusy;
            set
            {
                if (!Set(ref counterBusy, value))
                    return;
                CounterStatus = value ? ButtonStatus.Loading : ButtonStatus.None;
            }
        }

        private ButtonStatus counterStatus;
        [CreateProperty]
        public ButtonStatus CounterStatus
        {
            get => counterStatus;
            private set => Set(ref counterStatus, value);
        }

        private int clickCount;
        [CreateProperty]
        public int ClickCount
        {
            get => clickCount;
            set
            {
                if (Set(ref clickCount, value))
                    Notify(nameof(ClickCountText));
            }
        }

        /// <summary>Label.text is a string, so expose a formatted version instead of using a converter.</summary>
        [CreateProperty]
        public string ClickCountText => $"Clicks: {clickCount}";

        // --- Helpers ---------------------------------------------------------

        private bool Set<T>(ref T field, T value, [CallerMemberName] string property = "")
        {
            if (Equals(field, value))
                return false;
            field = value;
            Notify(property);
            return true;
        }

        private void Notify(string property) =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
    }
}
