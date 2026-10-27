using UnityEngine;

namespace GG.Unity.UserInterface.Panels
{
    public class PanelOpener : MonoBehaviour
    {
        [SerializeField] private PanelManager service;
        [SerializeField] private PanelDefinition panelDefinition;

        private void Awake()
        {
            if (service == null)
                service = GetComponentInParent<PanelManager>();
        }

        public void OpenPanel() => OpenPanel(panelDefinition);

        public void OpenPanel(PanelDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError($"{name}: no PanelDefinition to open.", this);
                return;
            }
            if (service == null || service.Manager == null)
            {
                Debug.LogError($"{name}: PanelService not found or not initialised.", this);
                return;
            }
            service.Manager.Open(definition);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
                return;
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;

            if (service == null)
                service = GetComponentInParent<PanelManager>(true);

            if (service == null)
                Debug.LogWarning($"{name}: no PanelService assigned or found in parents.", this);
        }
#endif
    }
}
