using UnityEditor;
using UnityEngine;

namespace GG.Unity.Advertisement.Editor
{
    /// <summary>
    /// Inspector tools for testing the consent flow: see what is stored, clear it,
    /// and re-run the flow in Play mode without restarting.
    /// </summary>
    [CustomEditor(typeof(ConsentFlow))]
    public class ConsentFlowEditor : UnityEditor.Editor
    {
        private SerializedProperty policyVersion;
        private SerializedProperty reaskAfterDays;

        private void OnEnable()
        {
            policyVersion = serializedObject.FindProperty("policyVersion");
            reaskAfterDays = serializedObject.FindProperty("reaskAfterDays");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Consent Testing", EditorStyles.boldLabel);

            DrawStoredState();

            if (Application.isPlaying)
                DrawRuntimeState((ConsentFlow)target);

            EditorGUILayout.Space();
            DrawButtons((ConsentFlow)target);

            // Keep the runtime readout live while the flow is running.
            if (Application.isPlaying)
                Repaint();
        }

        private void DrawStoredState()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Stored (PlayerPrefs)", EditorStyles.miniBoldLabel);

                int version = policyVersion != null ? policyVersion.intValue : 1;
                int maxAge = reaskAfterDays != null ? reaskAfterDays.intValue : 0;

                if (ConsentStore.TryLoad(version, maxAge, out bool personalised))
                {
                    EditorGUILayout.LabelField("Decision", personalised ? "Accepted (personalised ads)" : "Declined");
                    var decidedAt = ConsentStore.DecidedAtUtc;
                    EditorGUILayout.LabelField("Decided at (local)",
                        decidedAt.HasValue ? decidedAt.Value.ToLocalTime().ToString("g") : "Unknown");
                }
                else
                {
                    EditorGUILayout.LabelField("Decision",
                        "None valid for policy v" + version + " (the player will be asked)");
                }
            }
        }

        private static void DrawRuntimeState(ConsentFlow flow)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Runtime", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Busy", flow.IsBusy ? "Yes (waiting for the player)" : "No");
                EditorGUILayout.LabelField("Resolved", flow.HasResolved ? "Yes" : "No");
                if (flow.HasResolved)
                {
                    AdConsent c = flow.Current;
                    EditorGUILayout.LabelField("Personalised ads", c.PersonalisedAdsAllowed.ToString());
                    EditorGUILayout.LabelField("Child directed", c.IsChildDirected.ToString());
                    EditorGUILayout.LabelField("Tracking authorised", c.TrackingAuthorised.ToString());
                }
            }
        }

        private static void DrawButtons(ConsentFlow flow)
        {
            if (GUILayout.Button("Clear stored consent"))
            {
                ConsentStore.Clear();
                Debug.Log("[ConsentFlowEditor] Stored consent cleared. The player will be asked on the next resolve.");
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying || flow.IsBusy))
            {
                if (GUILayout.Button("Clear & restart consent flow"))
                {
                    ConsentStore.Clear();
                    Debug.Log("[ConsentFlowEditor] Stored consent cleared; restarting the consent flow.");
                    // Nothing stored now, so this shows the popup. If the SDK is already
                    // initialised, the new choice is applied through AdsManager.SetConsent.
                    flow.ResolveConsent();
                }

                if (GUILayout.Button("Change consent (as from a settings screen)"))
                    flow.ChangeConsent();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play mode to restart the flow or change consent.", MessageType.None);
            else if (flow.IsBusy)
                EditorGUILayout.HelpBox("The consent flow is waiting for the player.", MessageType.None);
        }
    }
}
