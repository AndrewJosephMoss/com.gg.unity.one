using UnityEngine;


namespace GG.Unity.Advertisement.LevelPlay
{
    [CreateAssetMenu(fileName = "LevelPlayIdsProvider", menuName = "Advertisement/LevelPlay/LevelPlayIdsProvider")]
    public class LevelPlayIdsProvider : ScriptableObject, 
        ILevelPlayAppIdProvider, ILevelPlayBannerAdUnitIdProvider, ILevelPlayInterstitialAdUnitIdProvider, ILevelPlayRewardedAdUnitIdProvider
    {
#if UNITY_ANDROID
        [SerializeField]
        private string androidAppId = "b972f7a5";

        [SerializeField]
        private string androidTopBannerId = "1vsqi5144iui8n6l";

        [SerializeField]
        private string androidBottomBannerId = "ygd5kjp4b6u76k6o";

        [SerializeField]
        private string androidInterstitialAdId = "b15n7w0d18xm5kwz";

        [SerializeField]
        private string androidRewardAdId = "3qmuoxx1nqrh8jd8";
#endif

#if UNITY_IPHONE
        [SerializeField]
        private string appleAppId = "1f63257bd";

        [SerializeField]
        private string iosTopBannerId = "7bfamxvaurjeozc4";

        [SerializeField]
        private string iosBottomBannerId = "4ym0hjqhs587jasp";

        [SerializeField]
        private string iosInterstitialAdId = "wb1n9xm2elk14un5";

        [SerializeField]
        private string iosRewardAdId = "ns232vyphanew4ks";
#endif 

        public string AppId
        {
            get
            {
#if UNITY_ANDROID
                return androidAppId;
#elif UNITY_IPHONE
                return appleAppId;
#else
                Debug.LogError("Unsupported platform for LevelPlay ads");
                return "";
#endif
            }
        }

        public string TopBannerAdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return androidTopBannerId;
#elif UNITY_IOS
                return iosTopBannerId;
#else
                Debug.LogError("Unsupported platform for Banner Ad ID");
                return null;
#endif
            }
        }

        public string BottomBannerAdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return androidBottomBannerId;
#elif UNITY_IOS
                return iosBottomBannerId;
#else
                Debug.LogError("Unsupported platform for Banner Ad ID");
                return null;
#endif
            }
        }

        public string InterstitialAdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return androidInterstitialAdId;
#elif UNITY_IOS
                return iosInterstitialAdId;
#else
                Debug.LogError("Unsupported platform for Interstitial Ad ID");
                return null;
#endif
            }
        }

        public string RewardedAdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return androidRewardAdId;
#elif UNITY_IOS
                return iosRewardAdId;
#else
                Debug.LogError("Unsupported platform for Rewarded Ad ID");
                return null;
#endif
            }
        }
    }

    public interface ILevelPlayAppIdProvider
    {
        string AppId { get; }
    }

    public interface ILevelPlayBannerAdUnitIdProvider
    {
        string TopBannerAdUnitId { get; }
        string BottomBannerAdUnitId { get; }
    }

    public interface ILevelPlayInterstitialAdUnitIdProvider
    {
        string InterstitialAdUnitId { get; }
    }

    public interface ILevelPlayRewardedAdUnitIdProvider
    {
        string RewardedAdUnitId { get; }
    }
}
