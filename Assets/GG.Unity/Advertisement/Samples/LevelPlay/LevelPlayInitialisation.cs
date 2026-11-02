using GG.Unity.Serialization;
using UnityEngine;
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

        protected override void ApplyConsent(AdConsent consent)
        {
            Debug.Log($"LevelPlay applying {consent}");

            // GDPR: true = the player consented to personalised ads.
            LevelPlayPrivacySettings.SetGDPRConsent(consent.PersonalisedAdsAllowed);

            // US privacy laws: true = the player opted OUT of the sale/sharing of their data.
            LevelPlayPrivacySettings.SetCCPA(!consent.PersonalisedAdsAllowed);

            // COPPA: true = treat the player as a child. Must be set before Init().
            LevelPlayPrivacySettings.SetCOPPA(consent.IsChildDirected);
        }

        protected override void InitialiseProvider()
        {
            LP.LevelPlay.OnInitSuccess += OnInitialisationComplete;
            LP.LevelPlay.OnInitFailed += OnInitialisationFailed;

            if (isTest)
                LP.LevelPlay.SetMetaData("is_test_suite", "enable");
        }

        protected override void Initialise()
        {
            string appId = appIdProvider.Ref.AppId;
            Debug.Log($"LevelPlay Initialising with appId: {appId}");

            LP.LevelPlay.Init(appId);

            if (isTest)
                LP.LevelPlay.ValidateIntegration();
        }

        private void OnInitialisationComplete(LP.LevelPlayConfiguration configuration)
        {
            Debug.Log($"LevelPlay initialisation complete: {configuration}");
            LevelPlayInitialisationComplete?.Invoke(configuration);
            NotifyInitialisationSucceeded();

            if (isTest)
            {
                Debug.Log("LevelPlay Launching test suite");
                LP.LevelPlay.LaunchTestSuite();
            }
        }

        private void OnInitialisationFailed(LP.LevelPlayInitError error)
        {
            Debug.LogWarning($"LevelPlay initialisation failed: {error}");
            NotifyInitialisationFailed();
        }

        protected override void DisposeProvider()
        {
            LP.LevelPlay.OnInitSuccess -= OnInitialisationComplete;
            LP.LevelPlay.OnInitFailed -= OnInitialisationFailed;
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