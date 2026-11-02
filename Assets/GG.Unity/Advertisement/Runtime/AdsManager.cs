using UnityEngine;

namespace GG.Unity.Advertisement
{
    /// <summary>
    /// Singleton for access to advertisement scripts.
    /// Owns the initialisation order: consent → SDK init → ad formats.
    /// Nothing initialises until <see cref="SetConsent"/> is called (normally by <see cref="ConsentFlow"/>).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AdsManager : MonoBehaviour
    {
        [SerializeField]
        private AdvertisementInitialisation initialisation;
        public AdvertisementInitialisation Initialisation => initialisation;

        [SerializeField]
        private BannerAds bannerAds;
        public BannerAds BannerAds => bannerAds;

        [SerializeField]
        private InterstitialAds interstitialAds;
        public InterstitialAds InterstitialAds => interstitialAds;

        [SerializeField]
        private InterstitialAdsController interstitialAdsController;
        public InterstitialAdsController InterstitialAdsController => interstitialAdsController;

        [SerializeField]
        private RewardAds rewardAds;
        public RewardAds RewardAds => rewardAds;

        private bool formatsInitialised;

        /// <summary>The consent the SDK was last configured with. Restrictive until <see cref="SetConsent"/> runs.</summary>
        public AdConsent Consent { get; private set; } = AdConsent.Restrictive();
        public bool HasConsent { get; private set; }

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

            if (initialisation != null)
                initialisation.InitialisationComplete.AddListener(OnSdkInitialised);
        }

        private void OnDestroy()
        {
            if (_instance != this)
                return;

            if (initialisation != null)
                initialisation.InitialisationComplete.RemoveListener(OnSdkInitialised);
            _instance = null;
        }

        /// <summary>
        /// Configure the SDK's privacy settings. The first call starts SDK initialisation;
        /// later calls (the player changed their choice) only update the privacy settings.
        /// </summary>
        public void SetConsent(AdConsent consent)
        {
            Consent = consent;
            HasConsent = true;

            if (initialisation == null)
            {
                Debug.LogError($"{gameObject.name}:{GetType().Name} has no AdvertisementInitialisation; ads will not initialise.", this);
                return;
            }

            initialisation.BeginInitialisation(consent);
        }

        private void OnSdkInitialised(AdvertisementInitialisation _)
        {
            if (formatsInitialised)
                return;
            formatsInitialised = true;

            if (rewardAds != null)
                rewardAds.InitialiseRewardAds();
            if (interstitialAds != null)
                interstitialAds.InitialiseInterstitialAds();
            if (bannerAds != null)
                bannerAds.InitialiseBannerAds();
        }

        #region Validation
        private void OnValidate()
        {
            if (initialisation == null)
            {
                Debug.LogWarning($"{gameObject.name}:{GetType()} has null initialisation, attempting to assign it.", this);
                initialisation = GetComponent<AdvertisementInitialisation>();
            }
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
