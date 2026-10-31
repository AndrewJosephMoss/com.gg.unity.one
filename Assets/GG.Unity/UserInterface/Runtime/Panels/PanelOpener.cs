using UnityEngine;
using UnityEngine.Serialization;

namespace GG.Unity.UserInterface.Panels
{
    public class PanelOpener : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("service")]
        private PanelManager manager;

        [SerializeField] private PanelDefinition panelDefinition;

        private void Awake()
        {
            if (manager == null)
                manager = GetComponentInParent<PanelManager>(true);
        }

        public void OpenPanel() => OpenPanel(panelDefinition);

        public void OpenPanel(PanelDefinition definition)
        {
            if (manager == null)
            {
                Debug.LogError($"{name}: no PanelManager assigned or found in parents.", this);
                return;
            }
            manager.OpenPanel(definition);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
                return;
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;

            if (manager == null)
                manager = GetComponentInParent<PanelManager>(true);

            if (manager == null)
                Debug.LogWarning($"{name}: no PanelManager assigned or found in parents.", this);
        }
#endif
    }
}
