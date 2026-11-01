using System;
using UnityEngine;

namespace GG.Unity.Advertisement
{
    /// <summary>Persists the player's consent decision together with the policy version it was given for.</summary>
    public static class ConsentStore
    {
        private const string DecisionKey = "gg_ads_consent_personalised";
        private const string VersionKey = "gg_ads_consent_policy_version";
        private const string DecidedAtKey = "gg_ads_consent_decided_utc_ticks";

        /// <summary>
        /// Loads a stored decision. Fails if there is none, it was given for a different policy
        /// version, or it is older than <paramref name="maxAgeDays"/> (0 = never expires).
        /// </summary>
        public static bool TryLoad(int policyVersion, int maxAgeDays, out bool personalisedAdsAllowed)
        {
            personalisedAdsAllowed = false;

            if (!PlayerPrefs.HasKey(DecisionKey))
                return false;
            if (PlayerPrefs.GetInt(VersionKey, -1) != policyVersion)
                return false;

            if (maxAgeDays > 0)
            {
                DateTime? decidedAt = DecidedAtUtc;
                if (!decidedAt.HasValue)
                    return false;

                TimeSpan age = DateTime.UtcNow - decidedAt.Value;
                // A negative age means the clock was moved back; don't trust it, ask again.
                if (age < TimeSpan.Zero || age > TimeSpan.FromDays(maxAgeDays))
                    return false;
            }

            personalisedAdsAllowed = PlayerPrefs.GetInt(DecisionKey) == 1;
            return true;
        }

        public static void Save(int policyVersion, bool personalisedAdsAllowed)
        {
            PlayerPrefs.SetInt(DecisionKey, personalisedAdsAllowed ? 1 : 0);
            PlayerPrefs.SetInt(VersionKey, policyVersion);
            PlayerPrefs.SetString(DecidedAtKey, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save(); // a consent record must survive a crash or force-quit
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(DecisionKey);
            PlayerPrefs.DeleteKey(VersionKey);
            PlayerPrefs.DeleteKey(DecidedAtKey);
            PlayerPrefs.Save();
        }

        public static DateTime? DecidedAtUtc
        {
            get
            {
                string stored = PlayerPrefs.GetString(DecidedAtKey, null);
                if (string.IsNullOrEmpty(stored) || !long.TryParse(stored, out long ticks))
                    return null;
                return new DateTime(ticks, DateTimeKind.Utc);
            }
        }
    }
}
