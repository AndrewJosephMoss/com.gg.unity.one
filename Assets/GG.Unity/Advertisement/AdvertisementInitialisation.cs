using UnityEngine;
using UnityEngine.Events;

namespace GG.Unity.Advertisement
{
    public abstract class AdvertisementInitialisation : MonoBehaviour
    {
        public UnityEvent<AdvertisementInitialisation> InitialisationComplete;
        public UnityEvent<AdvertisementInitialisation> InitialisationFailed;

        public static AdvertisementInitialisation Instance = null;

        /// <summary>
        /// There should be one Advertisement Initialisation object in existence.
        /// </summary>
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

        void Start()
        {
            Initialise();
        }

        public abstract void Initialise();
    }
}
