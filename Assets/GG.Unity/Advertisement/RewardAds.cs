using GG.Unity.Timers;
using System;
using UnityEngine;

namespace GG.Unity.Advertisement
{
    public abstract class RewardAds : MonoBehaviour
    {
        [Header("Load retry (exponential backoff)")]
        [SerializeField] private float baseRetryDelaySeconds = 2f;
        [SerializeField] private float maxRetryDelaySeconds = 60f;
        [SerializeField, Range(0f, 0.5f)] private float jitterFraction = 0.2f;

        public event Action AdLoaded;
        public event Action LoadFailed;
        public event Action ShowFailed;
        public event Action AdClosed;
        public event Action AdRewarded;

        private BackOffTimer loadRetryTimer;
        private bool isLoading;
        private bool isInitialised;

        public abstract bool IsReady { get; }

        public void InitialiseRewardAds()
        {
            if (isInitialised)
                return;

            isInitialised = true;

            loadRetryTimer = new BackOffTimer(
                this,
                LoadRewardAd,
                baseRetryDelaySeconds,
                maxRetryDelaySeconds,
                jitterFraction);

            InitialiseProvider();
            LoadRewardAd();
        }

        public void LoadRewardAd()
        {
            if (!isInitialised || isLoading || IsReady)
                return;

            // An explicit load supersedes any pending scheduled retry.
            loadRetryTimer.Cancel();

            isLoading = true;
            LoadAd();
        }

        public bool ShowRewardAd()
        {
            if (!isInitialised)
                return false;

            if (IsReady)
            {
                ShowAd();
                return true;
            }
            else if (!isLoading && !loadRetryTimer.IsPending)
            {
                LoadRewardAd();
            }
            return false;
        }

        protected abstract void InitialiseProvider();

        protected abstract void LoadAd();

        protected abstract void ShowAd();

        protected virtual void DisposeProvider() { }

        protected void NotifyLoadSucceeded()
        {
            isLoading = false;
            loadRetryTimer.Reset();
            SafeInvoke(AdLoaded);
        }

        protected void NotifyLoadFailed()
        {
            isLoading = false;
            loadRetryTimer.Schedule();
            SafeInvoke(LoadFailed);
        }

        protected void NotifyShowFailed()
        {
            isLoading = false;
            LoadRewardAd();
            SafeInvoke(ShowFailed);
        }

        protected void NotifyAdClosed()
        {
            loadRetryTimer.Reset();
            LoadRewardAd();
            SafeInvoke(AdClosed);
        }

        protected void NotifyAdRewarded()
        {
            SafeInvoke(AdRewarded);
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

        private void OnDestroy()
        {
            loadRetryTimer?.Cancel();
            if (isInitialised)
                DisposeProvider();

            AdLoaded = null;
            LoadFailed = null;
            ShowFailed = null;
            AdClosed = null;
            AdRewarded = null;
        }
    }
}
