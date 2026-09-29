# Architecture — Hang Up

## System Overview

```
┌──────────────────────────────────────────────────────────────┐
│                       HangUp.App                             │
│              (UI Layer — Custom GDI+ Rendering)              │
│                                                              │
│  ┌─────────────────┐    ┌──────────────────────────────┐    │
│  │ Software List   │    │  Block / Unblock Controls    │    │
│  │ (Adobe, Auto-   │    │  Toggle per-software/        │    │
│  │  desk, Corel,   │    │  per-rule buttons            │    │
│  │  SolidWorks)    │    └──────────────────────────────┘    │
│  └─────────────────┘                                         │
│  Style: Dark glassmorphism | Custom GDI+ (ไม่ใช่ WPF ธรรมดา) │
└──────────────────────────────────┬───────────────────────────┘
                                   │ calls
                                   ↓
┌──────────────────────────────────────────────────────────────┐
│                      HangUp.Core                             │
│              (Business Logic — Firewall Management)          │
│                                                              │
│  FirewallManager                                             │
│  ├── BlockApplication(path)    → สร้าง outbound block rule   │
│  ├── UnblockApplication(path)  → ลบ rule                     │
│  ├── IsBlocked(path)           → ตรวจสถานะ                   │
│  └── GetRules()                → list rules ทั้งหมด          │
│                                                              │
│  SoftwarePresets                                             │
│  ├── AdobePreset    → { name, paths[], ruleNames[] }        │
│  ├── AutodeskPreset → { name, paths[], ruleNames[] }        │
│  ├── CorelPreset    → { name, paths[], ruleNames[] }        │
│  └── SolidWorksPreset                                        │
└──────────────────────────────────┬───────────────────────────┘
                        │ wraps                    │ tested by
                        ↓                          ↓
┌───────────────────────────────┐  ┌──────────────────────────┐
│   Windows Firewall API        │  │      HangUp.Tests        │
│   (COM / netsh layer)         │  │   (Unit Tests — xUnit    │
│   ต้องการ Admin Rights         │  │    หรือ NUnit)           │
└───────────────────────────────┘  └──────────────────────────┘
```

## Project Breakdown

### HangUp.App
- **Entry point** — WinMain / Application startup
- **UI Rendering** — Custom GDI+ (ไม่ใช่ WPF XAML / WinForms Designer)
  - วาด control ด้วย `Graphics.DrawRectangle`, `FillPath`, `DrawString` โดยตรง
  - ต้องจัดการ DPI scaling เอง (`DeviceDpi`, `CreateGraphics().DpiX`)
  - ระวัง: pixel positioning จะต่างกันบน 100% vs 125% vs 150% DPI
- **Admin check** — ตรวจสอบ `WindowsPrincipal.IsInRole(Administrator)` ตอน startup
- **Depends on**: HangUp.Core

### HangUp.Core
- **FirewallManager** — wraps Windows Firewall COM API
  - เรียกผ่าน `INetFwPolicy2` (COM interop) หรือ PowerShell cmdlets
  - Rule name format: `"HangUp_Block_{AppName}_{index}"`
- **SoftwarePresets** — ข้อมูล paths และ rule names ของแต่ละซอฟต์แวร์
  - ⚠️ ต้องอัพเดทเมื่อ software release major version ใหม่
- **Depends on**: System.Security (Windows only)

### HangUp.Tests
- ทดสอบ FirewallManager logic โดยไม่ต้องมี Firewall จริง (mock/stub)
- ทดสอบ SoftwarePresets ว่า preset data ครบถ้วนและถูกต้อง

## Firewall Rule Logic

```
User กด "Block Adobe Photoshop"
    → App calls Core.FirewallManager.BlockApplication(path)
    → Core: สร้าง INetFwRule object
    → Core: กำหนด Action=Block, Direction=Outbound, Enabled=true
    → Core: กำหนด ApplicationName=path
    → Core: เพิ่มเข้า INetFwPolicy2.Rules collection
    → Windows Firewall บล็อก exe นั้นทันที

User กด "Unblock"
    → Core.FirewallManager.UnblockApplication(path)
    → Core: หา rules ที่ ApplicationName ตรงกัน
    → Core: ลบออกจาก Rules collection
```

## Admin Rights Flow

```csharp
// Startup check (ควรทำใน Program.cs / Main)
var identity = WindowsIdentity.GetCurrent();
var principal = new WindowsPrincipal(identity);
bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

if (!isAdmin)
{
    // ตัวเลือก 1: แสดง error dialog แล้วปิด
    MessageBox.Show("Hang Up ต้องการสิทธิ์ Administrator");
    Application.Exit();
    
    // ตัวเลือก 2: relaunch ตัวเองด้วย runas
    ProcessStartInfo psi = new() { Verb = "runas", FileName = exePath };
    Process.Start(psi);
    Application.Exit();
}
```

## Build & Distribution

### Development Build
```bash
dotnet build HangUp.sln -c Debug
# Run as Admin ใน VS: Project Properties → Debug → Enable native code debugging
# หรือ: set launchSettings.json "windowsExe": true
```

### Release Build (Portable Single-File)
```bash
dotnet publish HangUp.App \
    -c Release \
    -r win-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true
```

ผลลัพธ์: `publish/HangUp.exe` (~50-80MB รวม .NET 8 runtime)

### App Manifest (ต้องมีอยู่แล้ว)
```xml
<!-- HangUp.App.manifest -->
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

## Software Paths (ตัวอย่าง — ต้องอัพเดททุก major version)

```csharp
// ตัวอย่างใน SoftwarePresets.cs (อย่า hardcode แบบนี้)
// ควรใช้ Environment.GetFolderPath + wildcard pattern แทน

// ❌ Fragile (hardcode)
var ps2024 = @"C:\Program Files\Adobe\Adobe Photoshop 2024\Photoshop.exe";

// ✅ Better (dynamic lookup)
var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
var adobeDir = Path.Combine(programFiles, "Adobe");
var photoshopPaths = Directory.GetFiles(adobeDir, "Photoshop.exe", SearchOption.AllDirectories);
```
