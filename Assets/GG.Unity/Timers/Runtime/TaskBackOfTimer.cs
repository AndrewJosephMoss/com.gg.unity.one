using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GG.Unity.Timers
{
    /// <summary>
    /// Backoff timer with no MonoBehaviour dependency.
    /// </summary>
    public sealed class TaskBackOffTimer : IBackOffTimer
    {
        private readonly Action onComplete;
        private readonly float baseDelaySeconds;
        private readonly float maxDelaySeconds;
        private readonly float jitterFraction;
        private readonly Func<TimeSpan, CancellationToken, Task> delay;
        private readonly SynchronizationContext context;
        private readonly System.Random random = new System.Random();

        private int attemptCount;
        private CancellationTokenSource cts;

        public bool IsPending => cts != null;
        public int AttemptCount => attemptCount;

        /// <param name="delay">
        /// Injectable wait. Defaults to Task.Delay (real time, ignores Time.timeScale).
        /// Tests can pass (t, ct) => Task.CompletedTask to retry instantly.
        /// </param>
        public TaskBackOffTimer(
            Action onComplete,
            float baseDelaySeconds = 2f,
            float maxDelaySeconds = 45f,
            float jitterFraction = 0.2f,
            Func<TimeSpan, CancellationToken, Task> delay = null)
        {
            this.onComplete = onComplete;
            this.baseDelaySeconds = baseDelaySeconds;
            this.maxDelaySeconds = maxDelaySeconds;
            this.jitterFraction = jitterFraction;
            this.delay = delay ?? Task.Delay;
            context = SynchronizationContext.Current;
        }

        public void Schedule()
        {
            if (IsPending)
                return;

            var seconds = GetBackoffDelay(attemptCount);
            attemptCount++;

            var source = new CancellationTokenSource();
            cts = source;
            _ = RunAsync(TimeSpan.FromSeconds(seconds), source);
        }

        public void Cancel()
        {
            var source = cts;
            cts = null;

            if (source == null)
                return;

            source.Cancel();
            source.Dispose();
        }

        public void Reset()
        {
            Cancel();
            attemptCount = 0;
        }

        private async Task RunAsync(TimeSpan wait, CancellationTokenSource source)
        {
            try
            {
                await delay(wait, source.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (context != null)
                context.Post(_ => Complete(source), null);
            else
                Complete(source);
        }

        private void Complete(CancellationTokenSource source)
        {
            // Cancelled or replaced while the post was in flight.
            if (cts != source)
                return;

            cts = null;
            source.Dispose();

            try
            {
                onComplete?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private float GetBackoffDelay(int attempt)
        {
            float d = baseDelaySeconds * Mathf.Pow(2f, Mathf.Min(attempt, 10));
            d = Mathf.Min(d, maxDelaySeconds);

            // Jitter avoids synchronized retries across many devices.
            float jitter = d * jitterFraction * (float)(random.NextDouble() * 2.0 - 1.0);

            return Mathf.Clamp(d + jitter, 0.5f, maxDelaySeconds);
        }
    }
}
