using UnityEngine;
using UnityEngine.AI;
using GG.Unity.Clickable;
using System.Linq;

namespace GG.Unity.ClickToMove
{
    public class Mobile : MonoBehaviour, IMobile, IClickable
    {
        private const float IsMovingThreshold = 0.05f;
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;

        private bool isMoving = false;
        public bool IsMoving 
        {
            get {  return isMoving; }
            set
            {
                if (value != isMoving)
                {
                    isMoving = value;
                    animator.SetBool(IsMovingHash, value);
                }
            }
        }

        public void Clicked(RaycastHit hit, IClickablesContext clickablesContext)
        {
            if (clickablesContext.ContextClickables.Contains(this))
            {
                clickablesContext.RemoveClickableFromContext(this);
            }
            else if (!clickablesContext.HasSelection || clickablesContext.ContextClickables.All(c => c is IMobile))
            {
                clickablesContext.AddClickableToContext(this);
            }
        }

        public void MoveTo(Vector3 position)
        {
            Debug.Log($"Mobile: MoveTo {position}");
            agent.SetDestination(position);
        }

        private void Update()
        {
            float normalizedSpeed = agent.speed > 0f
                ? agent.velocity.magnitude / agent.speed
                : 0f;
            IsMoving = normalizedSpeed > IsMovingThreshold;
        }

        #region Validation
        void OnValidate()
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }
        #endregion
    }
}
