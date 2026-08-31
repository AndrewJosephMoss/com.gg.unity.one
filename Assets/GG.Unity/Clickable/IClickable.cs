using UnityEngine;

namespace GG.Unity.Clickable
{
    public interface IClickable
    {
        void Clicked(RaycastHit hit, IClickablesContext clickablesContext);
    }
}
