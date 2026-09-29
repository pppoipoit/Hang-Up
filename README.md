# 🚫 HangUp
### Internet Blocker for Design & Engineering Apps — Windows Edition
**by pppoipoit x DRKMTTR Studio © 2026**

> Cut internet access for Adobe, Autodesk, Corel, and SolidWorks — with a single GUI toggle.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)]()
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS-blue)]()
[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?logo=windows)]()
[![macOS](https://img.shields.io/badge/macOS-Intel%20%7C%20Apple%20Silicon-black?logo=apple)]()
[![License](https://img.shields.io/badge/License-CC%20BY--NC--ND%204.0-lightgrey)]()
[![Release](https://img.shields.io/badge/Release-v1.0.0-orange)]()

[![Repo Size](https://img.shields.io/github/repo-size/pppoipoit/Hang-Up?color=purple)]()
[![Last Commit](https://img.shields.io/github/last-commit/pppoipoit/Hang-Up?color=orange)]()
[![Stars](https://img.shields.io/github/stars/pppoipoit/Hang-Up?color=yellow)]()
[![Forks](https://img.shields.io/github/forks/pppoipoit/Hang-Up?color=teal)]()
[![Issues](https://img.shields.io/github/issues/pppoipoit/Hang-Up?color=red)]()

[![No Telemetry](https://img.shields.io/badge/Telemetry-None-brightgreen)]()
[![No Ads](https://img.shields.io/badge/Ads-None-brightgreen)]()
[![GUI Only](https://img.shields.io/badge/Terminal-Not%20Needed-2a9d8f)]()
[![Adobe](https://img.shields.io/badge/Adobe-Supported-FF0000?logo=adobe)]()
[![Autodesk](https://img.shields.io/badge/Autodesk-Supported-0696D7?logo=autodesk)]()
[![Corel](https://img.shields.io/badge/Corel-Supported-00B140)]()
[![SolidWorks](https://img.shields.io/badge/SolidWorks-Supported-D81E05)]()

---

## ❓ What is this?

**HangUp** is a one-click network blocker for design and engineering software.
Creative suites such as Adobe Creative Cloud, Autodesk, CorelDRAW, and
SolidWorks constantly phone home to telemetry and license servers — which can
make large apps feel sluggish and trigger repeated activation attempts.

HangUp adds **outbound block rules** for just those applications. Your browser,
email, music, and everything else stay online.

## ✨ Features

- **Per-app toggles** — block/unblock Adobe, Autodesk, SolidWorks, and Corel independently.
- **Block All / Unblock All** — one-click control for everything at once.
- **Instant effect** — rules apply immediately via the Windows Firewall API; no reboot.
- **Standalone portable `HangUp.exe`** — self-contained, embeds the .NET 8 runtime, no installation required.
- **Modern dark glassmorphism UI** — custom GDI+ rendering.
- **No telemetry, no ads, no accounts** — fully offline operation.


## 💻 Requirements

| Item | Requirement |
|---|---|
| OS | Windows 10 or Windows 11 (64-bit) |
| Privileges | **Administrator required** — firewall rules are a system-level change |
| Disk | ~170 MB for the self-contained executable |
| Network | Not required after download |

> ⚠️ Without Administrator rights the app cannot create firewall rules. It will
> silently do nothing, which looks like a bug — always right-click → **Run as
> administrator**.

## 🚀 Installation (Windows)

1. Download `HangUp-Windows-x64.exe` from the
   [latest release](https://github.com/pppoipoit/Hang-Up/releases).
2. Right-click the file → **Run as administrator**.
3. Windows SmartScreen may warn about an unrecognised publisher — choose
   **More info** → **Run anyway** (the app is unsigned).
4. Toggle any application to start blocking.

> No installer, no registry entries, no startup items. Delete the `.exe` to
> uninstall.

## 🍎 macOS Edition

The macOS version is a **separate codebase** on a different branch, because
macOS has no equivalent of the Windows application firewall:

- Switch to the [`macos`](https://github.com/pppoipoit/Hang-Up/tree/macos) branch
- 📥 Download: `HangUp-macOS-arm64.app.zip` (Apple Silicon) or
  `HangUp-macOS-x86_64.app.zip` (Intel)

## 🛠 Building from source

```powershell
git clone https://github.com/pppoipoit/Hang-Up.git
cd HangUp

# Portable single-file executable
# -> src/HangUp.App/bin/Release/net8.0-windows/win-x64/publish/HangUp.exe
dotnet publish src/HangUp.App/HangUp.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Or build the whole solution
dotnet build HangUp.sln -c Release
```

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows.

## 🏗 Project structure

```
HangUp.sln
src/
  HangUp.App/     UI (custom GDI+ dark glassmorphism) + entry point
  HangUp.Core/    Firewall rule logic, profile store, apps.json config
  HangUp.Tests/   Unit tests
assets/           App icons + apps.json (blockable app definitions)
docs/             Architecture, handoff, and known-issues notes
```

## ⚙️ How it works

Blocking is done through the **Windows Defender Firewall COM API**
(`INetFwPolicy2`), creating rules of the form `HangUp_Block_{AppName}_{index}`
with `Action=Block`, `Direction=Outbound`. Unblocking removes those rules.

Install paths and domain lists per application live in
[`assets/apps.json`](assets/apps.json) — easy to extend or trim.

> ⚠️ Application install paths change between major versions. If a preset stops
> working after a software update, that app's path list likely needs updating.

## ⚠️ Disclaimer

**Use at your own risk.** HangUp modifies your system firewall configuration and
requires Administrator privileges. While blocking telemetry and license
check-ins is generally harmless, interfering with software activation can cause
activation failures or unsupported states in vendor products. HangUp is not
affiliated with, endorsed by, or supported by Adobe, Autodesk, Dassault
Systèmes, or Corel. Always keep a backup of your firewall configuration and
ensure you can still reach the internet before relying on this tool.

## 👥 Credits & Contributors

- Project Lead / Core Developer: **pppoipoit** (DRKMTTR Studio)
- Development Assistant: **Annie** (Google Antigravity)
- Windows Build & Testing: *TBD*
- macOS Build & Testing: *TBD*

See [`CREDITS.md`](CREDITS.md) for the full list and third-party acknowledgements.

## 📄 License

This project is licensed under **CC BY-NC-ND 4.0**.

**Free for personal and educational use only.** You are **NOT** permitted to
sell, redistribute for profit, or use this software commercially. You may not
distribute modified versions. You must credit the developers.

Full legal text: [`LICENSE`](LICENSE)

