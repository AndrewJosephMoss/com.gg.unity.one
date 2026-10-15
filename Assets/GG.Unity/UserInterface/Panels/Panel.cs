using UnityEngine;

namespace GG.Unity.UserInterface.Panels
{
    public class Panel : MonoBehaviour
    {
        public PanelDefinition Definition { get; private set; }
        private PanelManager panelManager;

        public void Init(PanelDefinition definition, PanelManager manager)
        {
            Definition = definition;
            panelManager = manager;
        }

        public void Close() => panelManager.Close(this);
    }
}
