# HangUp - Project Concept & Architecture 🧠

## 1. What is HangUp?
HangUp is a lightweight, one-click firewall manager designed specifically for designers and 3D artists. Its primary purpose is to block applications (like Adobe CC, Autodesk, SolidWorks, Corel) from communicating with their license servers or telemetry endpoints, ensuring they remain offline while the rest of the computer stays online.

## 2. Windows vs macOS Architectures
The original Windows version modifies the **Windows Defender Firewall** using COM objects (`NetFwTypeLib`) to block outbound traffic for specific `.exe` paths. 

However, **macOS handles networking entirely differently**. 
* macOS Application Firewall (ALF) only blocks *incoming* connections.
* macOS `pf` (Packet Filter) operates on ports/IPs, NOT application paths. 
* Writing a Network Extension (like Little Snitch) is incredibly complex and requires Apple Developer approvals.

**Our macOS Solution:**
Instead of blocking `.app` paths, we target the actual problem:
1. **Domain Blocking (`/etc/hosts`)**: We hijack DNS requests for known telemetry/license domains (e.g., `adobe.io`, `autodesk.com`) and route them to `127.0.0.1`. This is 100% effective and doesn't require a constantly running background service.
2. **Service Blocking (`launchctl`)**: We can optionally unload background daemons that try to bypass hosts or run locally (e.g., `com.adobe.AGMService`).

## 3. UI Concept (Avalonia UI)
We use **Avalonia UI** for the macOS app. Why?
* It allows us to code in C# and design in XAML.
* It supports cross-compilation: We can build the macOS `.app` entirely from a Windows machine!
* It natively supports macOS vibrancy/glassmorphism via `TransparencyLevelHint="AcrylicBlur"`.

**Design Language:**
* "Dark Mode Neo-Brutalism/Modern"
* Smooth gradients, glass effects, and minimalistic toggles.
* The UI should wow the user at first glance.

## 4. Intel vs Apple Silicon (M1/M2/M3) Support
* **Mac Intel (x64)**: Fully supported natively without any emulation.
* **Apple Silicon (ARM64)**: Fully supported natively! **You do NOT need Rosetta.** .NET 8 can compile directly to `osx-arm64`, meaning the app will run at lightning speed on modern Macs.
* We will provide two separate builds (or a Universal Binary if we write a custom build script).
