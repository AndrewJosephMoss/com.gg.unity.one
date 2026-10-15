using System.Collections.Generic;
using UnityEngine;

namespace GG.Unity.UserInterface.Panels
{
    public class PanelService : MonoBehaviour
    {
        [SerializeField]
        private Transform screenLayer, popupLayer, overlayLayer;

        public PanelManager Manager { get; private set; }

        private void Awake()
        {
            Dictionary<PanelLayer, Transform> layers = new Dictionary<PanelLayer, Transform>
            {
                { PanelLayer.Screen, screenLayer },
                { PanelLayer.Popup, popupLayer },
                { PanelLayer.Overlay, overlayLayer }
            };
            Manager = new PanelManager(layers);
        }
    }
}
