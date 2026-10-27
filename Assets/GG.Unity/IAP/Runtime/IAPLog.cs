using UnityEngine;

namespace GG.Unity.IAP
{
    internal static class IAPLog
    {
        private const string Prefix = "[GG.IAP] ";

        public static void Info(string message) => Debug.Log(Prefix + message);
        public static void Warn(string message) => Debug.LogWarning(Prefix + message);
        public static void Error(string message) => Debug.LogError(Prefix + message);
    }
}
