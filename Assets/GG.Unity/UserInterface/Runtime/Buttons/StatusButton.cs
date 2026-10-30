using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Buttons
{
    public enum ButtonStatus { None, Loading }

    [UxmlElement]
    public partial class StatusButton : Button
    {
        private const string BaseClass = "status-button";
        private const string ButtonStatusClassFormat = BaseClass + "--{0}";

        public event Action<ButtonStatus, ButtonStatus> StatusChanged;

        protected static readonly Dictionary<ButtonStatus, string> StatusClasses =
            Enum.GetValues(typeof(ButtonStatus))
                .Cast<ButtonStatus>()
                .ToDictionary(
                    status => status,
                    status => string.Format(
                        ButtonStatusClassFormat, 
                        status.ToString().ToLowerInvariant()));

        private ButtonStatus status;
        [UxmlAttribute("status")]
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
                RemoveFromClassList(StatusClasses[previousStatus]);

            status = newStatus;
            if (newStatus != ButtonStatus.None)
                AddToClassList(StatusClasses[newStatus]);

            Blocks = BlocksInput(newStatus);

            StatusChanged?.Invoke(previousStatus, newStatus);
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
                this.AddManipulator(clickable);
        }

        protected virtual bool BlocksInput(ButtonStatus status) => status == ButtonStatus.Loading;
    }
}
