using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GG.Unity.Advertisement
{
    public abstract class RewardAdButton : MonoBehaviour
    {
        public UnityEvent RewardGranted;
        public UnityEvent RewardGrantedDueToFailedAttempts;
        public UnityEvent InternetAvailabilityNotDetected;
        public UnityEvent AdFailed;

        [SerializeField]
        private int attemptsRequiredForReward = 3;

        [SerializeField]
        private float maxWaitSeconds = 10;

        [SerializeField]
        private bool grantRewardIfAdUnavailable = true;

        // RewardAdButtons storing to the same path will jointly accumulate attempt counts
        [SerializeField]
        private string attemptsBasePath = "attempts_";

        protected bool IsWaitingForAd { get; private set; }
        private Coroutine waitTimeoutCoroutine;

        private string AttemptsPath
        {
            get
            {
                return string.Concat(attemptsBasePath, UnityEngine.Device.SystemInfo.deviceUniqueIdentifier);
            }
        }
        private int Attempts
        {
            get
            {
                return PlayerPrefs.GetInt(AttemptsPath, 0);
            }
            set
            {
                PlayerPrefs.SetInt(AttemptsPath, value);
            }
        }

        protected virtual void OnEnable()
        {
            AttachToRewardAdEvents();
        }

        protected virtual void OnDisable()
        {
            DetachFromRewardAdEvents();
            CancelWait();
        }

        private void AttachToRewardAdEvents()
        {
            if (AdsManager.Instance == null)
                return;

            AdsManager.Instance.RewardAds.AdLoaded += OnAdLoaded;
            AdsManager.Instance.RewardAds.ShowFailed += OnAdShowFailed;
            AdsManager.Instance.RewardAds.AdRewarded += OnAdRewarded;
            AdsManager.Instance.RewardAds.AdClosed += OnAdClosed;
        }

        private void DetachFromRewardAdEvents()
        {
            if (AdsManager.Instance == null)
                return;

            AdsManager.Instance.RewardAds.AdLoaded -= OnAdLoaded;
            AdsManager.Instance.RewardAds.ShowFailed -= OnAdShowFailed;
            AdsManager.Instance.RewardAds.AdRewarded -= OnAdRewarded;
            AdsManager.Instance.RewardAds.AdClosed -= OnAdClosed;
        }

        protected virtual bool IsInternetReachable()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        public void ShowRewardAd()
        {
            Debug.Log($"{name}:{GetType()}: ShowRewardAD - IsWaitingForAd:{IsWaitingForAd}");

            if (AdsManager.Instance == null || IsWaitingForAd)
                return;

            if (!IsInternetReachable())
            {
                HandleInternetNotReachable();
                return;
            }

            IsWaitingForAd = true;
            SetAdLoadingState(true);

            if (TryShowAd())
                return;

            waitTimeoutCoroutine = StartCoroutine(WaitForAdOrTimeout());
        }

        private bool TryShowAd()
        {
            bool shown = AdsManager.Instance != null && AdsManager.Instance.RewardAds.ShowRewardAd(this);
            if (shown)
            {
                CancelWait();
                SetAdLoadingState(false);
                ResetAttemptsIfNeeded();
            }
            return shown;
        }

        private IEnumerator WaitForAdOrTimeout()
        {
            yield return new WaitForSecondsRealtime(maxWaitSeconds);
            waitTimeoutCoroutine = null;

            if (!IsWaitingForAd)
                yield break;

            HandleAdUnavailable();
        }

        private void CancelWait()
        {
            if (waitTimeoutCoroutine != null)
            {
                StopCoroutine(waitTimeoutCoroutine);
                waitTimeoutCoroutine = null;
            }
            IsWaitingForAd = false;
        }

        protected abstract void SetAdLoadingState(bool isLoading);

        protected virtual void HandleInternetNotReachable()
        {
            InternetAvailabilityNotDetected?.Invoke();
        }

        protected virtual void HandleAdUnavailable()
        {
            CancelWait();
            SetAdLoadingState(false);

            int attempts = Attempts + 1;

            if (grantRewardIfAdUnavailable && attempts >= attemptsRequiredForReward)
            {
                ResetAttemptsIfNeeded();
                RewardGranted?.Invoke();
                RewardGrantedDueToFailedAttempts?.Invoke();
            }
            else
            {
                Attempts = attempts;
                AdFailed?.Invoke();
            }
        }

        private bool IsMine(object requester) => ReferenceEquals(requester, this);

        protected virtual void OnAdLoaded()
        {
            Debug.Log($"{name}:{GetType()}:OnAdLoaded");
            if (!IsWaitingForAd)
                return;

            // If this returns false we hit a rare race (became unready again between events).
            TryShowAd();
        }

        protected virtual void OnAdShowFailed(object requester)
        {
            Debug.Log($"{name}:{GetType()}:OnAdShowFailed");
            if (!IsMine(requester))
                return;

            HandleAdUnavailable();
        }

        protected virtual void OnAdRewarded(object requester)
        {
            Debug.Log($"{name}:{GetType()}:OnAdRewarded");
            if (!IsMine(requester))
                return;

            CancelWait();
            RewardGranted?.Invoke();
        }

        protected virtual void OnAdClosed(object requester)
        {
            Debug.Log($"{name}:{GetType()}:OnAdClosed");
            if (!IsMine(requester))
                return;

            CancelWait();
            SetAdLoadingState(false);
        }

        private void ResetAttemptsIfNeeded()
        {
            if (Attempts != 0)
                Attempts = 0;
        }
    }
}
