using System.Collections.Generic;
namespace GG.Unity.Clickable
{
    public interface IClickablesContext
    {
        IReadOnlyCollection<IClickable> ContextClickables { get; }

        bool HasSelection { get; } 

        void AddClickableToContext(IClickable clickable);

        void RemoveClickableFromContext(IClickable clickable);
    }
}

