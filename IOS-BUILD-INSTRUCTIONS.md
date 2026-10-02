# Creating an iOS build for WizardScoreboard

## When an iOS build is possible

An iOS build can be created when a compatible Mac is available as the Apple build host. This can be either:

- Visual Studio running on a Mac, or
- Visual Studio Enterprise 2026 on Windows paired with a Mac build host.

An attached iPhone by itself is not enough to compile or sign an iOS app. Windows needs the paired Mac because Xcode and Apple’s iOS build tools run on macOS. The Mac must have an Xcode version supported by the installed Visual Studio/.NET 10 iOS workload, and the iOS workload must be installed and ready.

## Project status

`src/scoreboard/WizardScoreboard.csproj` already targets `net10.0-ios`; its iOS minimum OS version is 15.0. The iOS platform folder includes `AppDelegate.cs`, `HeaderLabelHandler.cs`, `Program.cs`, and `Info.plist`. The iOS build has not yet been verified on a Mac.

## Prepare the Windows-to-Mac build connection

1. Start the Mac, install a Visual Studio-supported Xcode version, launch Xcode once, and accept its first-run license/components prompts.
2. Ensure the Mac has the .NET 10 iOS build workload required by the installed Visual Studio version. Keep Xcode and Visual Studio's MAUI/iOS tooling versions compatible.
3. On Windows, open `WizardScoreboard.slnx` in Visual Studio Enterprise 2026.
4. Use **Tools > iOS > Pair to Mac** and connect to the Mac. Complete any authentication prompts. Keep the Mac awake and reachable during build/deploy.
5. If pairing or workload checks fail, resolve those before trying to build; a connected iPhone does not replace the Mac build host.

## Build and test in Visual Studio

1. Set `src/scoreboard/WizardScoreboard.csproj` (`WizardScoreboard`) as the startup project.
2. Select the `net10.0-ios` target framework and **Debug** configuration.
3. For an initial compile check, select an iOS Simulator target and build the project. A simulator build does not require an iPhone provisioning profile.
4. Start the simulator target to verify installation and launch. Then, if a physical iPhone is available, select it as the deployment target and run the app there.
5. Check the Visual Studio Build and Debug output for errors. Record the Mac/Xcode and device/simulator versions with the result.

## Signing requirements

- **Simulator:** use an iOS Simulator target; device provisioning is not required.
- **Physical iPhone:** sign with an Apple development certificate and a provisioning profile that includes the app's bundle identifier and the device. Configure signing through Visual Studio's iOS bundle-signing settings; do not commit certificates, private keys, or passwords to the repository.
- **Ad Hoc, TestFlight, or App Store distribution:** use the appropriate Apple distribution certificate and provisioning profile, then create/archive and distribute the build through Visual Studio's supported Apple distribution workflow. An Apple Developer Program account is generally needed for these distribution paths.

Before device deployment or distribution, confirm the app has the intended stable bundle identifier and that the provisioning profile matches it. Do not infer or change the production identifier without the project owner's confirmation.

## WizardScoreboard checks after the first successful iOS build

- Confirm the app launches on both simulator and, if available, a physical iPhone.
- Check portrait and landscape layouts, including the `Wizard` header around safe areas/notches.
- Verify the iOS crash-report persistence and next-start offer. The Android test results in `Copilot.discussion.txt` do not establish iOS behavior.
- Record unavailable device tests as **not tested**, rather than treating a successful simulator build as a device test.

## Current blocker

The project can be built for iOS once a compatible Mac/Xcode build host is available and paired/configured. Without that Mac, iOS compilation, signing, deployment, and device verification cannot be completed from the current Windows-only setup.
