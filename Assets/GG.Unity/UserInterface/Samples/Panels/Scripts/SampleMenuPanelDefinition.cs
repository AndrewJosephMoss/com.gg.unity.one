using GG.Unity.UserInterface.Panels;
using UnityEngine;

namespace GG.Unity.UserInterface.Samples
{
    [CreateAssetMenu(fileName = "SampleMenuPanel", menuName = "GG.Unity/UserInterface/Samples/Sample Menu Panel")]
    public class SampleMenuPanelDefinition : PanelDefinition
    {
        [Header("Sample")]
        [SerializeField, Tooltip("Opened by 'Delete save…' and awaited for a result.")]
        private PanelDefinition confirmPanel;

        [SerializeField, Tooltip("Shown when a save or sync completes.")]
        private PanelDefinition toastPanel;

        [SerializeField, Min(0.1f)] private float saveDuration = 2f;
        [SerializeField, Min(0.1f)] private float syncDuration = 3.5f;

        public PanelDefinition ConfirmPanel => confirmPanel;
        public PanelDefinition ToastPanel => toastPanel;
        public float SaveDuration => saveDuration;
        public float SyncDuration => syncDuration;

        public override IPanelController CreateController() => new SampleMenuController(this);
    }
}
