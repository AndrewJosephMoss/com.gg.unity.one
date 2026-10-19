using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Buttons.Editor
{
    [CustomEditor(typeof(LoadingButtonBinder))]
    public class LoadingButtonBinderEditor : UnityEditor.Editor
    {
        private VisualTreeAsset _scannedAsset;
        private Hash128 _scannedHash;
        private readonly List<string> _uxmlLoadingButtons = new();

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            var binder = (LoadingButtonBinder)target;

            DrawLoadingControls(binder);
            EditorGUILayout.Space();
            DrawUxmlScan(binder);
        }

        private void DrawLoadingControls(LoadingButtonBinder binder)
        {
            EditorGUILayout.LabelField("Loading State", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Loading state can only be toggled in Play Mode, when the UI exists.",
                    MessageType.Info);
            }

            var bindings = binder.Bindings;
            if (bindings.Count == 0)
            {
                EditorGUILayout.LabelField("No bindings.");
                return;
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                foreach (var b in bindings)
                {
                    if (string.IsNullOrEmpty(b.elementName)) continue;

                    bool bound = b.Element != null;
                    using (new EditorGUI.DisabledScope(!bound))
                    {
                        EditorGUI.BeginChangeCheck();
                        bool value = EditorGUILayout.ToggleLeft(
                            bound ? b.elementName : $"{b.elementName} (not bound)", b.Loading);
                        if (EditorGUI.EndChangeCheck())
                            b.SetLoading(value);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("All Loading"))
                        foreach (var b in bindings) b.SetLoading(true);

                    if (GUILayout.Button("Clear All"))
                        foreach (var b in bindings) b.SetLoading(false);
                }
            }
        }

        private void DrawUxmlScan(LoadingButtonBinder binder)
        {
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

            var bound = binder.Bindings.Select(b => b.elementName).ToHashSet();

            var stale = bound
                .Where(n => !string.IsNullOrEmpty(n) && !_uxmlLoadingButtons.Contains(n))
                .Select(n => $"LoadingButton '{n}'")
                .ToList();
            if (stale.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Not found in the UXML as a LoadingButton:\n" + string.Join("\n", stale),
                    MessageType.Warning);
            }

            // The same element bound by both binders would fire clicked twice
            var plainBinder = binder.GetComponent<PanelRendererBinder>();
            if (plainBinder != null)
            {
                var duplicates = plainBinder.ButtonBindings
                    .Select(b => b.elementName)
                    .Where(n => !string.IsNullOrEmpty(n) && bound.Contains(n))
                    .Select(n => $"'{n}'")
                    .ToList();
                if (duplicates.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        "Also bound in PanelRendererBinder (clicks will fire twice):\n" +
                        string.Join("\n", duplicates),
                        MessageType.Warning);
                }
            }

            var missing = _uxmlLoadingButtons.Where(n => !bound.Contains(n)).ToList();

            using (new EditorGUI.DisabledScope(missing.Count == 0))
            {
                if (GUILayout.Button($"Add Missing Bindings From UXML ({missing.Count})"))
                {
                    Undo.RecordObject(binder, "Add loading button bindings");
                    foreach (var n in missing) binder.AddBinding(n);
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
            _uxmlLoadingButtons.Clear();

            var root = asset.Instantiate();
            foreach (var e in root.Query<LoadingButton>().ToList())
            {
                if (string.IsNullOrEmpty(e.name) || e.name.StartsWith("unity-")) continue;
                if (!_uxmlLoadingButtons.Contains(e.name)) _uxmlLoadingButtons.Add(e.name);
            }
        }
    }
}
