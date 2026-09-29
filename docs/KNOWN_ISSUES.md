# ⚠️ KNOWN ISSUES — Hang Up (Santa Claude Audit 2026-09-17)

> **Cline: อ่านก่อน session ทุกครั้ง** — คู่กับ HANDOFF.md + CURRENT_TASK.md

---

## 🔴 CRITICAL — รู้ไว้ก่อนเริ่มทุก session

### ISSUE-001: Admin Rights ไม่ได้ผลแบบเงียบๆ

**Status**: ℹ️ Design constraint — รู้อยู่แล้ว
**Impact**: ถ้ารัน app โดยไม่ได้ Admin → Firewall calls จะ return false หรือ do nothing โดยไม่มี exception ชัดเจน → ดูเหมือนทำงานได้แต่ไม่มีผล

**ระวัง**: ระหว่าง dev ถ้าลืมรัน VS เป็น Admin จะ debug ไม่ได้ผลจริง
**วิธีตรวจ**: เพิ่ม check ใน startup:
```csharp
bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
    .IsInRole(WindowsBuiltInRole.Administrator);
if (!isAdmin) { /* แจ้ง error ชัดๆ */ }
```

---

### ISSUE-002: Software Paths เปลี่ยนทุก Major Version

**Status**: ⚠️ Ongoing maintenance issue
**Impact**: preset rules ที่ hardcode path จะพังทันทีถ้าผู้ใช้อัพเดทซอฟต์แวร์

**ตัวอย่าง path ที่เปลี่ยน**:
- Adobe CC 2023: `C:\Program Files\Adobe\Adobe Photoshop 2023\`
- Adobe CC 2024: `C:\Program Files\Adobe\Adobe Photoshop 2024\`
- Adobe CC 2025: `C:\Program Files\Adobe\Adobe Photoshop 2025\`

**แนวทางป้องกัน**:
- ใช้ wildcard ในชื่อโฟลเดอร์ถ้า Firewall API รองรับ
- หรือ lookup registry ของ software แทน hardcode path
- ตรวจสอบทุก major version release ของ Adobe/Autodesk

---

## 🟡 MEDIUM

### ISSUE-003: ไม่มี CHANGELOG.md

**Status**: ⚠️ ขาดหาย
**Impact**: ไม่รู้ว่า version ไหนเพิ่ม/แก้อะไร ทำให้ track regression ยาก
**Fix**: สร้าง CHANGELOG.md บันทึก version history

### ISSUE-004: ไม่ทราบสถานะ Build และ Tests ล่าสุด

**Status**: ❓ ไม่มีข้อมูล — ต้อง verify ใน TC-002
**รายละเอียด**: docs ไม่มีมาตั้งแต่ต้น ไม่มีบันทึก validation ใดๆ
**Fix**: รัน baseline ใน TC-002 แล้วอัพเดท HANDOFF.md

### ISSUE-005: GDI+ Custom Rendering — DPI ต่างกันอาจมีปัญหา

**Status**: ⚠️ ยังไม่ verify
**รายละเอียด**: Custom GDI+ rendering อ่อนไหวต่อ DPI settings
เครื่องที่ใช้ 125%, 150%, 200% scaling อาจเห็น UI เพี้ยน
**วิธีตรวจ**: ทดสอบบนหน้าจอ/การตั้งค่า DPI ต่างๆ ก่อน release

---

## ✅ จุดแข็ง (ไม่ต้องกังวล)

| จุด | รายละเอียด |
|---|---|
| Solution structure | ดี — แยก App/Core/Tests ชัดเจน |
| Single-file distribution | portable exe ไม่ต้อง install |
| Embed .NET runtime | รันได้ทุกเครื่อง ไม่ต้องติดตั้ง .NET แยก |

---
*Audit: Santa Claude | 2026-09-17*
