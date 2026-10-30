using System;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface
{
    [Serializable]
    public class ToggleBinding : ElementBinding<Toggle>
    {
        public UnityEvent<bool> ValueChanged;
        public bool Value { get; private set; }

        public override void Bind(VisualElement root, UnityEngine.Object context)
        {
            Element = Find(root, context);
            if (Element == null)
                return;

            Element.SetValueWithoutNotify(Value);
            Element.RegisterValueChangedCallback(OnChanged);
        }

        public override void Unbind()
        {
            Element?.UnregisterValueChangedCallback(OnChanged);
            Element = null;
        }

        private void OnChanged(ChangeEvent<bool> evt)
        {
            Value = evt.newValue;
            ValueChanged?.Invoke(evt.newValue);
        }
    }
}
