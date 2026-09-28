using GG.Unity.Advertisement;
using GG.Unity.Serialization;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace GG.Unity.Advertisement.LevelPlay
{
    public class LevelPlayBannerAds : BannerAds
    {
        public InterfaceRef<ILevelPlayBannerAdUnitIdProvider> bannerIdsProvider;

        private ILevelPlayBannerAd topBanner;
        private ILevelPlayBannerAd bottomBanner;

        public override void InitialiseBannerAds()
        {
            Debug.Log("LevelPlay: BannerAds initialisation");
            string topBannerId = bannerIdsProvider.Ref.TopBannerAdUnitId;
            string bottomBannerId = bannerIdsProvider.Ref.BottomBannerAdUnitId;
            topBanner = InitialiseBanner(topBannerId, LevelPlayAdSize.BANNER, LevelPlayBannerPosition.TopCenter);
            bottomBanner = InitialiseBanner(bottomBannerId, LevelPlayAdSize.BANNER, LevelPlayBannerPosition.BottomCenter);
        }

        private ILevelPlayBannerAd InitialiseBanner(string bannerId, LevelPlayAdSize size, LevelPlayBannerPosition position)
        {
            var adConfig = new LevelPlayBannerAd.Config.Builder()
                .SetSize(size)
                .SetPosition(position)
                .SetDisplayOnLoad(true)
                .SetRespectSafeArea(true)
                .Build();
            ILevelPlayBannerAd bannerAd = new LevelPlayBannerAd(bannerId, adConfig);
            bannerAd.LoadAd();
            return bannerAd;
        }

        public override void HideBanner(Placement placement)
        {
            switch (placement)
            {
                case Placement.Top:
                    topBanner?.HideAd();
                    break;
                case Placement.Bottom:
                    bottomBanner?.HideAd();
                    break;
            }
        }

        public override void ShowBanner(Placement placement)
        {
            switch (placement)
            {
                case Placement.Top:
                    topBanner?.ShowAd();
                    break;
                case Placement.Bottom:
                    bottomBanner?.ShowAd();
                    break;
            }
        }

        private void OnDestroy()
        {
            HideBanner(Placement.Top);
            HideBanner(Placement.Bottom);
        }
    }
}
