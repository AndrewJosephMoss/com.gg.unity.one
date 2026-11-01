using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GG.Unity.Advertisement
{
    /// <summary>
    /// Asks the player for consent. Swap implementations without touching the rest of the
    /// flow: e.g. <c>PanelConsentProvider</c> (your own popup) or a Google UMP / certified-CMP
    /// provider for EEA, UK and Swiss traffic.
    /// </summary>
    public abstract class ConsentProvider : MonoBehaviour
    {
        /// <summary>
        /// Shows the consent UI and waits for the player.
        /// Returns true (personalised ads allowed), false (declined), or null (dismissed
        /// without making a choice).
        /// </summary>
        public abstract Task<bool?> RequestConsentAsync(CancellationToken cancellationToken);
    }
}
