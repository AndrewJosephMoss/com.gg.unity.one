using GG.Unity.Serialization;
using Unity.Services.LevelPlay;

namespace GG.Unity.Advertisement.LevelPlay
{
    public class LevelPlayInterstitialAds : InterstitialAds
    {
        public InterfaceRef<ILevelPlayInterstitialAdUnitIdProvider> interstitialIdsProvider;

        public override bool IsReady => interstitialAd != null && interstitialAd.IsAdReady();

        private ILevelPlayInterstitialAd interstitialAd;

        protected override void InitialiseProvider()
        {
            string interstitialAdId = interstitialIdsProvider.Ref.InterstitialAdUnitId;
            var config = new LevelPlayInterstitialAd.Config.Builder()
                .SetBidFloor(0.4)
                .Build();
            interstitialAd = new LevelPlayInterstitialAd(interstitialAdId, config);

            interstitialAd.OnAdLoaded += HandleAdLoaded;
            interstitialAd.OnAdLoadFailed += HandleAdLoadFailed;
            interstitialAd.OnAdDisplayFailed += HandleAdDisplayFailed;
            interstitialAd.OnAdClosed += HandleAdClosed;
        }

        protected override void LoadAd() => interstitialAd.LoadAd();

        protected override void ShowAd() => interstitialAd.ShowAd();

        private void HandleAdLoaded(LevelPlayAdInfo _) => NotifyLoadSucceeded();
        private void HandleAdLoadFailed(LevelPlayAdError _) => NotifyLoadFailed();
        private void HandleAdDisplayFailed(LevelPlayAdInfo _, LevelPlayAdError __) => NotifyShowFailed();
        private void HandleAdClosed(LevelPlayAdInfo _) => NotifyAdClosed();

        protected override void DisposeProvider()
        {
            if (interstitialAd == null)
                return;

            interstitialAd.OnAdLoaded -= HandleAdLoaded;
            interstitialAd.OnAdLoadFailed -= HandleAdLoadFailed;
            interstitialAd.OnAdDisplayFailed -= HandleAdDisplayFailed;
            interstitialAd.OnAdClosed -= HandleAdClosed;
            interstitialAd.DestroyAd();
        }
    }
}