using UnityEngine;

namespace GG.Unity.Advertisement
{
    public class InterstitialAdOpportunity : MonoBehaviour
    {
        [SerializeField]
        private bool declareOnAwake;

        [SerializeField]
        private bool overrideMinWaitTime;

        private void Awake()
        {
            if (declareOnAwake)
                DeclareInterstitialAdOpportunity();
        }

        public void DeclareInterstitialAdOpportunity()
        {
            AdsManager.Instance?.InterstitialAdsController.DeclareInterstitialAdOpportunity(overrideMinWaitTime);
        } 
    }
}
