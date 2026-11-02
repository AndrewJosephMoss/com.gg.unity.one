using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GG.Unity.IAP.Samples.Editor
{
    /// <summary>
    /// Builds the IAP test scene and its catalog in one click, so the scene never has to be
    /// hand-maintained or committed as YAML.
    /// </summary>
    public static class IAPSampleSceneBuilder
    {
        private const string Folder = "Assets/GG.IAP.Sample";
        private const string CatalogPath = Folder + "/IAPSampleCatalog.asset";
        private const string ScenePath = Folder + "/IAPSample.unity";

        [MenuItem("GG.Unity/IAP/Create Sample Scene")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();

            var catalog = LoadOrCreateCatalog();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("IAPManager");

            // IAPManager.OnValidate logs one "requires a grant handler" error on AddComponent,
            // before the field is wired below. It is harmless.
            var handler = go.AddComponent<SampleGrantHandler>();
            var manager = go.AddComponent<IAPManager>();

            var so = new SerializedObject(manager);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("grantHandler").objectReferenceValue = handler;
            so.FindProperty("initialiseOnStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // The controller lives on its own GameObject so testers can find it in the Hierarchy.
            var controllerGo = new GameObject("IAP Sample Controller");
            var controller = controllerGo.AddComponent<IAPSampleController>();

            var cso = new SerializedObject(controller);
            cso.FindProperty("manager").objectReferenceValue = manager;
            cso.FindProperty("grantHandler").objectReferenceValue = handler;
            cso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = controllerGo;

            Debug.Log($"[IAP Sample] Created {ScenePath}. Enter Play Mode with 'IAP Sample Controller' selected.");
        }

        private static IAPCatalogConfig LoadOrCreateCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<IAPCatalogConfig>(CatalogPath);
            if (existing != null)
                return existing;

            var catalog = ScriptableObject.CreateInstance<IAPCatalogConfig>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);

            // 'products' is private; fill it through serialization like the inspector would.
            var so = new SerializedObject(catalog);
            var list = so.FindProperty("products");

            AddEntry(list, "sample.gems_100", IAPProductKind.Consumable);
            AddEntry(list, "sample.remove_ads", IAPProductKind.NonConsumable);
            AddEntry(list, "sample.vip_monthly", IAPProductKind.Subscription);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AddEntry(SerializedProperty list, string id, IAPProductKind kind)
        {
            list.arraySize++;
            var entry = list.GetArrayElementAtIndex(list.arraySize - 1);
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            entry.FindPropertyRelative("googlePlayId").stringValue = string.Empty;
            entry.FindPropertyRelative("appleAppStoreId").stringValue = string.Empty;
        }
    }
}
