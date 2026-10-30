using UnityEngine.Events;
using UnityEngine.UIElements;
using System;

namespace GG.Unity.UserInterface.Buttons
{
    [Serializable]
    public class StatusButtonBinding : ElementBinding<StatusButton>
    {
        public UnityEvent Clicked;
        public ButtonStatus Status { get; private set; } = ButtonStatus.None;

        public override void Bind(VisualElement root, UnityEngine.Object context)
        {
            Element = Find(root, context);
            if (Element == null)
                return;
            
            Element.clicked += OnClicked;
            Element.Status = Status;
        }

        public override void Unbind()
        {
            if (Element != null) 
                Element.clicked -= OnClicked;
            Element = null;
        }

        public void SetStatus(ButtonStatus status)
        {
            if (Status == status)
                return;

            Status = status;
            if (Element != null)
                Element.Status = status;
        }

        void OnClicked() => Clicked?.Invoke();
    }
}
