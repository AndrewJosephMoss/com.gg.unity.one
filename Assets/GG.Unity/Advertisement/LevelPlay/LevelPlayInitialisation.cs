using UnityEngine;
using GG.Unity.Advertisement;
using GG.Unity.Serialization;
using UnityEngine.Events;
using LP = Unity.Services.LevelPlay;
using Unity.Services.LevelPlay;

namespace GG.Unity.Advertisement.LevelPlay
{
    public class LevelPlayInitialisation : AdvertisementInitialisation
    {
        public UnityEvent<LP.LevelPlayConfiguration> LevelPlayInitialisationComplete;

        public InterfaceRef<ILevelPlayAppIdProvider> appIdProvider;

        [SerializeField]
        private bool isTest = false;

        public override void Initialise()
        {
            LP.LevelPlay.OnInitSuccess += OnInitialisationComplete;
            LP.LevelPlay.OnInitFailed += OnInitialisationFailed;

            LevelPlayPrivacySettings.SetGDPRConsent(false);
            LevelPlayPrivacySettings.SetCOPPA(false);
            LevelPlayPrivacySettings.SetCCPA(false);

            string appId = appIdProvider.Ref.AppId;
            Debug.Log($"LP.LevelPlay. Intialising with appId: {appId}");

            if (isTest)
            {
                LP.LevelPlay.SetMetaData("is_test_suite", "enable");
            }
            LP.LevelPlay.Init(appId);
            LP.LevelPlay.ValidateIntegration();
        }

        private void OnInitialisationComplete(LP.LevelPlayConfiguration configuration)
        {
            Debug.Log($"LP.initialisation complete: {configuration}");
            LevelPlayInitialisationComplete?.Invoke(configuration);
            base.InitialisationComplete?.Invoke(this);
            if (isTest)
            {
                Debug.Log("LP. Launching test suite");
                LP.LevelPlay.LaunchTestSuite();
            }
        }

        private void OnInitialisationFailed(LP.LevelPlayInitError error)
        {
            base.InitialisationFailed?.Invoke(this);
            Debug.LogWarning($"LevelPlay initialiseation failed: {error}");
        }

        #region Validation
        private void OnValidate()
        {
            if (appIdProvider.Ref == null)
            {
                Debug.LogError($"{this.GetType().Name}: appIdProvider is not set or does not implement ILevelPlayAppIdProvider.", this);
            }
        }
        #endregion

    }
}

