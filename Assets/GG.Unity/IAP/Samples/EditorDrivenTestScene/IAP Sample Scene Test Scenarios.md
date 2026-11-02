# IAP Sample Scene Test Scenarios

Nov 2, 2026 · @Andrew Moss

Thirteen scenarios cover the GG.Unity.IAP wrapper through Unity IAP's Fake Store, all driven from the **IAP Sample Controller** inspector in Play Mode. Rows marked **(observe)** are cases where the Fake Store's behaviour isn't documented: write down what happens.

## Before you start

1. Run **GG.Unity ▸ IAP ▸ ****E****nter**** Samp****le Sce****ne**
2. Select the **IAP Sample Controller** GameObject in the Hierarchy.
3. Click **Reset save** so grant counts start at zero.

Results appear in the controller's event log, newest first, and in the Console when *Mirror To Console* is on. Purchase results read `→ <Status> (product, txn, error)`; service events start with `[event]`.

## Startup

| # | Scenario | Do | Expect |
| --- | --- | --- | --- |
| 1 | Initialisation | Enter Play Mode. | `StateChanged → Initialising`, then `→ Ready`, then `ProductsUpdated`. Three products appear with Fake Store prices. `OwnedProductsFetched` is probably empty on a fresh run **(observe)**. |
| 1b | Manual initialisation | Untick *Initialise On Start* on `IAPManager`, enter Play Mode, then click **Initialise**. | State stays `Uninitialised` and Buy/Restore are disabled until you click Initialise; then as scenario 1. |

## Happy paths

| # | Scenario | Do | Expect |
| --- | --- | --- | --- |
| 2 | Buy a consumable | Buy `sample.gems_100`, confirm in the Fake Store dialog. | `PurchaseAsync`, then `PurchaseConfirmed … user-initiated: True`, then `→ Success`. Granted shows 1x. |
| 3 | Buy it again | Buy `sample.gems_100` again. | Granted 2x, with a new txn ID. |
| 4 | Durable products | Buy `sample.remove_ads` and `sample.vip_monthly`. | Success, and **OWNED** next to each. What a second purchase does: **(observe)**. |
| 5 | Persistence | Exit and re-enter Play Mode. | Grant counts and OWNED markers survive (PlayerPrefs). Whether `OwnedProductsFetched` lists the durable products: **(observe)**; if not, the Fake Store doesn't keep purchases between sessions. |

In scenario 2, the txn ID in `PurchaseConfirmed` must match the one in `→ Success`. If the call hangs at "Awaiting 1 call(s)…" instead, the transaction matching needs revisiting.

## User and store failures

| # | Scenario | Do | Expect |
| --- | --- | --- | --- |
| 6 | Cancel | Buy anything, cancel in the Fake Store dialog. | `→ Cancelled`, no grant. |
| 7 | Unknown product | Click **Buy unknown id**. | `→ UnknownProduct` straight away; the store is never called. |
| 8 | Busy | Click **Buy ×2**. | Second line is `→ Busy`; the first purchase carries on normally. |
| 8b | Busy across products | Set *Grant delay* to 5s, buy one product, then Buy another during the delay. | `→ Busy`: only one user purchase can be in flight at a time. |

## Your game's failures (failure injection)

| # | Scenario | Do | Expect |
| --- | --- | --- | --- |
| 9 | Grant handler declines | Set *Next grant* to **Decline**, then buy. | `→ Failed (… Purchase handler declined)`, no grant, *Next grant* resets to Grant. The order stays **unconfirmed** in the store. |
| 9b | Re-delivery after decline | After scenario 9, exit and re-enter Play Mode. | **(observe)** If the Fake Store re-delivers: `PurchaseConfirmed … user-initiated: False` and the count goes up by one. Also shows whether pending orders arrive before or after `OwnedProductsFetched`. |
| 10 | Grant handler throws | Set *Next grant* to **Throw**, then buy. | `→ Failed` with the exception message, an error in the Console, no grant. Otherwise as scenario 9. |
| 11 | Receipt rejected | Tick *Reject Receipts* (works mid-play), then buy. Untick afterwards. | `Receipt validator: reject all`, then `→ Failed (… Receipt validation failed)`. The grant handler is never called. |
| 12 | Slow grant | Set *Grant delay* to about 5s, then buy. | "Awaiting 1 call(s)…" stays until the grant finishes; Success arrives only after the delay, so confirmation waits for your save. |

## Restore

| # | Scenario | Do | Expect |
| --- | --- | --- | --- |
| 13 | Restore | Click **Restore**. | `→ Restore succeeded` or `failed` **(observe)**. If `OwnedProductsFetched` follows, the sample handler marks those products OWNED again, even after Reset save. |

## What this scene can't test

The Fake Store can't produce these, so they need a fake `IIapBackend` and EditMode tests. They are also the paths most likely to go wrong in production.

- A store disconnect, and the init retry and backoff that follows
- Deferred or "ask to buy" orders
- Pending orders arriving before a grant handler is set
- An old order arriving in the middle of a new purchase
- The per-product Busy check and the gate while existing orders are fetched (both need an order that arrives without you buying it)
