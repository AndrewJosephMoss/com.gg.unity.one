using UnityEditor;
using UnityEngine;

namespace GG.Unity.UserInterface.Panels.Editor
{
    [CustomEditor(typeof(PanelOpener))]
    public class PanelOpenerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Open Panel"))
                {
                    foreach (var t in targets)
                        ((PanelOpener)t).OpenPanel();
                }
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play mode to open panels. The PanelManager is created at runtime.", MessageType.Info);
        }
    }
}
