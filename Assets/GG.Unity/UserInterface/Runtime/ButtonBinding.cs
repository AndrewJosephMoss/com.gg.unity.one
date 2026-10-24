using System;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface
{
    [Serializable]
    public abstract class ButtonBindingBase<T> : ElementBinding where T : Button
    {
        public UnityEvent onClick;
        public T Element { get; private set; }

        public override void Bind(VisualElement root, UnityEngine.Object context)
        {
            Element = Find<T>(root, context);
            if (Element != null) Element.clicked += OnClicked;
        }

        public override void Unbind()
        {
            if (Element != null) Element.clicked -= OnClicked;
            Element = null;
        }

        void OnClicked() => onClick?.Invoke();
    }

    [Serializable]
    public class ButtonBinding : ButtonBindingBase<Button> { }
}
