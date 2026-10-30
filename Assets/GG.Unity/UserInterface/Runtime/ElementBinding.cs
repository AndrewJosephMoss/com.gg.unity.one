using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface
{
    [Serializable]
    public abstract class ElementBinding<T> where T : VisualElement
    {
        public string elementName;
        public T Element { get; protected set; }

        public abstract void Bind(VisualElement root, UnityEngine.Object context);
        public abstract void Unbind();

        protected T Find(VisualElement root, UnityEngine.Object context)
        {
            if (string.IsNullOrEmpty(elementName))
            {
                Debug.LogWarning($"{typeof(T).Name} binding has no element name", context);
                return null;
            }

            var element = root.Q<T>(elementName);
            if (element == null)
                Debug.LogWarning($"{typeof(T).Name} '{elementName}' not found in UXML", context);
            return element;
        }
    }
}
