using UnityEngine;

namespace GG.Unity.Advertisement
{
    public class BannerAdOpportunity: MonoBehaviour
    {
        [SerializeField]
        private BannerAds.Placement[] placements;
        public BannerAds.Placement[] Placements => placements;

        [SerializeField]
        private bool declareOnEnable = true;

        void OnEnable()
        {
            if (declareOnEnable)
            {
                DeclareOpportunity();
            }
        }

        void OnDisable()
        {
            RescindOpportunity();
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
    }
}
