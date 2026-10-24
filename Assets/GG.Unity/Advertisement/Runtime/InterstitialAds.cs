using GG.Unity.Timers;
using UnityEngine;
using System;

namespace GG.Unity.Advertisement
{
    public abstract class InterstitialAds : MonoBehaviour
    {
        [Header("Load retry (exponential backoff)")]
        [SerializeField] private float baseRetryDelaySeconds = 2f;
        [SerializeField] private float maxRetryDelaySeconds = 60f;
        [SerializeField, Range(0f, 0.5f)] private float jitterFraction = 0.2f;

        public event Action AdDisplayed;

        private BackOffTimer loadRetryTimer;
        private bool isLoading;
        private bool isInitialised;

        public abstract bool IsReady { get; }

        public void InitialiseInterstitialAds()
        {
            if (isInitialised)
                return;

            isInitialised = true;

            loadRetryTimer = new BackOffTimer(
                this,
                LoadInterstitialAd,
                baseRetryDelaySeconds,
                maxRetryDelaySeconds,
                jitterFraction);

            InitialiseProvider();
            LoadInterstitialAd();
        }

        public void LoadInterstitialAd()
        {
            if (!isInitialised || isLoading || IsReady)
                return;

            // An explicit load supersedes any pending scheduled retry.
            loadRetryTimer.Cancel();

            isLoading = true;
            LoadAd();
        }

        public void ShowInterstitialAd()
        {
            if (!isInitialised)
                return;

            if (IsReady)
                ShowAd();
            else if (!isLoading && !loadRetryTimer.IsPending)
                LoadInterstitialAd();
        }

        protected abstract void InitialiseProvider();

        protected abstract void LoadAd();

        protected abstract void ShowAd();

        protected virtual void DisposeProvider() { }

        protected void NotifyLoadSucceeded()
        {
            isLoading = false;
            loadRetryTimer.Reset();
        }

        protected void NotifyLoadFailed()
        {
            isLoading = false;
            loadRetryTimer.Schedule();
        }

        protected void NotifyShowFailed()
        {
            // The ad was consumed or invalid; get a fresh one.
            isLoading = false;
            LoadInterstitialAd();
        }

        protected void NotifyAdDisplayed()
        {
            SafeInvoke(AdDisplayed);
        }

        private static void SafeInvoke(Action handlers)
        {
            if (handlers == null)
                return;

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try { ((Action)handler)(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        protected void NotifyAdClosed()
        {
            // Preload the next one right away and start with a fresh backoff.
            loadRetryTimer.Reset();
            LoadInterstitialAd();
        }

        private void OnDestroy()
        {
            loadRetryTimer?.Cancel();
            if (isInitialised)
                DisposeProvider();
        }
    }
}