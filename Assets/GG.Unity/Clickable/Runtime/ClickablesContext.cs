using System.Collections.Generic;
using UnityEngine;

namespace GG.Unity.Clickable
{
    public class ClickablesContext : IClickablesContext
    {
        private readonly HashSet<IClickable> contextClickables = new HashSet<IClickable>();
        public IReadOnlyCollection<IClickable> ContextClickables => contextClickables;

        public bool HasSelection => contextClickables.Count > 0;

        public void AddClickableToContext (IClickable clickable)
        {
            Debug.Log($"Added to clickables context {clickable}");
            contextClickables.Add(clickable);
        }

        public void RemoveClickableFromContext (IClickable clickable)
        {
            Debug.Log($"Removed from clickables context {clickable}");
            contextClickables.Remove(clickable);
        }
    }
}
