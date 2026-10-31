using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Panels
{
    /// <summary>
    /// Data describing a panel. Shared by every instance, so it must not hold runtime state;
    /// put per-instance behaviour in an <see cref="IPanelController"/> via <see cref="CreateController"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "PanelDefinition", menuName = "GG.Unity/UserInterface/PanelDefinition")]
    public class PanelDefinition : ScriptableObject
    {
        [SerializeField] private VisualTreeAsset tree;

        [Header("Stacking")]
        [SerializeField, Tooltip("Draw/input bucket. Higher layers always sit above lower ones.")]
        private PanelLayer layer = PanelLayer.Window;

        [SerializeField, Tooltip("Re-use the open instance instead of opening a second one.")]
        private bool singleton = true;

        [SerializeField, Tooltip("Blocks pointer input and focus to everything beneath.")]
        private bool modal = true;

        [Header("Closing")]
        [SerializeField, Tooltip("Close when PanelManager.CloseTopPanel() is called (Back/Escape).")]
        private bool closeOnBack = true;

        [SerializeField, Tooltip("Close when the modal backdrop (the host element itself) is clicked. Modal panels only.")]
        private bool closeOnBackdropClick;

        [SerializeField, Min(0f), Tooltip("Seconds to keep the panel in the hierarchy after closing so USS transitions can play. Match your stylesheet's transition-duration; 0 removes it immediately.")]
        private float closeDuration = 0.15f;

        [Header("Focus")]
        [SerializeField, Tooltip("Move keyboard/gamepad focus into this panel when it opens. Turn off for toasts and HUD panels.")]
        private bool focusOnOpen = true;

        public VisualTreeAsset Tree => tree;
        public PanelLayer Layer => layer;
        public bool Singleton => singleton;
        public bool Modal => modal;
        public bool CloseOnBack => closeOnBack;
        public bool CloseOnBackdropClick => closeOnBackdropClick;
        public float CloseDuration => closeDuration;
        public bool FocusOnOpen => focusOnOpen;

        /// <summary>
        /// Create the behaviour for one open instance of this panel. Return null for purely static panels.
        /// </summary>
        public virtual IPanelController CreateController() => null;

        protected virtual void OnValidate()
        {
            if (tree == null)
                Debug.LogWarning($"PanelDefinition '{name}' has no VisualTreeAsset assigned.", this);

            if (closeOnBackdropClick && !modal)
                Debug.LogWarning($"PanelDefinition '{name}': closeOnBackdropClick only works on modal panels.", this);
        }
    }
}
