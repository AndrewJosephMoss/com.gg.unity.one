using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GG.Unity.Advertisement
{
    /// <summary>
    /// Resolves the player's ad consent (from storage, or by asking through a
    /// <see cref="ConsentProvider"/>), then hands it to <see cref="AdsManager.SetConsent"/>.
    /// The ad SDK is not initialised until this has run, so no ad request is ever made
    /// before consent is known.
    /// </summary>
    public class ConsentFlow : MonoBehaviour
    {
        public event Action<AdConsent> ConsentResolved;

        [SerializeField, Tooltip("How the player is asked. E.g. PanelConsentProvider for your own popup.")]
        private ConsentProvider provider;

        [SerializeField, Min(1), Tooltip("Bump to ask everyone again, e.g. when your privacy policy or ad partners change.")]
        private int policyVersion = 1;

        [SerializeField, Min(0), Tooltip("Ask again after this many days. 0 = a decision never expires. Check the right value with your legal counsel.")]
        private int reaskAfterDays = 365;

        [SerializeField, Tooltip("Treat every player as a child (COPPA / Google Play Families). Skips the prompt; personalised ads are always off.")]
        private bool childDirected;

        [SerializeField, Tooltip("On iOS, show the App Tracking Transparency prompt after the player accepts personalised ads.")]
        private bool requestIosTrackingAuthorisation = true;

        [SerializeField, Tooltip("Resolve consent (and so initialise ads) in Start. Turn off if another script calls ResolveConsent().")]
        private bool resolveOnStart = true;

        private CancellationTokenSource lifetime;

        public bool IsBusy { get; private set; }
        public bool HasResolved { get; private set; }
        public AdConsent Current { get; private set; } = AdConsent.Restrictive();

        private void Awake() => lifetime = new CancellationTokenSource();

        private void Start()
        {
            if (resolveOnStart)
                ResolveConsent();
        }

        private void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }

        /// <summary>Use the stored decision if there is a valid one, otherwise ask. Then initialise ads.</summary>
        public void ResolveConsent() => _ = RunAsync(forcePrompt: false);

        /// <summary>Always ask, e.g. from a "Privacy settings" button. Dismissing keeps the current choice.</summary>
        public void ChangeConsent() => _ = RunAsync(forcePrompt: true);

        private async Task RunAsync(bool forcePrompt)
        {
            if (IsBusy)
                return;

            IsBusy = true;
            CancellationToken token = lifetime.Token;
            try
            {
                AdConsent? consent = await ResolveAsync(forcePrompt, token);
                if (consent.HasValue && !token.IsCancellationRequested)
                    Apply(consent.Value);
            }
            catch (OperationCanceledException)
            {
                // Destroyed mid-flow (e.g. scene unloaded). Nothing to apply.
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                // A broken consent UI must not leave the game without ads, and must not
                // assume consent: fall back to the most restrictive settings.
                if (!forcePrompt && !token.IsCancellationRequested)
                    Apply(AdConsent.Restrictive(childDirected));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<AdConsent?> ResolveAsync(bool forcePrompt, CancellationToken token)
        {
            if (childDirected)
            {
                // Children can't opt in to personalised ads, so there is nothing to ask.
                return AdConsent.Restrictive(isChildDirected: true);
            }

            bool? decision;
            if (!forcePrompt && ConsentStore.TryLoad(policyVersion, reaskAfterDays, out bool stored))
            {
                decision = stored;
            }
            else
            {
                decision = await AskAsync(token);

                if (decision.HasValue)
                    ConsentStore.Save(policyVersion, decision.Value);
                else if (forcePrompt)
                    return null; // dismissed from settings: keep the current choice

                // Dismissed on first run without choosing: no personalised ads this session,
                // nothing stored, so we ask again next launch.
            }

            bool personalised = decision == true;
            bool tracking = personalised
                && await TrackingAuthorisation.ResolveAsync(requestIosTrackingAuthorisation, token);

            return new AdConsent(personalised, isChildDirected: false, trackingAuthorised: tracking);
        }

        private async Task<bool?> AskAsync(CancellationToken token)
        {
            if (provider == null)
            {
                Debug.LogError($"{name}:{GetType().Name} has no ConsentProvider; treating as declined.", this);
                return null;
            }
            return await provider.RequestConsentAsync(token);
        }

        private void Apply(AdConsent consent)
        {
            Current = consent;
            HasResolved = true;
            Debug.Log($"[ConsentFlow] Resolved {consent}");

            var ads = AdsManager.Instance;
            if (ads == null)
                Debug.LogError("[ConsentFlow] No AdsManager found; ads will not initialise.", this);
            else
                ads.SetConsent(consent);

            try { ConsentResolved?.Invoke(consent); }
            catch (Exception e) { Debug.LogException(e, this); }
        }
    }
}
