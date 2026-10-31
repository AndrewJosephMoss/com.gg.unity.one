# Panel system sample

A launcher HUD plus four panels that exercise the panel system end to end.

## Setup

1. Copy the `PanelSample` folder anywhere under `Assets/`, keeping its `Scripts`, `UI` and `Editor` subfolders.
2. Make sure the panel system scripts, `PanelSystem.uss` and `StatusButton.cs` are in the project.
3. Run **Tools > GG.Unity > Panels > Create Sample Scene**.
4. Press Play in the new `PanelSample.unity`.

The builder creates the PanelDefinition assets in `PanelSample/Definitions` and wires the scene. Running it again updates the assets in place and rebuilds the scene.

If you use assembly definitions, the `Editor` folder needs an Editor-only asmdef that references your panel, button and sample assemblies (and `Unity.InputSystem`).

## What to try

| Launcher button | Shows |
| --- | --- |
| Menu (StatusButtons) | Non-modal Window panel. Play / Settings / Help just `Debug.Log`. Save and Sync are StatusButtons bound to a per-panel model; they block clicks and focus while Loading, then show a toast. Delete save opens a nested, awaited confirm. |
| Message (asset text) | Modal Popup with text from the asset. Closes on OK, Esc or a backdrop click. |
| Message (runtime args) | Same panel and asset, text passed through `MessageArgs`. |
| Confirm (awaited result) | `OpenPanelAsync<bool>`; the result appears under the launcher. |
| Toast (overlay layer) | Non-modal, non-focusing Overlay panels that stack and auto-close. |
| Back / Close all | `PanelManager.CloseTopPanel()` and `CloseAllPanels()`. |

Things worth checking:

- With a modal open, Tab and the arrow keys can't move focus into the launcher (focus trap).
- Esc (or gamepad B) closes the top panel. With nothing to close, `PanelBackInput` raises its unhandled event and the launcher reports it.
- Closing the menu mid-save cancels the save, and closing it while Delete save's dialog is open closes the dialog too.
- Every panel open and close is logged with a `[PanelSample]`, `[Menu]` or `[Confirm]` prefix.
