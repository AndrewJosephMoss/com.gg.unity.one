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
            if (topBanner != null || bottomBanner != null)
                return; // already initialised

            Debug.Log("LevelPlay: BannerAds initialisation");
            string topBannerId = bannerIdsProvider.Ref.TopBannerAdUnitId;
            string bottomBannerId = bannerIdsProvider.Ref.BottomBannerAdUnitId;
            topBanner = InitialiseBanner(topBannerId, LevelPlayAdSize.BANNER, LevelPlayBannerPosition.TopCenter);
            bottomBanner = InitialiseBanner(bottomBannerId, LevelPlayAdSize.BANNER, LevelPlayBannerPosition.BottomCenter);

            // Show any banners whose opportunities were declared before init (e.g. during the consent popup).
            RefreshBanners();
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
            bannerAd.HideAd(); // Prevent auto show of loaded ad
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

        // Replaces the old OnDestroy, which hid Unity's call to the base class guard
        // and so hid the live banners whenever a duplicate was destroyed.
        protected override void DisposeBanners()
        {
            topBanner?.DestroyAd();
            bottomBanner?.DestroyAd();
            topBanner = null;
            bottomBanner = null;
        }
    }
}
