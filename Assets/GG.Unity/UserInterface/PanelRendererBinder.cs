using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface
{
    [Serializable]
    public abstract class ElementBinding
    {
        public string elementName;

        public abstract void Bind(VisualElement root, UnityEngine.Object context);
        public abstract void Unbind();

        protected T Find<T>(VisualElement root, UnityEngine.Object context) where T : VisualElement
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

    [Serializable]
    public class ToggleBinding : ElementBinding
    {
        public UnityEvent<bool> onValueChanged;

        public Toggle Element { get; private set; }

        public override void Bind(VisualElement root, UnityEngine.Object context)
        {
            Element = Find<Toggle>(root, context);
            Element?.RegisterValueChangedCallback(OnChanged);
        }

        public override void Unbind()
        {
            Element?.UnregisterValueChangedCallback(OnChanged);
            Element = null;
        }

        private void OnChanged(ChangeEvent<bool> evt) => onValueChanged?.Invoke(evt.newValue);
    }

    [RequireComponent(typeof(PanelRenderer))]
    public class PanelRendererBinder : MonoBehaviour
    {
        [SerializeField] private List<ButtonBinding> buttonBindings = new();
        [SerializeField] private List<ToggleBinding> toggleBindings = new();

        public IReadOnlyList<ButtonBinding> ButtonBindings => buttonBindings;
        public IReadOnlyList<ToggleBinding> ToggleBindings => toggleBindings;

#if UNITY_EDITOR
        public void AddButtonBinding(string elementName) =>
            buttonBindings.Add(new ButtonBinding { elementName = elementName });

        public void AddToggleBinding(string elementName) =>
            toggleBindings.Add(new ToggleBinding { elementName = elementName });
#endif

        private PanelRenderer _panelRenderer;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDestroy()
        {
            Unbind();
            if (_panelRenderer != null)
                _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
        {
            Unbind();
            foreach (var b in buttonBindings) b.Bind(root, this);
            foreach (var t in toggleBindings) t.Bind(root, this);
        }

        private void Unbind()
        {
            foreach (var b in buttonBindings) b.Unbind();
            foreach (var t in toggleBindings) t.Unbind();
        }

        public bool TryGetToggle(string elementName, out Toggle toggle)
        {
            foreach (var t in toggleBindings)
            {
                if (t.elementName == elementName && t.Element != null)
                {
                    toggle = t.Element;
                    return true;
                }
            }
            toggle = null;
            return false;
        }
    }
}
