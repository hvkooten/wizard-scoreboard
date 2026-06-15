# Wizard Scoreboard

[![CI](https://github.com/hvkooten/wizard-scoreboard/actions/workflows/ci.yml/badge.svg)](https://github.com/hvkooten/wizard-scoreboard/actions/workflows/ci.yml)
[![Android Release](https://github.com/hvkooten/wizard-scoreboard/actions/workflows/android-release.yml/badge.svg)](https://github.com/hvkooten/wizard-scoreboard/actions/workflows/android-release.yml)

Wizard Scoreboard is a .NET MAUI app for tracking points in the Wizard card game.

## What This App Does

- Create and manage player groups.
- Track bids, actual tricks, and score progression per round.
- Apply configurable Wizard scoring rules.
- Store group-specific settings (including bid rule behavior).
- Support multiple languages.

## Android Download

- Latest APK: [Download the latest Android build](https://github.com/hvkooten/wizard-scoreboard/releases/latest/download/wizard-scoreboard-android.apk)
- Version history: [Version overview](VERSION_OVERVIEW.md)

## How To Install On Android

1. Download the APK from the link above.
2. Open the downloaded file on your Android device.
3. If prompted, allow installation from unknown sources for your browser or file manager.
4. Complete the install and open Wizard Scoreboard.

## Release Process

- Push a version tag in the format `vX.Y.Z` (example: `v1.2.0`).
- The Android release workflow will:
	- Validate that `vX.Y.Z` is higher than the current source version.
	- Build a Release Android APK with that version embedded.
	- Publish a GitHub Release and upload the APK.
	- Update `VERSION_OVERVIEW.md` by adding the new release link at the top.
