# HangUp macOS - TODO & Progress Tracker 📝

This file tracks the current state of the project. **AI Developers: Update this file as you make progress.**

## 🟢 Completed (Phase 1: Foundation)
- [x] Initialized Avalonia UI Project (`HangUp.Mac.App` & `HangUp.Mac.Core`).
- [x] Copied and adapted Windows Models & Configs (`AppProfile`, `AppData.cs`).
- [x] Implemented `MacFirewallManager.cs` utilizing `osascript` to prompt the user natively for `sudo` privileges.
- [x] Added logic to read/write block markers in `/etc/hosts`.
- [x] Created `MainWindow.axaml` with AcrylicBlur/Glassmorphism design.
- [x] Initialized Git repository for tracking changes (The ultimate "Undo" system).

## 🟢 Completed (Phase 2: UI Binding & Polish)
- [x] Bind `MainWindow.axaml` to `MainWindowViewModel` and `AppItemViewModel`.
- [x] Connect the ToggleSwitch in the UI to trigger `MacFirewallManager.BlockAppAsync` and `UnblockAppAsync`.
- [x] Implement UI state persistence (load current block states when app launches by scanning `/etc/hosts`).
- [x] Implemented real-time stats calculation (Blocked count, Allowed count, Total domain rules, Blocked ratio).
- [x] Implemented `BlockAllAsync` and `UnblockAllAsync` batch commands.

## 🟢 Completed (Phase 3: Build & Release)
- [x] Written PowerShell script (`build-mac.ps1`) to compile and package the output into standalone `.app` bundles & `.zip` packages.
- [x] Published for Apple Silicon (`HangUp-AppleSilicon.app` / `HangUp-AppleSilicon.zip`).
- [x] Published for Intel Mac (`HangUp-Intel.app` / `HangUp-Intel.zip`).

## 🛠 Current Known Issues / Notes
* When transferring `.app` or extracting `.zip` on macOS for the first time without an Apple Developer certificate, run `xattr -cr /path/to/HangUp-*.app` or right-click -> Open to bypass Gatekeeper quarantine.

---
## ⏪ UNDO SYSTEM (How to revert if you break something)
We use **Git** as our undo system. 
1. If you make a mistake and the project won't compile, run: `git checkout .` (This resets all uncommitted changes).
2. **AI Developers:** ALWAYS run `git commit -am "Short description"` after completing a major task so there is a safe restore point!
