using GG.Unity.Timers;
using UnityEngine;
using UnityEngine.Events;

namespace GG.Unity.Advertisement
{
    /// <summary>
    /// Initialises the ad SDK once consent is known. Nothing happens on Start any more:
    /// <see cref="AdsManager.SetConsent"/> (normally called by <see cref="ConsentFlow"/>)
    /// calls <see cref="BeginInitialisation"/>.
    /// </summary>
    public abstract class AdvertisementInitialisation : MonoBehaviour
    {
        public UnityEvent<AdvertisementInitialisation> InitialisationComplete;
        public UnityEvent<AdvertisementInitialisation> InitialisationFailed;

        [SerializeField] private float baseRetryDelaySeconds = 2f;
        [SerializeField] private float maxRetryDelaySeconds = 60f;
        [SerializeField, Range(0f, 0.5f)] private float jitterFraction = 0.2f;

        private BackOffTimer retryTimer;
        private bool isInitialising;

        public bool IsInitialised { get; private set; }

        /// <summary>True once <see cref="BeginInitialisation"/> has been called.</summary>
        public bool HasStarted => retryTimer != null;

        public AdConsent Consent { get; private set; } = AdConsent.Restrictive();

        /// <summary>Apply consent, then start initialising the SDK. Later calls only update consent.</summary>
        public void BeginInitialisation(AdConsent consent)
        {
            if (HasStarted)
            {
                UpdateConsent(consent);
                return;
            }

            Consent = consent;

            retryTimer = new BackOffTimer(
                this,
                AttemptInitialise,
                baseRetryDelaySeconds,
                maxRetryDelaySeconds,
                jitterFraction);

            // Privacy flags must reach the SDK before its init call.
            ApplyConsent(consent);
            InitialiseProvider();
            AttemptInitialise();
        }

        /// <summary>The player changed their choice after initialisation (e.g. from a settings screen).</summary>
        public void UpdateConsent(AdConsent consent)
        {
            Consent = consent;
            ApplyConsent(consent);
        }

        private void AttemptInitialise()
        {
            if (IsInitialised || isInitialising)
                return;

            isInitialising = true;
            Initialise();
        }

        /// <summary>
        /// Pass the consent to your SDK's privacy API (GDPR consent, "do not sell", child-directed).
        /// Called before the first <see cref="Initialise"/> and again whenever consent changes.
        /// </summary>
        protected abstract void ApplyConsent(AdConsent consent);

        /// <summary>Subscribe to the SDK's init-result events. Called once, before the first attempt.</summary>
        protected abstract void InitialiseProvider();

        /// <summary>Kick off the SDK's init call. Called on the first attempt and on every retry.</summary>
        protected abstract void Initialise();

        /// <summary>Unsubscribe from the SDK's events. Called on destroy, if initialisation was started.</summary>
        protected virtual void DisposeProvider() { }

        // ---- Provider callbacks: forward your SDK's init result to these --------

        protected void NotifyInitialisationSucceeded()
        {
            isInitialising = false;
            IsInitialised = true;
            retryTimer.Reset();
            InitialisationComplete?.Invoke(this);
        }

        protected void NotifyInitialisationFailed()
        {
            isInitialising = false;
            retryTimer.Schedule();
            InitialisationFailed?.Invoke(this);
        }

        private void OnDestroy()
        {
            retryTimer?.Cancel();
            if (HasStarted)
                DisposeProvider();
        }
    }
}
