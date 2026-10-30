using System;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface
{
    [Serializable]
    public class ButtonBinding : ElementBinding<Button>
    {
        public UnityEvent Clicked;

        public override void Bind(VisualElement root, UnityEngine.Object context)
        {
            Element = Find(root, context);
            if (Element != null) 
                Element.clicked += OnClicked;
        }

        public override void Unbind()
        {
            if (Element != null) 
                Element.clicked -= OnClicked;
            Element = null;
        }

        void OnClicked() => Clicked?.Invoke();
    }
}
