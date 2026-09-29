# Project Handoff — Hang Up (Windows Edition)

**Last updated**: 2026-09-17 (Santa Claude cross-project audit — REVISED after discovering Mac version)
**Status**: Active — Unknown (ไม่มี doc เดิม)
**Owner**: DRKMTTR Studio (Tokenmee)

---

## ⚠️ สำคัญมาก: นี่คือ Cross-Platform Project!

**Hang Up มี 2 เวอร์ชันคนละ codebase กันเลย** เพราะ Windows กับ macOS จัดการ Firewall คนละแบบโดยสิ้นเชิง:

| | ไฟล์นี้อธิบาย | เอกสารอีกชุด |
|---|---|---|
| **Windows Edition** | ✅ ไฟล์นี้ (`HangUp/docs/`) | — |
| **macOS Edition** | ❌ ไม่ใช่ไฟล์นี้ | ดู `HangUp.Mac/docs/HANDOFF.md` แยกต่างหาก |

**โครงสร้าง Drive จริง**:
```
Hang Up/ (root)
├── readme.md              ← Mac troubleshooting notes (Gatekeeper/codesign)
├── HangUp/                ← ⬅️ คุณอยู่ตรงนี้ (Windows .NET 8 + Firewall API)
│   └── docs/HANDOFF.md    ← ไฟล์นี้
└── HangUp.Mac/             ← Mac version (Avalonia UI + /etc/hosts blocking)
    ├── .clinerules         ← มีอยู่แล้ว (ดีกว่า Windows edition ตอนเริ่ม audit)
    ├── README.md           ← AI guidelines ละเอียดมาก
    ├── PROJECT_CONCEPT.md  ← อธิบาย WHY สถาปัตยกรรมต่างกัน
    ├── TODO.md             ← ทำหน้าที่ CURRENT_TASK.md (Phase 1-4 เสร็จหมดแล้ว ณ Sep 13)
    └── src/
        ├── HangUp.Mac.Core/  (Config/, Firewall/, Models/)
        └── HangUp.Mac.App/   (Avalonia UI)
```

**ถ้า Cline ถูกขอให้ทำงาน Mac version → ไปอ่าน `HangUp.Mac/README.md` และ `HangUp.Mac/PROJECT_CONCEPT.md` ก่อนเสมอ อย่าเอา logic ของไฟล์นี้ (Windows) ไปใช้กับ Mac เด็ดขาด เพราะกลไกบล็อกคนละแบบกันสิ้นเชิง**

---

## Project Snapshot (Windows Edition)

### What this is
**Hang Up (Windows)** — Windows Firewall Manager สำหรับดีไซเนอร์และช่างภาพ
บล็อก Adobe, Autodesk, SolidWorks, Corel ไม่ให้ส่ง telemetry หรือเรียก license server โดยไม่จำเป็น
แจกจ่ายเป็น portable exe ไฟล์เดียว embed .NET 8 runtime ไว้ในตัว

### Why it exists
ซอฟต์แวร์ Creative Suite หลายตัวส่ง telemetry และ call home บ่อยมาก ทำให้ช้าหรือ activate ซ้ำๆ
Hang Up จัดการ Windows Firewall rules แบบ GUI แทนการ run `netsh` command มือ

### Tech Stack

| Component | Technology | หมายเหตุ |
|---|---|---|
| Language | C# (.NET 8) | |
| UI | Custom GDI+ (glassmorphism dark) | ไม่ใช่ standard WPF/WinForms |
| Firewall | Windows Firewall with Advanced Security | ต้องการ Admin |
| Solution | HangUp.sln | 3 projects ย่อย |
| Distribution | Single-file portable exe | embed .NET 8 runtime |
| Admin | **ต้องการ Administrator เสมอ** | ไม่ได้จะ error ชัดเจน |

### Solution Structure

```
HangUp.sln
├── src/
│   ├── HangUp.App/      ← UI + WinMain entry point
│   ├── HangUp.Core/     ← Firewall rule logic (ส่วนสำคัญที่สุด)
│   └── HangUp.Tests/    ← Unit tests
├── docs/                ← [สร้างใหม่ 2026-09-17]
└── assets/              ← Resources
```

---

## Current Product State

### ✅ ยืนยันแล้ว (จาก audit)
- Solution structure ชัดเจน แยก App / Core / Tests
- มีไฟล์ README.md อธิบายโปรเจกต์
- มีการ update ล่าสุด Sep 2026

### ⚠️ ไม่ทราบสถานะ (ต้อง verify ใน session ถัดไป)
- สถานะ build ล่าสุด (ผ่านไหม)
- ผล unit tests ล่าสุด
- Version ปัจจุบัน
- Feature ที่ implement แล้วกับที่ยังค้าง

### ❌ ขาดหาย (ก่อน Santa Claude audit)
- HANDOFF.md, CURRENT_TASK.md, KNOWN_ISSUES.md (สร้างแล้ว 2026-09-17)
- .clinerules (สร้างแล้ว 2026-09-17)
- CHANGELOG.md

---

## Target Software Presets

Hang Up บล็อก outbound connections ของ:

| Software | บริษัท | เหตุผล |
|---|---|---|
| Photoshop, Illustrator, Premiere | Adobe | telemetry + CC license check |
| AutoCAD, Maya, 3ds Max | Autodesk | license activation server |
| SolidWorks | Dassault Systèmes | license check |
| CorelDRAW, Painter | Corel | telemetry + activation |

⚠️ **สำคัญ**: Paths ของ software เหล่านี้เปลี่ยนทุก major version ต้องอัพเดท preset list ตาม

---

## Critical Notes for Cline

1. **รันเป็น Admin เสมอ** — Firewall API ไม่ throw error ชัดเจนถ้าไม่มีสิทธิ์ แค่ "ไม่ทำงาน"
2. **ทดสอบบน real Windows** — Firewall rules ทดสอบใน VM/container ไม่ได้ผลจริง
3. **ระวัง software paths** — hardcode path จะพังทันทีถ้าผู้ใช้ install software เวอร์ชันใหม่
4. **GDI+ rendering** — ซับซ้อนกว่า WinForms ธรรมดา ดูไฟล์ใน HangUp.App ก่อนแก้ UI

---

## Recent Changes

| วันที่ | สิ่งที่เปลี่ยน | ทำไม | ไฟล์ |
|---|---|---|---|
| 2026-09-17 | สร้าง docs/ และ .clinerules/ | Santa Claude audit — ไม่มี doc เลย | docs/, .clinerules/ |
| Sep 2026 | [ล่าสุด] | [ดูจาก git log หรือ CHANGELOG] | [TBD] |

---

## Validation Status

| ขั้นตอน | คำสั่ง | ผลล่าสุด |
|---|---|---|
| Build | `dotnet build HangUp.sln` | [รอ verify ใน session ถัดไป] |
| Tests | `dotnet test HangUp.Tests/` | [รอ verify] |
| Run | รันเป็น Admin แล้วเปิด UI | [รอ verify] |

---
*Handoff created by: Santa Claude | 2026-09-17*
