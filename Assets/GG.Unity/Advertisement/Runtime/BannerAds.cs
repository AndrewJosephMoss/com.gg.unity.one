using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace GG.Unity.Advertisement
{
    public abstract class BannerAds: MonoBehaviour
    {
        public enum Placement { Top, Bottom };

        public static BannerAds Instance = null;

        private HashSet<BannerAdOpportunity> TopOpportunities = new HashSet<BannerAdOpportunity>();
        private HashSet<BannerAdOpportunity> BottomOpportunities = new HashSet<BannerAdOpportunity>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        public abstract void InitialiseBannerAds();

        public void DeclareOpportunity(BannerAdOpportunity opportunity)
        {
            if (opportunity.Placements.Contains(Placement.Top))
                TopOpportunities.Add(opportunity);
            if (opportunity.Placements.Contains(Placement.Bottom))
                BottomOpportunities.Add(opportunity);

            if (TopOpportunities.Count > 0)
                ShowBanner(Placement.Top);
            if (BottomOpportunities.Count > 0)
                ShowBanner(Placement.Bottom);
        }
        
        public void RescindOpportunity(BannerAdOpportunity opportunity)
        {
            if (opportunity.Placements.Contains(Placement.Top))
                TopOpportunities.Remove(opportunity);
            if (opportunity.Placements.Contains(Placement.Bottom))
                BottomOpportunities.Remove(opportunity);

            if (TopOpportunities.Count == 0)
                HideBanner(Placement.Top);
            if (BottomOpportunities.Count == 0)
                HideBanner(Placement.Bottom);
        }

        public abstract void ShowBanner(Placement placement);

        public abstract void HideBanner(Placement placement);

        private void OnDestroy()
        {
            HideBanner(Placement.Top);
            HideBanner(Placement.Bottom);
        }
    }
}
