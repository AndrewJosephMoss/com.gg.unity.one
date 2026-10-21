using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    [CreateAssetMenu(fileName = "PanelDefinition", menuName = "GG.Unity/UserInterface/PanelDefinition")]
    public class PanelDefinition : ScriptableObject
    {
        [SerializeField] private VisualTreeAsset tree;
        public VisualTreeAsset Tree => tree;

        [SerializeField] private bool singleton = true;   // Re-use if already open
        public bool Singleton => singleton;

        [SerializeField] private bool modal = true;       // Blocks pointer input to panels beneath
        public bool Modal => modal;

        [SerializeField] private bool closeOnBack = true; // Close on cancel/escape
        public bool CloseOnBack => closeOnBack;

        private void OnValidate()
        {
            if (tree == null)
                Debug.LogWarning($"PanelDefinition '{name}' has no VisualTreeAsset assigned.", this);
        }
    }
}