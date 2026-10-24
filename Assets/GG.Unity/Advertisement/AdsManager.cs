using UnityEngine;

namespace GG.Unity.Advertisement
{
    /// <summary>
    /// Singleton for access to advertisement scripts
    /// There should be a single instance of each advertisement script in existence.
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        [SerializeField]
        private BannerAds bannerAds;
        public BannerAds BannerAds => bannerAds;

        [SerializeField]
        private InterstitialAds interstitialAds;
        public InterstitialAds InterstitialAds => interstitialAds;

        [SerializeField]
        private InterstitialAdsController interstitialAdsController;
        public InterstitialAdsController InterstitialAdsController => InterstitialAdsController;

        [SerializeField]
        private RewardAds rewardAds;
        public RewardAds RewardAds => rewardAds;


        private static AdsManager _instance;
        public static AdsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindAnyObjectByType<AdsManager>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Validation
        private void OnValidate()
        {
            if (bannerAds == null)
            {
                Debug.LogWarning($"{gameObject.name}:{GetType()} has null bannerAds, attempting to assign it.", this);
                bannerAds = GetComponent<BannerAds>();
            }
            if (interstitialAds == null)
            {
                Debug.LogWarning($"{gameObject.name}:{GetType()} has null interstitialAds, attempting to assign it.", this);
                interstitialAds = GetComponent<InterstitialAds>();
            }
            if (rewardAds == null)
            {
                Debug.LogWarning($"{gameObject.name}:{GetType()} has null rewardAds, attempting to assign it.", this);
                rewardAds = GetComponent<RewardAds>();
            }
            if (interstitialAdsController == null)
            {
                Debug.LogWarning($"{gameObject.name}:{GetType()} has null interstitialAdsController, attempting to assign it.", this);
                interstitialAdsController = GetComponent<InterstitialAdsController>();
            }
        }
        #endregion
    }
}
