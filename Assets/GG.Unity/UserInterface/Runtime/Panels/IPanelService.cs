using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GG.Unity.UserInterface.Panels
{
    public interface IPanelService
    {
        event Action<Panel> PanelOpened;
        event Action<Panel> PanelClosed;

        /// <summary>Open panels, bottom to top (ordered by layer, then by open order).</summary>
        IReadOnlyList<Panel> OpenPanels { get; }

        /// <summary>The topmost open panel, or null.</summary>
        Panel Top { get; }

        bool IsAttached { get; }

        /// <summary>
        /// Open a panel. Safe to call before the renderer has provided a root; the panel appears once attached.
        /// If the definition is a singleton that is already open, it is brought to front and returned (args are ignored).
        /// </summary>
        Panel Open(PanelDefinition definition, object args = null);

        /// <summary>Open a panel and wait for it to close. Returns the close result cast to T, or default.</summary>
        Task<T> OpenAsync<T>(PanelDefinition definition, object args = null, CancellationToken cancellationToken = default);

        void Close(Panel panel, object result = null);

        /// <summary>
        /// Handle a Back/Cancel request. Closes the topmost panel that allows it.
        /// Returns true if the request was consumed (a panel closed, or a modal blocked it).
        /// </summary>
        bool CloseTopPanel();

        void CloseAll();

        bool IsOpen(PanelDefinition definition);

        /// <summary>Find the topmost open instance of a definition.</summary>
        bool TryGetOpen(PanelDefinition definition, out Panel panel);
    }
}
