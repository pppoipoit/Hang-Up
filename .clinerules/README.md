# .clinerules — Hang Up (Windows Firewall Manager)

> **อ่านก่อน session ทุกครั้ง** — ไฟล์นี้รวม workflow + code quality + safety rules

---

## 0. เริ่ม Session

```
1. อ่าน docs/KNOWN_ISSUES.md    ← ปัญหาที่รู้แล้ว
2. อ่าน docs/HANDOFF.md         ← สถานะโปรเจกต์
3. อ่าน docs/CURRENT_TASK.md    ← งานที่ต้องทำ
4. เปิด HangUp.sln ใน VS 2022   ← IDE หลัก
5. Build → Run ยืนยัน baseline  ← ก่อนแก้ใดๆ
```

## 1. Tech Stack

| Layer | Technology |
|---|---|
| Language | C# (.NET 8) |
| UI | Custom GDI+ rendering (ไม่ใช่ WPF/WinForms standard) |
| Style | Dark glassmorphism |
| Firewall API | Windows Firewall with Advanced Security (netsh / COM API) |
| Solution | HangUp.sln → HangUp.App, HangUp.Core, HangUp.Tests |
| Distribution | Single-file portable exe (embed .NET 8 runtime) |
| Admin | ต้องการสิทธิ์ Administrator เสมอ (แก้ Firewall rules) |

## 2. Solution Structure

```
HangUp.sln
├── src/
│   ├── HangUp.App/      ← WinMain, UI rendering (GDI+)
│   ├── HangUp.Core/     ← Firewall logic, rule management
│   └── HangUp.Tests/    ← Unit tests
├── docs/                ← HANDOFF.md, CURRENT_TASK.md, KNOWN_ISSUES.md
└── assets/              ← Icons, images
```

## 3. Build & Run

```bash
# Build (ต้องมี .NET 8 SDK)
dotnet build HangUp.sln

# Run (ต้องการ Admin!)
# คลิกขวา → Run as Administrator
# หรือใน VS: Debug → Start as Administrator

# Run Tests
dotnet test HangUp.Tests/

# Build portable exe (single-file + embedded runtime)
dotnet publish HangUp.App -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## 4. กฎสำคัญ

### ห้ามทำเด็ดขาด
- ❌ ห้ามแก้ Firewall rule logic ใน HangUp.Core โดยไม่มี test
- ❌ ห้ามเพิ่ม software ใน preset list โดยไม่ตรวจว่า paths ถูกต้อง (Adobe/Autodesk/Corel paths เปลี่ยนทุก version)
- ❌ ห้ามใช้ `Process.Start("netsh", ...)` แบบ raw string concat → SQL injection equivalent
- ❌ ห้าม hardcode paths ของ software (เช่น `C:\Program Files\Adobe\...`) — ต้องใช้ environment variables หรือ Registry lookup
- ❌ ห้ามแก้ GDI+ rendering code โดยไม่ทดสอบบน multiple DPI settings

### ต้องทำทุกครั้ง
- ✅ รันในโหมด Admin เสมอขณะ dev (ไม่งั้นการเรียก Firewall API จะ fail เงียบๆ)
- ✅ รัน `dotnet test` ผ่านก่อน commit
- ✅ อัพเดท HANDOFF.md และ CURRENT_TASK.md ก่อนปิด session

## 5. Target Software Presets (Context สำคัญ!)

Hang Up บล็อก "call home" ของซอฟต์แวร์สำหรับดีไซเนอร์/ช่างภาพ:
- **Adobe** (Photoshop, Illustrator, Premiere, etc.) → บล็อก telemetry + license server
- **Autodesk** (AutoCAD, Maya, etc.) → บล็อก license check servers
- **SolidWorks** → บล็อก license activation
- **Corel** → บล็อก telemetry

paths ของ software เหล่านี้เปลี่ยนทุก major version — ต้องระวังเวลาอัพเดท preset list

## 6. Admin Requirement

App ต้องการสิทธิ์ Admin เพราะ Windows Firewall API ไม่อนุญาต non-admin access
- Manifest ต้องมี `requireAdministrator`
- ถ้าไม่มี Admin → Firewall calls จะ throw `UnauthorizedAccessException` หรือ return false โดยไม่มี error
