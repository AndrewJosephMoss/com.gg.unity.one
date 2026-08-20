using System.Collections.Generic;

namespace GG.Unity.Clickable
{
    public class ClickContext : IClickContext
    {
        public List<IClickable> ContextClickables { get; private set; } = new List<IClickable>();

        public void InitialiseClickContext(IClickable clickable)
        {
            ContextClickables.Clear();
            ContextClickables.Add(clickable);
        }
    }
}
