using UnityEngine;

namespace GG.Unity.UserInterface.Panels
{
    [CreateAssetMenu(fileName = "PanelDefinition", menuName = "GG.Unity/UserInterface/PanelDefinition")]
    public class PanelDefinition : ScriptableObject
    {
        [SerializeField]
        private Panel prefab;
        public Panel Prefab => prefab;

        [SerializeField]
        private PanelLayer layer = PanelLayer.Popup;
        public PanelLayer Layer => layer;

        [SerializeField]
        private bool singleton = true; // Re-use if open
        public bool Singleton => singleton;

        [SerializeField]
        private bool modal = true;
        public bool Modal => modal; // Block input

        [SerializeField]
        private bool closeOnBack = true;
        public bool CloseOnBack => closeOnBack; // Close on escape/back

        #region Validation
        private void OnValidate()
        {
            if (prefab == null)
                Debug.LogWarning($"PanelDefinition '{name}' has no prefab assigned.", this);
        }
        #endregion
    }

    public enum PanelLayer
    {
        Screen = 0,
        Popup = 10,
        Overlay = 20
    }
}
