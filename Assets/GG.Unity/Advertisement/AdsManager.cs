using UnityEngine;

namespace GG.Unity.Advertisement
{
    /// <summary>
    /// Singleton for access to advertisement scripts
    /// There should be a single instance of each advertisement script in existence.
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance = null;

        [SerializeField]
        private BannerAds bannerAds;
        public BannerAds BannerAds => bannerAds;

        [SerializeField]
        private InterstitialAds interstitialAds;
        public InterstitialAds InterstitialAds => interstitialAds;

        [SerializeField]
        private RewardAds rewardAds;
        public RewardAds RewardAds => rewardAds;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(Instance);
            }
            else
            {
                Destroy(this.gameObject);
            }
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
        }
        #endregion
    }
}
