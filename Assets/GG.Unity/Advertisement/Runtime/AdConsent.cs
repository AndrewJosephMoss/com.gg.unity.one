namespace GG.Unity.Advertisement
{
    /// <summary>
    /// The privacy state the ad SDK is configured with. Provider-agnostic: each
    /// <see cref="AdvertisementInitialisation"/> maps it onto its own SDK's API.
    /// </summary>
    public readonly struct AdConsent
    {
        /// <summary>
        /// The player agreed to personalised ads. Map to GDPR consent = true and
        /// US "do not sell" = false. When false, map to the most restrictive settings.
        /// </summary>
        public bool PersonalisedAdsAllowed { get; }

        /// <summary>The player must be treated as a child (COPPA / Google Play Families).</summary>
        public bool IsChildDirected { get; }

        /// <summary>
        /// iOS App Tracking Transparency was authorised. Always true on other platforms when
        /// personalised ads are allowed. Informational: ad SDKs read ATT status themselves.
        /// </summary>
        public bool TrackingAuthorised { get; }

        public AdConsent(bool personalisedAdsAllowed, bool isChildDirected, bool trackingAuthorised)
        {
            // A child can never be opted in to personalised ads.
            PersonalisedAdsAllowed = personalisedAdsAllowed && !isChildDirected;
            IsChildDirected = isChildDirected;
            TrackingAuthorised = trackingAuthorised && PersonalisedAdsAllowed;
        }

        /// <summary>No personalisation, no tracking. The safe fallback whenever consent is unknown.</summary>
        public static AdConsent Restrictive(bool isChildDirected = false) =>
            new AdConsent(false, isChildDirected, false);

        public override string ToString() =>
            $"AdConsent(personalised:{PersonalisedAdsAllowed}, child:{IsChildDirected}, tracking:{TrackingAuthorised})";
    }
}
