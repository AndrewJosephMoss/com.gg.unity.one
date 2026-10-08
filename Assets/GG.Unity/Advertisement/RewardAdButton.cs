using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GG.Unity.Advertisement
{
    public abstract class RewardAdButton : MonoBehaviour
    {
        public UnityEvent RewardGranted;

        [SerializeField]
        private GameObject InternetUnreachablePopup;

        [SerializeField]
        private GameObject RewardGrantedDueToAdFailure;

        [SerializeField]
        private GameObject AdFailedPopup;

        [SerializeField]
        private int attemptsRequiredForReward = 3;

        [SerializeField]
        private float maxWaitSeconds = 10;

        [SerializeField]
        private bool grantRewardIfAdUnavailable = true;

        // RewardAdButtons storing to the same path will jointly accumulate attempt counts
        [SerializeField]
        private string attemptsBasePath = "attempts_";

        protected bool IsWaitingForAd {  get; private set; }
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

        private void OnEnable()
        {
            AttachToRewardAdEvents();
        }

        private void OnDisable()
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
            if (AdsManager.Instance == null || IsWaitingForAd)
                return;

            if (!IsInternetReachable())
            {
                HandleInternetNotReachable();
                return;
            }

            IsWaitingForAd = true;
            SetAdLoadingState(true);

            bool shownImmediately = AdsManager.Instance.RewardAds.ShowRewardAd();
            if (shownImmediately)
            {
                ResetAttemptsIfNeeded();
                SetAdLoadingState(false);
                return;
            }

            waitTimeoutCoroutine = StartCoroutine(WaitForAdOrTimeout());

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
            Instantiate(InternetUnreachablePopup);
        }

        protected virtual void HandleAdUnavailable()
        {
            CancelWait();
            SetAdLoadingState(false);

            int attempts = Attempts + 1;

            if (grantRewardIfAdUnavailable && attempts >= attemptsRequiredForReward)
            {
                ResetAttemptsIfNeeded();
                Instantiate(RewardGrantedDueToAdFailure);
                RewardGranted?.Invoke();
            }
            else
            {
                Attempts = attempts;
                Instantiate(AdFailedPopup);
            }
        }

        protected virtual void OnAdLoaded()
        {
            if (!IsWaitingForAd)
                return;

            bool shown = AdsManager.Instance != null && AdsManager.Instance.RewardAds.ShowRewardAd();
            if (shown)
            {
                CancelWait();
                SetAdLoadingState(false);
                ResetAttemptsIfNeeded();
            }
            else
            {
                // Rare race: became unready again between events. Let the timeout
                // keep running rather than failing early on one bad tick.
            }
        }
        
        protected virtual void OnAdShowFailed()
        {
            if (!IsWaitingForAd)
                return;

            HandleAdUnavailable();
        }

        protected virtual void OnAdRewarded()
        {
            CancelWait();
            RewardGranted?.Invoke();
        }

        protected virtual void OnAdClosed()
        {
            if (IsWaitingForAd)
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
