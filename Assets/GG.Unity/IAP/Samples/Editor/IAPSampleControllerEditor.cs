using UnityEditor;
using UnityEngine;

namespace GG.Unity.IAP.Samples.Editor
{
    /// <summary>
    /// Inspector-based control panel for the IAP sample scene. Select the "IAP Sample Controller"
    /// GameObject in Play Mode to drive purchases through Unity IAP's Fake Store.
    /// </summary>
    [CustomEditor(typeof(IAPSampleController))]
    public sealed class IAPSampleControllerEditor : UnityEditor.Editor
    {
        private Vector2 logScroll;
        private bool showLog = true;

        private IAPSampleController Controller => (IAPSampleController)target;

        // Keeps state, products and the log live while playing.
        public override bool RequiresConstantRepaint() => EditorApplication.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to drive IAP from this inspector. " +
                    "Purchases in the Editor go through Unity IAP's Fake Store.",
                    MessageType.Info);
                DrawSaveControls();
                return;
            }

            var service = Controller.Service;
            if (service == null)
            {
                EditorGUILayout.HelpBox(
                    IAPManager.Instance == null
                        ? "No IAPManager in the scene."
                        : "IAPManager has no Service (check the catalog is assigned).",
                    MessageType.Warning);
                return;
            }

            DrawStatus(service);
            EditorGUILayout.Space();
            DrawGrantSwitches(service);
            EditorGUILayout.Space();
            DrawProducts(service);
            EditorGUILayout.Space();
            DrawSaveControls();
            EditorGUILayout.Space();
            DrawLog();
        }

        // ---------------------------------------------------------------- sections

        private void DrawStatus(IAPManagingService service)
        {
            EditorGUILayout.LabelField("Service", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("State", service.State.ToString());
                if (Controller.PendingCalls > 0)
                    EditorGUILayout.LabelField($"Awaiting {Controller.PendingCalls} call(s)…", EditorStyles.miniLabel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(service.State != IAPState.Uninitialised))
                {
                    if (GUILayout.Button("Initialise"))
                        Controller.Initialise();
                }

                using (new EditorGUI.DisabledScope(!service.IsReady))
                {
                    if (GUILayout.Button("Restore"))
                        Controller.Restore();

                    if (GUILayout.Button(new GUIContent("Buy unknown id", "Uses 'Unknown Product Id' above.")))
                        Controller.PurchaseUnknown();
                }
            }
        }

        private void DrawGrantSwitches(IAPManagingService service)
        {
            EditorGUILayout.LabelField("Grant handler", EditorStyles.boldLabel);

            var handler = Controller.GrantHandler;
            if (handler == null)
            {
                EditorGUILayout.HelpBox("No SampleGrantHandler found.", MessageType.None);
                return;
            }

            handler.NextOrder = (SampleGrantHandler.GrantBehaviour)EditorGUILayout.EnumPopup(
                new GUIContent("Next grant", "Applies to the next order only, then resets to Grant."),
                handler.NextOrder);

            handler.GrantDelaySeconds = EditorGUILayout.Slider(
                new GUIContent("Grant delay (s)", "Simulates a slow save or server call."),
                handler.GrantDelaySeconds, 0f, 10f);

            if (GUILayout.Button("Select grant handler", EditorStyles.miniButton))
                EditorGUIUtility.PingObject(handler);
        }

        private void DrawProducts(IAPManagingService service)
        {
            EditorGUILayout.LabelField($"Products ({service.Products.Count})", EditorStyles.boldLabel);

            if (service.Products.Count == 0)
            {
                EditorGUILayout.LabelField("None fetched yet.", EditorStyles.miniLabel);
                return;
            }

            foreach (var product in service.Products)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(product.Id, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(
                            $"{product.Kind}  ·  {product.LocalisedPrice ?? "?"}",
                            EditorStyles.miniLabel);
                    }

                    var owned = SampleGrantHandler.IsOwned(product.Id) ? "  ·  OWNED" : string.Empty;
                    EditorGUILayout.LabelField(
                        $"Granted {SampleGrantHandler.GetGrantCount(product.Id)}x{owned}" +
                        (product.AvailableToPurchase ? string.Empty : "  ·  not available"),
                        EditorStyles.miniLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    using (new EditorGUI.DisabledScope(!service.IsReady))
                    {
                        if (GUILayout.Button("Buy"))
                            Controller.Purchase(product.Id);

                        if (GUILayout.Button(new GUIContent("Buy ×2", "Second call should return Busy.")))
                            Controller.PurchaseTwice(product.Id);
                    }
                }
            }
        }

        private void DrawSaveControls()
        {
            EditorGUILayout.LabelField("Sample save", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{SampleGrantHandler.TransactionCount} transaction(s) recorded");
                if (GUILayout.Button("Reset save", GUILayout.Width(100)))
                    Controller.ResetSave();
            }
        }

        private void DrawLog()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                showLog = EditorGUILayout.Foldout(showLog, $"Event log ({Controller.LogLines.Count})", true);
                if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(60)))
                    Controller.ClearLog();
            }

            if (!showLog)
                return;

            logScroll = EditorGUILayout.BeginScrollView(logScroll, EditorStyles.helpBox, GUILayout.Height(240));

            // Newest first, so the latest result is visible without scrolling.
            var lines = Controller.LogLines;
            for (int i = lines.Count - 1; i >= 0; i--)
                EditorGUILayout.SelectableLabel(lines[i], EditorStyles.miniLabel, GUILayout.Height(14));

            EditorGUILayout.EndScrollView();
        }
    }
}
