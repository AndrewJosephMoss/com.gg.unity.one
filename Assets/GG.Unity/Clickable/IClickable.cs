using UnityEngine;

namespace GG.Unity.Clickable
{
    public interface IClickable
    {
        void Clicked(IClickContext context);
    }
}
