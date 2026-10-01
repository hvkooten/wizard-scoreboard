# Android scoreboard toolbar regression

Status: manual verification required. Service/unit tests do not exercise Android native measurement or rendering.

## Setup

- Use an Android device or emulator and record the app build, Android version, screen size, and text scaling.
- Disable **Simplified buttons** in Settings.
- Start a test game and complete the first round, so **Edit last round** is available.

## Repeated mode changes

1. Confirm the text buttons include Next Round, Edit last round, Pause, and End Game.
2. Enable Simplified buttons and return to the scoreboard.
3. Confirm the Edit, Pause, and End icons are all visible, equally sized square touch targets (48 logical units), and aligned to the right. Next Round must remain separate on the left, without overlapping the icons.
4. Tap Edit and confirm the latest-round correction dialog opens. Cancel without changing scores.
5. Disable Simplified buttons and return. Confirm all available actions have their full text labels and normal text-button sizing.
6. Repeat steps 2-5 at least five times. Edit must appear on the first and every subsequent switch; no icons may disappear or alternate between rectangular and square sizing.
- At each switch, Start Game and Next Round must never both be visible. Before starting a game only Start Game is available; during an active, unpaused game only Next Round is available; while paused only Start Game is available.

## State and sizing checks

- Pause the game: the Pause icon must become Resume. Edit and Next Round must be hidden while paused. Toggle both modes, then resume and confirm the completed round is editable again.
- Start another round, finish it, and confirm Edit still opens the latest completed round.
- Cancel the End Game confirmation and confirm the game and toolbar are unchanged.
- Restart the app with Simplified buttons enabled and reopen a saved game; verify the same icon sizing and alignment.
- Repeat on the narrowest supported portrait screen, in landscape, and with larger system text. Check that Next Round remains readable and does not overlap the secondary icons.
- Repeat with bold text enabled and disabled and with light and dark themes.
- With TalkBack enabled, verify the Edit, Pause/Resume, and End icons announce their localized action names.

Attach screenshots before and after toggling and record any failures; do not mark native Android rendering as verified based only on a successful build or unit-test run.
