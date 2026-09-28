using GG.Unity.Serialization;
using Unity.Services.LevelPlay;

namespace GG.Unity.Advertisement.LevelPlay
{
    public class LevelPlayRewardAds : RewardAds
    {
        public InterfaceRef<ILevelPlayRewardedAdUnitIdProvider> interstitialIdsProvider;
        public override bool IsReady => rewardAd != null && rewardAd.IsAdReady();

        private ILevelPlayRewardedAd rewardAd;

        protected override void InitialiseProvider()
        {
            string rewardAdId = interstitialIdsProvider.Ref.RewardedAdUnitId;
            var config = new LevelPlayRewardedAd.Config.Builder()
                .SetBidFloor(0.4)
                .Build();
            rewardAd = new LevelPlayRewardedAd(rewardAdId, config);

            rewardAd.OnAdLoaded += HandleAdLoaded;
            rewardAd.OnAdLoadFailed += HandleAdLoadFailed;
            rewardAd.OnAdDisplayFailed += HandleAdDisplayFailed;
            rewardAd.OnAdClosed += HandleAdClosed;
            rewardAd.OnAdRewarded += HandleAdRewarded;
        }

        protected override void LoadAd() => rewardAd.LoadAd();

        protected override void ShowAd() => rewardAd.ShowAd();

        private void HandleAdLoaded(LevelPlayAdInfo _) => NotifyLoadSucceeded();
        private void HandleAdLoadFailed(LevelPlayAdError _) => NotifyLoadFailed();
        private void HandleAdDisplayFailed(LevelPlayAdInfo _, LevelPlayAdError __) => NotifyShowFailed();
        private void HandleAdClosed(LevelPlayAdInfo _) => NotifyAdClosed();

        private void HandleAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            NotifyAdRewarded();
        }

        protected override void DisposeProvider()
        {
            if (rewardAd == null)
                return;

            rewardAd.OnAdLoaded -= HandleAdLoaded;
            rewardAd.OnAdLoadFailed -= HandleAdLoadFailed;
            rewardAd.OnAdDisplayFailed -= HandleAdDisplayFailed;
            rewardAd.OnAdClosed -= HandleAdClosed;
            rewardAd.OnAdRewarded -= HandleAdRewarded;
            rewardAd.DestroyAd();
        }
    }
}
