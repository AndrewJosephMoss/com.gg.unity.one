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

        private Camera mainCamera;

        private ClickablesContext ClickablesContext { get; } = new ClickablesContext();

        private void Awake()
        {
            mainCamera = Camera.main;
            clickAction = inputActions.FindActionMap(ClickActionMap)
                .FindAction(ClickActionName);
            pointerAction = inputActions.FindActionMap(ClickActionMap)
                .FindAction(PointerActionName);
        }

        private void OnEnable()
        {
            clickAction.Enable();
            pointerAction.Enable();
            clickAction.performed += OnClick;
        }

        private void OnDisable()
        {
            clickAction.performed -= OnClick;
            clickAction.Disable();
            pointerAction.Disable();
        }

        private void OnClick(InputAction.CallbackContext callbackContext)
        {
            Vector2 screenPosition = pointerAction.ReadValue<Vector2>();

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableMask))
                return; // Reset context? - requires play test

            GameObject obj = hit.collider.gameObject;

            if (!obj.TryGetComponent<IClickable>(out IClickable clickable))
                return; // Reset context? - requires play test

            clickable.Clicked(hit, ClickablesContext);
        }
    }
}

