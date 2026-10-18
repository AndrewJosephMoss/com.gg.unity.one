using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GG.Unity.UserInterface;

namespace GG.Unity.UserInterfacer.Editor
{
    [CustomEditor(typeof(PanelRendererBinder))]
    public class PanelRendererBinderEditor : UnityEditor.Editor
    {
        private VisualTreeAsset _scannedAsset;
        private Hash128 _scannedHash;
        private readonly List<string> _uxmlButtons = new();
        private readonly List<string> _uxmlToggles = new();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            var binder = (PanelRendererBinder)target;
            var panel = binder.GetComponent<PanelRenderer>();
            var asset = panel != null ? panel.visualTreeAsset : null;

            if (asset == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a UXML asset to the PanelRenderer to enable scanning.",
                    MessageType.Info);
                return;
            }

            RescanIfNeeded(asset);

            var boundButtons = binder.ButtonBindings.Select(b => b.elementName).ToHashSet();
            var boundToggles = binder.ToggleBindings.Select(b => b.elementName).ToHashSet();

            // Bindings pointing at names that no longer exist in the UXML
            var stale = new List<string>();
            stale.AddRange(boundButtons.Where(n => !string.IsNullOrEmpty(n) && !_uxmlButtons.Contains(n))
                                       .Select(n => $"Button '{n}'"));
            stale.AddRange(boundToggles.Where(n => !string.IsNullOrEmpty(n) && !_uxmlToggles.Contains(n))
                                       .Select(n => $"Toggle '{n}'"));
            if (stale.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Not found in the UXML:\n" + string.Join("\n", stale),
                    MessageType.Warning);
            }

            var missingButtons = _uxmlButtons.Where(n => !boundButtons.Contains(n)).ToList();
            var missingToggles = _uxmlToggles.Where(n => !boundToggles.Contains(n)).ToList();
            int missing = missingButtons.Count + missingToggles.Count;

            using (new EditorGUI.DisabledScope(missing == 0))
            {
                if (GUILayout.Button($"Add Missing Bindings From UXML ({missing})"))
                {
                    Undo.RecordObject(binder, "Add UXML bindings");
                    foreach (var n in missingButtons) binder.AddButtonBinding(n);
                    foreach (var n in missingToggles) binder.AddToggleBinding(n);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(binder);
                    EditorUtility.SetDirty(binder);
                }
            }
        }

        private void RescanIfNeeded(VisualTreeAsset asset)
        {
            var hash = AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(asset));
            if (asset == _scannedAsset && hash == _scannedHash)
                return;

            _scannedAsset = asset;
            _scannedHash = hash;
            _uxmlButtons.Clear();
            _uxmlToggles.Clear();

            var root = asset.Instantiate();
            Collect(root.Query<Button>().ToList(), _uxmlButtons);
            Collect(root.Query<Toggle>().ToList(), _uxmlToggles);
        }

        private static void Collect<T>(List<T> elements, List<string> names) where T : VisualElement
        {
            foreach (var e in elements)
            {
                // Skip unnamed elements and Unity's internal ones (e.g. "unity-...")
                if (string.IsNullOrEmpty(e.name) || e.name.StartsWith("unity-")) continue;
                if (!names.Contains(e.name)) names.Add(e.name);
            }
        }
    }
}