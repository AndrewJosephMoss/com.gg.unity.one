using System;
using UnityEngine;

namespace GG.Unity.IAP
{
    [Serializable]
    public sealed class IAPSettings
    {
        [Header("Init / reconnect retry (exponential backoff)")]
        [SerializeField] private float baseRetryDelaySeconds = 2f;
        [SerializeField] private float maxRetryDelaySeconds = 60f;
        [SerializeField, Range(0f, 0.5f)] private float jitterFraction = 0.2f;

        [Header("Timeouts")]
        [SerializeField] private float productFetchTimeoutSeconds = 20f;

        public float BaseRetryDelaySeconds => baseRetryDelaySeconds;
        public float MaxRetryDelaySeconds => maxRetryDelaySeconds;
        public float JitterFraction => jitterFraction;
        public float ProductFetchTimeoutSeconds => productFetchTimeoutSeconds;
    }
}
