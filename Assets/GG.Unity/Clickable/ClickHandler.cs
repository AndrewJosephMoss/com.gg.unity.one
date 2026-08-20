using UnityEngine;
using UnityEngine.InputSystem;

namespace GG.Unity.Clickable
{
    public class ClickHandler : MonoBehaviour
    {
        private const string ClickActionMap = "Player";
        private const string ClickActionName = "Click";
        private const string PointerActionName = "Pointer";

        [SerializeField] private LayerMask clickableMask;
        [SerializeField] private InputActionAsset inputActions;
        private InputAction clickAction;
        private InputAction pointerAction;

        private ClickContext ClickContext { get; } = new ClickContext();

        private void Awake()
        {
            clickAction = inputActions.FindActionMap(ClickActionMap)
                .FindAction(ClickActionName);
            pointerAction = inputActions.FindActionMap(ClickActionMap)
                .FindAction(PointerActionName);
        }

        private void OnEnable()
        {
            clickAction.Enable();
            clickAction.performed += OnClick;
        }

        private void OnDisable()
        {
            clickAction.Disable();
            clickAction.performed -= OnClick;
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            Vector2 screenPosition = pointerAction.ReadValue<Vector2>();

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableMask))
                return; // Reset context? - requires play test

            GameObject obj = hit.collider.gameObject;

            if (!obj.TryGetComponent<IClickable>(out IClickable clickable))
                return; // Reset context? - requires play test

            clickable.Clicked(ClickContext);
        }
    }
}

