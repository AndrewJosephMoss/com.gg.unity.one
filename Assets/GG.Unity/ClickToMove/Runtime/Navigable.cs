using GG.Unity.Clickable;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace GG.Unity.ClickToMove
{
    public class Navigable : MonoBehaviour, IClickable
    {
        [Header("Nav settings")]
        [SerializeField] private float sampleDistance = 0.5f;

        public void Clicked(RaycastHit hit, IClickablesContext clickablesContext)
        {
            Debug.Log($"Navigable clicked");

            if (!clickablesContext.ContextClickables.Any(c => c is IMobile))
                return;

            if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, sampleDistance, NavMesh.AllAreas))
                return;

            foreach (var c in clickablesContext.ContextClickables)
            {
                if (c is IMobile)
                    (c as IMobile).MoveTo(navMeshHit.position);
            }
        }
    }
}

