namespace GG.Unity.UserInterface.Panels
{
    /// <summary>
    /// Per-instance behaviour for an open panel. Created by
    /// <see cref="PanelDefinition.CreateController"/> each time a panel opens,
    /// so it can safely hold state (unlike the shared definition asset).
    /// </summary>
    public interface IPanelController
    {
        /// <summary>
        /// Called after the panel is added to the hierarchy. Runtime arguments are in
        /// <see cref="Panel.Args"/>. The controller may close its own panel from here.
        /// </summary>
        void OnOpen(Panel panel);

        /// <summary>
        /// Called once when the panel is closed, before its close transition runs.
        /// The UI is still attached, so values can be read from it. <see cref="Panel.Result"/> is set.
        /// </summary>
        void OnClose(Panel panel);
    }
}
