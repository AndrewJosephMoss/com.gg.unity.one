using UnityEngine;

namespace GG.Unity.Advertisement
{
    public class BannerAdOpportunity: MonoBehaviour
    {
        [SerializeField]
        private BannerAds.Placement[] placements;
        public BannerAds.Placement[] Placements => placements;

        [SerializeField]
        private bool declareOnAwake = true;

        void Awake()
        {
            if (declareOnAwake)
            {
                DeclareOpportunity();
            }
        }

        public void DeclareOpportunity()
        {
            if (BannerAds.Instance == null)
            {
                Debug.LogWarning("No BannerAds instance");
                return;
            }
            BannerAds.Instance.DeclareOpportunity(this);
        }

        public void RescindOpportunity()
        {
            if (BannerAds.Instance == null)
            {
                Debug.LogWarning("No BannerAds instance");
                return;
            }
            BannerAds.Instance.RescindOpportunity(this);
        }

        void OnDestory()
        {
            RescindOpportunity();
        }
    }
}
