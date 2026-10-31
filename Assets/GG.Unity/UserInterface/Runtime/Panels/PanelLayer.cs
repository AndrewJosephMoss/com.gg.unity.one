namespace GG.Unity.UserInterface.Panels
{
    public enum PanelLayer
    {
        Hud = 0,        // persistent, mostly non-interactive panels
        Window = 100,   // menus and full screens
        Popup = 200,    // dialogs and confirmations
        Overlay = 300   // toasts, tooltips, loading screens
    }
}
