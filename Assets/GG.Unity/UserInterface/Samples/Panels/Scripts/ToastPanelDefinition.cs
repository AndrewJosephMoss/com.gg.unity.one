using GG.Unity.UserInterface.Panels;
using UnityEngine;

namespace GG.Unity.UserInterface.Samples
{
    /// <summary>
    /// Short-lived notification on the Overlay layer. Pass a string as args to override the text.
    /// Configure as: non-singleton, non-modal, closeOnBack off, focusOnOpen off.
    /// </summary>
    [CreateAssetMenu(fileName = "ToastPanel", menuName = "GG.Unity/UserInterface/Samples/Toast Panel")]
    public class ToastPanelDefinition : PanelDefinition
    {
        [Header("Toast")]
        [SerializeField] private string defaultText = "Hello!";
        [SerializeField, Min(0.25f)] private float duration = 2.5f;

        public string DefaultText => defaultText;
        public float Duration => duration;

        public override IPanelController CreateController() => new ToastPanelController(this);
    }
}
