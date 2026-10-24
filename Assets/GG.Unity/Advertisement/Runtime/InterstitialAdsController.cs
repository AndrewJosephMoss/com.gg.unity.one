using System;
using UnityEngine;

namespace GG.Unity.Advertisement
{
    public class InterstitialAdsController : MonoBehaviour
    {
        private const string LastAdShowTimeKey = "interstitial_last_show_time_ticks_utc_";

        [SerializeField]
        private InterstitialAds interstitialAds;

        [SerializeField]
        [Tooltip("Minimum gap between interstitials, in seconds.")]
        private int minSecondsBetweenAds = 16 * 60;

        private TimeSpan MinTimeSpan => TimeSpan.FromSeconds(minSecondsBetweenAds);

        private string LastAdShowTimeKeyForDevice =>
            string.Concat(LastAdShowTimeKey, SystemInfo.deviceUniqueIdentifier);

        private DateTime LastAdShowTimeUtc
        {
            get
            {
                string stored = PlayerPrefs.GetString(LastAdShowTimeKeyForDevice, null);
                if (string.IsNullOrEmpty(stored) || !long.TryParse(stored, out long ticks))
                    return DateTime.MinValue;

                return new DateTime(ticks, DateTimeKind.Utc);
            }
            set
            {
                PlayerPrefs.SetString(LastAdShowTimeKeyForDevice, value.Ticks.ToString());
            }
        }

        private TimeSpan SpanSinceLastAd => DateTime.UtcNow - LastAdShowTimeUtc;

        private bool MinTimeHasPassed => SpanSinceLastAd > MinTimeSpan;

        private void OnEnable()
        {
            if (interstitialAds != null)
                interstitialAds.AdDisplayed += OnAdDisplayed;
        }

        private void OnDisable()
        {
            if (interstitialAds != null)
                interstitialAds.AdDisplayed -= OnAdDisplayed;
        }

        private void OnAdDisplayed()
        {
            LastAdShowTimeUtc = DateTime.UtcNow;
        }

        public void DeclareInterstitialAdOpportunity(bool overrideMinWaitTime = false)
        {
            if (interstitialAds == null)
            {
                Debug.LogWarning($"{gameObject.name}:{GetType()} has no interstitialAds assigned.", this);
                return;
            }

            if (!overrideMinWaitTime && !MinTimeHasPassed)
                return;

            interstitialAds.ShowInterstitialAd();
        }

        #region Validation
        private void OnValidate()
        {
            if (interstitialAds == null)
            {
                Debug.LogWarning($"{gameObject.name}:{GetType()} has null interstitialAds, attempting to assign it.", this);
                interstitialAds = GetComponent<InterstitialAds>();
            }
        }
        #endregion
    }
}
