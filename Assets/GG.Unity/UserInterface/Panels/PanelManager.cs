using System;
using System.Collections.Generic;
using UnityEngine;

namespace GG.Unity.UserInterface.Panels
{
    public class PanelManager
    {
        private readonly Dictionary<PanelLayer, Transform> layers;
        private readonly Dictionary<PanelDefinition, Panel> singletons = new();
        private readonly List<Panel> stack = new();

        public event Action<Panel> PanelOpened;
        public event Action<Panel> PanelClosed;

        public PanelManager(Dictionary<PanelLayer, Transform> layers) => this.layers = layers;

        public Panel Open(PanelDefinition def)
        {
            if (def.Singleton && singletons.TryGetValue(def, out var existing) && existing != null)
            {
                existing.transform.SetAsLastSibling();
                return existing;
            }

            Panel panel = UnityEngine.Object.Instantiate(def.Prefab, layers[def.Layer]);
            panel.Init(def, this);

            if (def.Singleton) 
                singletons[def] = panel;

            stack.Add(panel);
            PanelOpened?.Invoke(panel);

            return panel;
        }

        public void Close(Panel panel)
        {
            if (panel == null) 
                return;

            stack.Remove(panel);

            if (panel.Definition.Singleton) 
                singletons.Remove(panel.Definition);

            PanelClosed?.Invoke(panel);
            UnityEngine.Object.Destroy(panel.gameObject);
        }

        public bool CloseTopPanel()
        {
            stack.RemoveAll(p => p == null);   // handles panels destroyed externally (scene unload)
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                if (stack[i].Definition.CloseOnBack)
                {
                    Close(stack[i]); 
                    return true;
                }
            }
            return false;
        }
    }
}
