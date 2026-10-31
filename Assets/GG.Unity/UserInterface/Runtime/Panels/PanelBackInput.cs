#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace GG.Unity.UserInterface.Panels
{
    /// <summary>
    /// Routes a Back/Cancel input action to <see cref="PanelManager.CloseTopPanel"/>.
    /// Requires the Input System package, with Unity.InputSystem referenced by this assembly's asmdef.
    /// </summary>
    public class PanelBackInput : MonoBehaviour
    {
        [SerializeField] private PanelManager manager;

        [SerializeField, Tooltip("Typically UI/Cancel (Escape, gamepad B/East).")]
        private InputActionReference backAction;

        [SerializeField, Tooltip("Invoked when Back was pressed but no panel consumed it, e.g. to open a pause menu.")]
        private UnityEvent unhandledBack;

        private InputAction action;
        private bool enabledByThis;

        private void Awake()
        {
            if (manager == null)
                manager = GetComponentInParent<PanelManager>(true);
        }

        private void OnEnable()
        {
            action = backAction != null ? backAction.action : null;
            if (action == null)
            {
                Debug.LogError($"{name}: no back action assigned.", this);
                return;
            }

            action.performed += OnBack;
            if (!action.enabled)
            {
                action.Enable();
                enabledByThis = true;
            }
        }

        private void OnDisable()
        {
            if (action == null)
                return;

            action.performed -= OnBack;
            if (enabledByThis)
            {
                action.Disable();
                enabledByThis = false;
            }
            action = null;
        }

        private void OnBack(InputAction.CallbackContext context)
        {
            if (manager == null || !manager.CloseTopPanel())
                unhandledBack?.Invoke();
        }
    }
}
#endif
