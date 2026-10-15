using UnityEngine;

namespace GG.Unity.UserInterface.Panels
{
    public class PanelOpener : MonoBehaviour
    {
        private PanelService service;

        private void Awake()
        {
            service = GetComponentInParent<PanelService>();
        }

        public void OpenPanel(PanelDefinition definition)
        {
            if (service == null)
            {
                Debug.LogError($"{name}: PanelService not found in parent hierarchy.", this);
                return;
            }
            service.Manager.Open(definition);
        }

        #region Validation
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Skip if in prefab editing mode or if this is part of a prefab asset
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) 
                return;
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null) 
                return;

            if (GetComponentInParent<PanelService>(true) == null)
                Debug.LogWarning($"{name}: no PanelService found in parents.", this);
        }
#endif
        #endregion
    }
}
