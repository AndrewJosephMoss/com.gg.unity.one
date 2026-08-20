using System.Collections.Generic;
using UnityEngine;

namespace GG.Unity.Clickable
{
    public interface IClickContext
    {
        List<IClickable> ContextClickables { get; }

        void InitialiseClickContext(IClickable clickable);
    }
}

