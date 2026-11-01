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

        // ShowRewardAd(requester), so only the object that started the ad reacts to its outcome.
        public event Action<object> ShowFailed;
        public event Action<object> AdClosed;
        public event Action<object> AdRewarded;

        private BackOffTimer loadRetryTimer;
        private bool isLoading;
        private bool isInitialised;

        private object activeRequester;
        public object ActiveRequester => activeRequester;

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

        /// <summary>
        /// Shows the ad if ready. The requester is passed back in ShowFailed, AdClosed and
        /// AdRewarded so listeners can tell whether the outcome belongs to them.
        /// </summary>
        public bool ShowRewardAd(object requester)
        {
            if (!isInitialised)
                return false;

            if (IsReady)
            {
                // Set before ShowAd(): some SDKs can call back synchronously.
                activeRequester = requester;
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
            Debug.Log($"{GetType()}:NotifyLoadSucceeded");
            isLoading = false;
            loadRetryTimer.Reset();
            SafeInvoke(AdLoaded);
        }

        protected void NotifyLoadFailed()
        {
            Debug.Log($"{GetType()}:NotifyLoadFailed");
            isLoading = false;
            loadRetryTimer.Schedule();
            SafeInvoke(LoadFailed);
        }

        protected void NotifyShowFailed()
        {
            Debug.Log($"{GetType()}:NotifyShowFailed");
            object requester = activeRequester;
            activeRequester = null;
            isLoading = false;
            LoadRewardAd();
            SafeInvoke(ShowFailed, requester);
        }

        protected void NotifyAdClosed()
        {
            Debug.Log($"{GetType()}:NotifyAdClosed");
            loadRetryTimer.Reset();
            LoadRewardAd();
            SafeInvoke(AdClosed, activeRequester);
        }

        protected void NotifyAdRewarded()
        {
            Debug.Log($"{GetType()}:NotifyAdRewarded");
            object requester = activeRequester;
            activeRequester = null;
            SafeInvoke(AdRewarded, requester);
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

        private static void SafeInvoke(Action<object> handlers, object requester)
        {
            if (handlers == null)
                return;

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try { ((Action<object>)handler)(requester); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void OnDestroy()
        {
            loadRetryTimer?.Cancel();
            if (isInitialised)
                DisposeProvider();

            activeRequester = null;
            AdLoaded = null;
            LoadFailed = null;
            ShowFailed = null;
            AdClosed = null;
            AdRewarded = null;
        }
    }
}
