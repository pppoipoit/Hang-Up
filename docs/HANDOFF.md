# Project Handoff — Hang Up (macOS Edition)

**Last updated**: 2026-09-17 (Santa Claude cross-project audit)
**Status**: 🟢 Phase 1-4 Complete — พร้อม release / QA
**Owner**: DRKMTTR Studio (Tokenmee)

> **นี่คือ Mac edition** — คนละ codebase จาก Windows edition โดยสิ้นเชิง
> ดู Windows edition ที่ `../HangUp/docs/HANDOFF.md`
> อ่าน `../README.md` และ `../PROJECT_CONCEPT.md` ก่อนแก้โค้ดเสมอ (มี AI guidelines ละเอียดมากอยู่แล้ว)

---

## Project Snapshot

### What this is
**Hang Up (macOS)** — เวอร์ชัน Mac ของ Firewall Manager สำหรับดีไซเนอร์/ช่างภาพ
เป้าหมายเดียวกับ Windows edition (บล็อก Adobe/Autodesk/SolidWorks/Corel telemetry)
แต่ **กลไกบล็อกต่างกันโดยสิ้นเชิง** เพราะข้อจำกัดของ macOS

### ทำไมต้องคนละสถาปัตยกรรมกับ Windows

macOS **ไม่รองรับ path-based outbound firewall blocking** แบบ Windows Firewall COM API:
- macOS Application Firewall (ALF) บล็อกได้แค่ *incoming* connections
- macOS `pf` (Packet Filter) ทำงานที่ระดับ port/IP ไม่ใช่ application path
- เขียน Network Extension (แบบ Little Snitch) ซับซ้อนมาก ต้องขอ Apple Developer approval

**กลยุทธ์ที่ใช้แทน**:
1. **Domain Blocking** — เขียนทับ `/etc/hosts` ให้ domain telemetry/license (เช่น `adobe.io`, `autodesk.com`) ชี้ไปที่ `127.0.0.1`
2. **Service Blocking** — unload background daemon ที่พยายาม bypass hosts (เช่น `com.adobe.AGMService`) ด้วย `launchctl bootout`

### Tech Stack

| Component | Technology | หมายเหตุ |
|---|---|---|
| Language | C# (.NET 8) | เหมือน Windows แต่ logic ต่างกันหมด |
| UI Framework | **Avalonia UI** | ไม่ใช่ WPF ไม่ใช่ MAUI — เขียน XAML แต่ cross-platform |
| Blocking Method | `/etc/hosts` rewrite + `launchctl` | ไม่ใช้ pfctl บล็อก .app path เด็ดขาด |
| Privilege escalation | `osascript -e 'do shell script "..." with administrator privileges'` | prompt sudo แบบ native macOS |
| Target platforms | Apple Silicon (arm64) + Intel (x64) | แยก build คนละไฟล์ **ไม่ต้อง Rosetta** |
| Build | Cross-compile จาก **Windows** ได้ | ผ่าน `dotnet publish -r osx-arm64/osx-x64` |
| Design | Dark Mode Neo-Brutalism/Modern, AcrylicBlur glassmorphism | `TransparencyLevelHint="AcrylicBlur"` |

### Solution Structure

```
HangUp.Mac/
├── HangUp.Mac.slnx
├── .clinerules              ← มีอยู่แล้ว (single file)
├── README.md                ← AI guidelines ละเอียดมาก (อ่านก่อนเสมอ)
├── PROJECT_CONCEPT.md       ← อธิบาย WHY ของสถาปัตยกรรม
├── TODO.md                  ← Progress tracker (ทำหน้าที่ CURRENT_TASK.md)
├── build-mac.ps1            ← Build script (PowerShell, รันบน Windows ได้)
├── dist/                    ← Build output (.app + .zip)
├── .git/                    ← มี Git repo แยกต่างหาก (ใช้เป็น undo system)
└── src/
    ├── HangUp.Mac.Core/     ← Business logic
    │   ├── Config/          ← apps.json config
    │   ├── Firewall/        ← MacFirewallManager.cs (หัวใจของระบบ)
    │   └── Models/          ← AppProfile.cs ฯลฯ
    └── HangUp.Mac.App/      ← Avalonia UI
        ├── Views/           ← .axaml files
        ├── ViewModels/      ← MVVM bindings
        └── Assets/          ← icons .png
```

---

## Current Product State

### ✅ Phase 1: Foundation — COMPLETE
- Avalonia UI Project initialized (`HangUp.Mac.App` + `HangUp.Mac.Core`)
- Copied/adapted Windows Models & Configs (`AppProfile`, `AppData.cs`)
- `MacFirewallManager.cs` ใช้ `osascript` prompt sudo แบบ native
- Logic อ่าน/เขียน block markers ใน `/etc/hosts`
- `MainWindow.axaml` ด้วย AcrylicBlur/Glassmorphism design
- Git repo init (ใช้เป็น undo system)

### ✅ Phase 2: UI Binding & Polish — COMPLETE
- Bind `MainWindow.axaml` → `MainWindowViewModel` + `AppItemViewModel`
- ToggleSwitch เชื่อมกับ `MacFirewallManager.BlockAppAsync` / `UnblockAppAsync`
- UI state persistence (โหลดสถานะ block ปัจจุบันจากการสแกน `/etc/hosts` ตอนเปิดแอป)
- Real-time stats (Blocked count, Allowed count, Total domain rules, Blocked ratio)
- `BlockAllAsync` / `UnblockAllAsync` batch commands

### ✅ Phase 3: Build & Release — COMPLETE
- `build-mac.ps1` — compile + package เป็น `.app` bundle + `.zip`
- Published: Apple Silicon (`HangUp-AppleSilicon.app/.zip`)
- Published: Intel Mac (`HangUp-Intel.app/.zip`)

### ✅ Phase 4: Elevation & Robust Blocking — COMPLETE
- แก้ deadlock ใน `osascript` execution (อ่าน stdout/stderr พร้อมกันผ่าน `Task.WhenAll`)
- เปลี่ยนจาก fragile `sed` scripts → atomic C# in-memory hosts management + `cp` สะอาดจาก `/tmp`
- ย้าย temp execution script ไปที่ `/tmp` แทน `/var/folders/` (แก้ macOS TCC/Darwin permission rejection)
- เพิ่ม user cancellation detection (`-128`) + status message สีต่างๆ (Red/Green/Blue/Orange)

### 🔴 ยังไม่ทำ (จาก TODO.md ต้นฉบับ + README.md Next Steps)
- [ ] Universal Binary (รวม Apple Silicon + Intel เป็นไฟล์เดียว) — ตอนนี้แยก build
- [ ] Custom `.app` bundle builder ที่สมบูรณ์ (Info.plist, Contents/MacOS structure) — ตอนนี้ทำ manual/บางส่วน
- [ ] Logic parse/block LaunchD agents เพิ่มเติม (ยังไม่ครบตามที่ README ระบุไว้ท้ายๆ)
- [ ] Apple Developer code signing จริง (ตอนนี้ใช้ ad-hoc sign `codesign --sign -`)

---

## Critical Notes for Cline (สำคัญมาก — จาก README.md ต้นฉบับ)

1. **ห้ามใช้ `pfctl` บล็อก `.app` path เด็ดขาด** — ไม่ใช่กลยุทธ์ที่ถูกต้องสำหรับโปรเจกต์นี้
2. **UI ทดสอบบน Windows ได้** — รัน `dotnet run --project src/HangUp.Mac.App/HangUp.Mac.App.csproj` แล้ว `MacFirewallManager` จะ mock การเรียก `osascript` อัตโนมัติถ้าตรวจพบว่ารันบน Windows
3. **Core logic (Firewall) ต้องเทสต์บน Mac จริงหรือ macOS VM เท่านั้น** — เทสต์บน Windows ได้แค่ UI
4. **ใช้ Git เป็น undo system** — ถ้าโค้ด compile ไม่ผ่าน: `git checkout .` (reset uncommitted changes)
   **AI Developer ต้องรัน `git commit -am "..."` หลังทำงานเสร็จแต่ละ major task เสมอ**
5. **Avalonia UI ไม่ใช่ WPF ไม่ใช่ MAUI** — bind data ด้วย `ReactiveUI` หรือ `INotifyPropertyChanged` มาตรฐาน

---

## Build Commands (Cross-compile จาก Windows)

```bash
# Apple Silicon (M1/M2/M3) — ไม่ต้อง Rosetta
dotnet publish "src/HangUp.Mac.App/HangUp.Mac.App.csproj" -c Release -r osx-arm64 --self-contained true

# Intel Mac
dotnet publish "src/HangUp.Mac.App/HangUp.Mac.App.csproj" -c Release -r osx-x64 --self-contained true

# หรือใช้ script สำเร็จรูป (แนะนำ)
./build-mac.ps1
```

⚠️ **หมายเหตุจาก README ต้นฉบับ**: `dotnet publish` เพียวๆ จะได้ Unix executable แต่ไม่ได้ `.app` bundle โครงสร้างเต็ม (Contents/MacOS, Contents/Resources, Info.plist) — ต้องจัดโฟลเดอร์เองหรือใช้ `dotnet-bundle`

---

## Recent Changes

| วันที่ | สิ่งที่เปลี่ยน | ทำไม | ไฟล์ |
|---|---|---|---|
| 2026-09-17 | สร้าง docs/HANDOFF.md + KNOWN_ISSUES.md (มาตรฐานเดียวกับโปรเจกต์อื่น) | Santa Claude audit — TODO.md มีอยู่แล้วแต่ยังไม่ตรง format มาตรฐาน | docs/ |
| 2026-09-13 | Phase 4 complete (elevation fixes) | แก้ deadlock + permission issues | Firewall/MacFirewallManager.cs |
| 2026-08-08 | Phase 1-3 complete | Initial build | ทั้งโปรเจกต์ |

---

## Validation Status

| ขั้นตอน | คำสั่ง | ผลล่าสุดที่ทราบ |
|---|---|---|
| UI test (บน Windows) | `dotnet run --project src/HangUp.Mac.App/...` | ✅ ทดสอบได้ (mock osascript) — [รอ verify session ถัดไป] |
| Firewall logic test | ต้องรันบน Mac จริง | ❓ ไม่ทราบผลล่าสุด — verify บน Mac |
| Build Apple Silicon | `build-mac.ps1` | ✅ ตาม TODO.md Phase 3 |
| Build Intel | `build-mac.ps1` | ✅ ตาม TODO.md Phase 3 |

---

## Where to Resume

1. อ่าน `docs/KNOWN_ISSUES.md` (ไฟล์นี้คู่กัน)
2. อ่าน `../TODO.md` — progress tracker ต้นฉบับ (ยังใช้งานคู่กับ docs/ ชุดใหม่ได้)
3. อ่าน `../README.md` — AI Developer Guidelines เต็ม (ห้ามข้าม)
4. อ่าน `../PROJECT_CONCEPT.md` — เข้าใจ WHY ก่อนแก้ Firewall logic
