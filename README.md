# HangUp (Windows Edition) ⚡

**HangUp** is a fast, portable, one-click Windows Firewall manager tailored for designers and digital artists. It manages application firewall rules to prevent software suites (Adobe, Autodesk, SolidWorks, Corel) from communicating with telemetry and license servers without affecting normal internet connectivity.

## ✨ Features
* **Standalone Portable Executable:** Single-file `HangUp.exe` (Embedded .NET 8 runtime, zero dependencies needed).
* **Modern Dark Glassmorphism UI:** Built with custom GDI+ rendering.
* **Instant Outbound Blocking:** Uses Windows Defender Firewall API to block application executables.
* **One-Click Actions:** Independent application toggles, "Block All", and "Unblock All".

## 🚀 How to Run & Build
### Running
Just run `HangUp.exe` with Administrator privileges.

### Building from Source
```powershell
dotnet publish src/HangUp.App/HangUp.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## 🍎 macOS Edition
Looking for the macOS version? Switch to the [`macos`](https://github.com/pppoipoit/Hang-Up/tree/macos) branch.
