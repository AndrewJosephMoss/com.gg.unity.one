using System;
using System.Collections;
using UnityEngine;

namespace GG.Unity.Timers
{
    /// <summary>
    /// Runs a callback after an exponentially growing delay (with jitter).
    /// Call Schedule() after each failure, and Reset() after a success.
    /// </summary>
    public class BackOffTimer
    {
        private readonly MonoBehaviour anchor;
        private readonly Action onComplete;
        private readonly float baseDelaySeconds;
        private readonly float maxDelaySeconds;
        private readonly float jitterFraction;

        private int attemptCount;
        private Coroutine coroutine;

        public bool IsPending => coroutine != null && anchor != null;

        public int AttemptCount => attemptCount;

        public BackOffTimer(
            MonoBehaviour anchor,
            Action onComplete,
            float baseDelaySeconds = 2f,
            float maxDelaySeconds = 45f,
            float jitterFraction = 0.2f)
        {
            this.anchor = anchor;
            this.onComplete = onComplete;
            this.baseDelaySeconds = baseDelaySeconds;
            this.maxDelaySeconds = maxDelaySeconds;
            this.jitterFraction = jitterFraction;
        }

        /// <summary>
        /// Schedules the callback after the next backoff delay.
        /// Does nothing if one is already pending.
        /// </summary>
        public void Schedule()
        {
            if (anchor == null)
            {
                Debug.LogError("BackOffTimer: anchor MonoBehaviour is null or destroyed.");
                return;
            }

            if (IsPending)
                return;

            float delay = GetBackoffDelay(attemptCount);
            attemptCount++;
            coroutine = anchor.StartCoroutine(WaitAndInvoke(delay));
        }

        public void Cancel()
        {
            if (coroutine != null && anchor != null)
                anchor.StopCoroutine(coroutine);

            coroutine = null;
        }

        /// <summary>Cancels any pending callback and restarts the backoff from the base delay.</summary>
        public void Reset()
        {
            Cancel();
            attemptCount = 0;
        }

        private IEnumerator WaitAndInvoke(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            coroutine = null;
            onComplete?.Invoke();
        }

        private float GetBackoffDelay(int attempt)
        {
            float delay = baseDelaySeconds * Mathf.Pow(2f, Mathf.Min(attempt, 10));
            delay = Mathf.Min(delay, maxDelaySeconds);

            // Jitter avoids synchronized retries across many devices.
            float jitter = delay * jitterFraction * UnityEngine.Random.Range(-1f, 1f);

            return Mathf.Clamp(delay + jitter, 0.5f, maxDelaySeconds);
        }
    }
}