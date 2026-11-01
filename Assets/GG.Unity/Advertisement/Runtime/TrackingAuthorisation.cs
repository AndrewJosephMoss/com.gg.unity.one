using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR && GG_IOS_ATT
using Unity.Advertisement.IosSupport;
#endif

namespace GG.Unity.Advertisement
{
    /// <summary>
    /// iOS App Tracking Transparency. Requires the "iOS 14 Advertising Support" package
    /// (com.unity.ads.ios-support), the GG_IOS_ATT scripting define on iOS, and an
    /// NSUserTrackingUsageDescription entry in Info.plist. On every other platform, or
    /// without the define, tracking counts as authorised.
    /// </summary>
    public static class TrackingAuthorisation
    {
        /// <summary>
        /// Returns whether tracking is authorised. If the status is undetermined and
        /// <paramref name="requestIfUndetermined"/> is set, shows the system prompt and waits.
        /// </summary>
        public static Task<bool> ResolveAsync(bool requestIfUndetermined, CancellationToken cancellationToken,
            float timeoutSeconds = 60f)
        {
#if UNITY_IOS && !UNITY_EDITOR && GG_IOS_ATT
            return ResolveOnIosAsync(requestIfUndetermined, cancellationToken, timeoutSeconds);
#else
            return Task.FromResult(true);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR && GG_IOS_ATT
        private static async Task<bool> ResolveOnIosAsync(bool requestIfUndetermined,
            CancellationToken cancellationToken, float timeoutSeconds)
        {
            var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();

            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED && requestIfUndetermined)
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();

                // The binding has no completion callback in all versions, so poll. The timeout
                // stops ads being blocked forever if the system never shows the prompt.
                float deadline = Time.realtimeSinceStartup + timeoutSeconds;
                while (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED
                       && Time.realtimeSinceStartup < deadline)
                {
                    await Task.Delay(250, cancellationToken);
                    status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                }
            }

            return status == ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;
        }
#endif
    }
}
