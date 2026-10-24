
using GG.Unity.Timers;
using UnityEngine;
using UnityEngine.Events;

namespace GG.Unity.Advertisement
{
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

        private void Start()
        {
            retryTimer = new BackOffTimer(
                this,
                AttemptInitialise,
                baseRetryDelaySeconds,
                maxRetryDelaySeconds,
                jitterFraction);

            InitialiseProvider();
            AttemptInitialise();
        }

        private void AttemptInitialise()
        {
            if (IsInitialised || isInitialising)
                return;

            isInitialising = true;
            Initialise();
        }

        /// <summary>Subscribe to the SDK's init-result events. Called once, before the first attempt.</summary>
        protected abstract void InitialiseProvider();

        /// <summary>Kick off the SDK's init call. Called on the first attempt and on every retry.</summary>
        protected abstract void Initialise();

        /// <summary>Unsubscribe from the SDK's events. Called on destroy.</summary>
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
            DisposeProvider();
        }
    }
}