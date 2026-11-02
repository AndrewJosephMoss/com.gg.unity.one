using System;

namespace GG.Unity.IAP.Storefront
{
    /// <summary>
    /// The game's answer to "does the player own this?". Your save data is the source of truth,
    /// not the store, so the game implements this (often on the same class as IPurchaseGrantHandler).
    /// </summary>
    public interface IOwnershipProvider
    {
        /// <summary>
        /// True for granted non-consumables and active subscriptions. Never true for consumables.
        /// Must work offline / before IAP is ready, so owned items show as owned immediately.
        /// </summary>
        bool IsOwned(string productId);

        /// <summary>Raise whenever an entitlement is granted or revoked.</summary>
        event Action OwnershipChanged;
    }
}
