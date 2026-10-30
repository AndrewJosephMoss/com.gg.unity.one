using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Buttons
{
    [RequireComponent(typeof(PanelRenderer))]
    public class StatusButtonBinder : MonoBehaviour
    {
        [SerializeField] private List<StatusButtonBinding> bindings = new();

        public IReadOnlyList<StatusButtonBinding> Bindings => bindings;

#if UNITY_EDITOR
        public void AddBinding(string elementName) =>
            bindings.Add(new StatusButtonBinding { elementName = elementName });
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

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement root, int version)
        {
            Unbind();
            foreach (var b in bindings) b.Bind(root, this);
        }

        private void Unbind()
        {
            foreach (var b in bindings) b.Unbind();
        }

        public bool TryGetBinding(string elementName, out StatusButtonBinding binding)
        {
            foreach (var b in bindings)
            {
                if (b.elementName == elementName)
                {
                    binding = b;
                    return true;
                }
            }
            binding = null;
            return false;
        }

        public void SetStatus(string elementName, ButtonStatus status)
        {
            if (TryGetBinding(elementName, out var binding))
                binding.SetStatus(status);
            else
                Debug.LogWarning($"No status button binding named '{elementName}'", this);
        }
    }
}
