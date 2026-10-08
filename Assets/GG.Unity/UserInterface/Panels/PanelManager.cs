using System.Collections.Generic;
using UnityEngine;

namespace GG.Unity.UserInterface.Panels
{
    public class PanelManager
    {
        // If panel manager is instanced how should objects like RewardAdButton access it?
        private readonly Dictionary<string, GameObject> open = new();

        public GameObject Open(string id, GameObject prefab, Transform parent)
        {
            if (open.TryGetValue(id, out var existing) && existing != null)
                return existing;
            var go = Object.Instantiate(prefab, parent);
            open[id] = go;
            return go;
        }
    }
}
