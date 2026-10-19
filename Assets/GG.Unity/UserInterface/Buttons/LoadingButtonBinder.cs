using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Buttons
{
    [RequireComponent(typeof(PanelRenderer))]
    public class LoadingButtonBinder : MonoBehaviour
    {
        [SerializeField] private List<LoadingButtonBinding> bindings = new();

        public IReadOnlyList<LoadingButtonBinding> Bindings => bindings;

#if UNITY_EDITOR
        public void AddBinding(string elementName) =>
            bindings.Add(new LoadingButtonBinding { elementName = elementName });
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
            foreach (var b in bindings) b.Bind(root, this);
        }

        private void Unbind()
        {
            foreach (var b in bindings) b.Unbind();
        }

        public bool TryGetBinding(string elementName, out LoadingButtonBinding binding)
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

        public void SetLoading(string elementName, bool loading)
        {
            if (TryGetBinding(elementName, out var binding))
                binding.SetLoading(loading);
            else
                Debug.LogWarning($"No loading button binding named '{elementName}'", this);
        }
    }
}
