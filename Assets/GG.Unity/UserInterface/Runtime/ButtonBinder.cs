using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface
{
    [RequireComponent(typeof(PanelRenderer))]
    public class ButtonBinder : MonoBehaviour
    {
        [SerializeField] public List<ButtonBinding> bindings = new();

#if UNITY_EDITOR
        public void AddButtonBinding(string elementName) =>
            bindings.Add(new ButtonBinding { elementName = elementName });
#endif

        [SerializeField] private PanelRenderer panelRenderer;

        private void Awake()
        {
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDestroy()
        {
            Unbind();
            if (panelRenderer != null)
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
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

        #region Validation
        private void OnValidate()
        {
            if (panelRenderer == null)
                panelRenderer = GetComponent<PanelRenderer>();
        }
        #endregion
    }
}
