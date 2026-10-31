using System;
using System.Runtime.CompilerServices;
using GG.Unity.UserInterface.Buttons;
using Unity.Properties;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Samples
{
    /// <summary>
    /// Per-instance data source for the sample menu panel. The StatusButtons bind their
    /// Status and text to these properties in SampleMenuPanel.uxml.
    /// </summary>
    public sealed class SampleMenuModel : INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        private ButtonStatus saveStatus;
        [CreateProperty]
        public ButtonStatus SaveStatus
        {
            get => saveStatus;
            set => Set(ref saveStatus, value);
        }

        private string saveText = "Save";
        [CreateProperty]
        public string SaveText
        {
            get => saveText;
            set => Set(ref saveText, value);
        }

        private ButtonStatus syncStatus;
        [CreateProperty]
        public ButtonStatus SyncStatus
        {
            get => syncStatus;
            set => Set(ref syncStatus, value);
        }

        private string syncText = "Sync to cloud";
        [CreateProperty]
        public string SyncText
        {
            get => syncText;
            set => Set(ref syncText, value);
        }

        private void Set<T>(ref T field, T value, [CallerMemberName] string property = "")
        {
            if (Equals(field, value))
                return;
            field = value;
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
