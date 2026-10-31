using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Buttons
{
    public enum ButtonStatus { None, Loading }

    [UxmlElement]
    public partial class StatusButton : Button
    {
        public const string BaseClass = "status-button";
        public const string ButtonStatusClassFormat = BaseClass + "--{0}";
        public static readonly BindingId StatusProperty = (BindingId)nameof(Status);

        private static readonly Dictionary<ButtonStatus, string> statusClasses =
            Enum.GetValues(typeof(ButtonStatus))
                .Cast<ButtonStatus>()
                .ToDictionary(
                    status => status,
                    status => string.Format(ButtonStatusClassFormat, status.ToString().ToLowerInvariant()));

        public static IReadOnlyDictionary<ButtonStatus, string> StatusClasses => statusClasses;

        private ButtonStatus status;
        [UxmlAttribute("status"), CreateProperty]
        public ButtonStatus Status
        {
            get => status;
            set => SetStatus(value);
        }

        private bool blocks;
        public bool Blocks
        {
            get => blocks;
            protected set => SetBlocks(value);
        }

        public StatusButton()
        {
            AddToClassList(BaseClass);
            Blocks = BlocksInput(status);
        }

        protected void SetStatus(ButtonStatus newStatus)
        {
            if (status == newStatus)
                return;

            ButtonStatus previousStatus = status;
            if (previousStatus != ButtonStatus.None)
                RemoveFromClassList(statusClasses[previousStatus]);

            status = newStatus;
            if (newStatus != ButtonStatus.None)
                AddToClassList(statusClasses[newStatus]);

            Blocks = BlocksInput(newStatus);

            NotifyPropertyChanged(StatusProperty);
            OnStatusChanged(previousStatus, newStatus);
        }

        protected void SetBlocks(bool newBlocks)
        {
            if (blocks == newBlocks)
                return;

            blocks = newBlocks;
            focusable = !newBlocks;

            if (newBlocks)
            {
                Blur();
                this.RemoveManipulator(clickable);
            }
            else
            {
                this.AddManipulator(clickable);
            }
        }

        // called from the constructor
        protected virtual bool BlocksInput(ButtonStatus status) => status == ButtonStatus.Loading;

        // Hook to react to transitions
        protected virtual void OnStatusChanged(ButtonStatus previous, ButtonStatus current) { }
    }
}
