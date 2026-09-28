using UnityEngine;
using UnityEngine.Events;

namespace GG.Unity.Advertisement
{
    public abstract class AdvertisementInitialisation : MonoBehaviour
    {
        public UnityEvent<AdvertisementInitialisation> InitialisationComplete;
        public UnityEvent<AdvertisementInitialisation> InitialisationFailed;

        void Start()
        {
            //TODO, if initialisation fails keep trying to initialise with a back off timer
            Initialise();
        }

        public abstract void Initialise();
    }
}
