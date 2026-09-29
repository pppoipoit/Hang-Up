# ⚠️ KNOWN ISSUES — Hang Up (macOS Edition)

> **Cline: อ่านก่อน session ทุกครั้ง** — คู่กับ `docs/HANDOFF.md` และ `../TODO.md`

---

## 🟡 MEDIUM — จาก TODO.md ต้นฉบับ (ยังไม่แก้)

### ISSUE-001: Gatekeeper Quarantine บล็อกการเปิดแอปครั้งแรก

**Status**: ⚠️ Known limitation — ไม่มี Apple Developer certificate
**Impact**: ผู้ใช้เปิด `.app` ครั้งแรกหลัง download/extract จะโดน macOS บล็อก (Gatekeeper)

**สาเหตุ**: แอปยังไม่ได้ sign ด้วย Apple Developer certificate จริง (ใช้ ad-hoc signing `codesign --sign -` เท่านั้น)

**วิธีแก้ชั่วคราว** (จาก `../readme.md` ต้นฉบับ):
```bash
# ลบ quarantine attribute
sudo xattr -rd com.apple.quarantine /Applications/Hangup-AppleSilicon.app
# หรือ
xattr -cr /Applications/HangUp-AppleSilicon.app

# หรือ: คลิกขวา → Open (แทน double-click)
```

**วิธีแก้ถาวร**: สมัคร Apple Developer Program ($99/ปี) แล้ว sign ด้วย certificate จริง + notarize ผ่าน `notarytool`
**Priority**: ควรทำก่อน public release จริงจัง (ตอนนี้โอเคสำหรับ internal/beta testing)

---

## 🔴 CRITICAL — ตรวจพบเพิ่มเติมจาก Santa Claude Audit (2026-09-17)

### ISSUE-002: Universal Binary ยังไม่มี — ต้องแจก 2 ไฟล์แยก

**Status**: 🔴 Not implemented (ระบุใน TODO.md Next Steps)
**Impact**: ผู้ใช้ต้องรู้เองว่าเครื่องตัวเองเป็น Apple Silicon หรือ Intel ถึงจะโหลดไฟล์ถูก
**Fix แนวทาง**: ใช้ `lipo` รวม 2 binary เป็น universal หรือใช้ script ตรวจ architecture อัตโนมัติตอน download

### ISSUE-003: .app Bundle Structure สร้างแบบ manual/บางส่วน

**Status**: ⚠️ ตามที่ README.md ต้นฉบับระบุไว้ (หมวด "How to Build")
**Impact**: `dotnet publish` เพียวๆ ไม่ได้ `.app` bundle เต็มรูปแบบ (Contents/MacOS, Contents/Resources, Info.plist) ต้องพึ่ง `build-mac.ps1` จัดการเพิ่ม
**ต้องตรวจ**: เปิด `build-mac.ps1` ดูว่า logic สร้าง bundle ครบถ้วนแค่ไหน มี Info.plist ที่ถูกต้องไหม (bundle identifier, version, permissions)

### ISSUE-004: LaunchD Agent Blocking ยังไม่ครบ

**Status**: 🔴 Not implemented (ระบุใน README.md ต้นฉบับ Next Steps ข้อสุดท้าย)
**Impact**: บาง background daemon ของ Adobe/Autodesk อาจไม่ถูก unload ครบ ถ้า service ใหม่ที่ไม่อยู่ใน list เดิม
**Fix แนวทาง**: เพิ่ม logic parse/unload LaunchD agents ให้ครอบคลุมมากขึ้น

### ISSUE-005: Cross-compile จาก Windows — โค้ดที่ยิ่ง macOS-specific ยิ่งเทสต์บน Windows ไม่ได้

**Status**: ℹ️ Design constraint — รู้อยู่แล้ว ไม่ใช่บัค
**รายละเอียด**: `MacFirewallManager` mock ตัวเองอัตโนมัติเมื่อรันบน Windows (ตาม README) — แปลว่า **การเทสต์บน Windows ทดสอบได้แค่ UI ไม่ได้ทดสอบว่า `/etc/hosts` เขียนถูกจริงไหม, `osascript` prompt ทำงานจริงไหม**
**ระวัง**: อย่าเข้าใจผิดว่า "UI ทดสอบผ่านบน Windows" = "Firewall logic ใช้งานได้จริง" ต้อง verify บน Mac จริงเสมอก่อน release

---

## ✅ แก้แล้วอย่างดี (Phase 4 — verified จาก TODO.md)

| ปัญหาเดิม | วิธีแก้ที่ใช้ |
|---|---|
| Process deadlock ตอนรัน osascript | อ่าน stdout+stderr พร้อมกันด้วย `Task.WhenAll` |
| `sed` script แก้ hosts ไม่เสถียร | เปลี่ยนเป็น atomic C# in-memory + `cp` จาก `/tmp` |
| macOS TCC/Darwin permission reject | ย้าย temp script จาก `/var/folders/` → `/tmp` |
| ไม่รู้ว่า user cancel sudo prompt | เพิ่ม cancellation detection code `-128` |

---

## 📋 สรุป Action Items เรียงตามความสำคัญ

1. 🔴 **ก่อน release จริง**: แก้ ISSUE-001 (code signing) — สำคัญที่สุดสำหรับ user experience
2. 🟡 **ควรทำ**: ISSUE-004 (LaunchD coverage) — ป้องกัน telemetry รั่ว
3. 🟡 **Nice to have**: ISSUE-002 (Universal Binary) — ลดความสับสนผู้ใช้
4. ℹ️ **รู้ไว้เสมอ**: ISSUE-005 — อย่า confuse Windows-tested กับ Mac-verified

---
*Audit: Santa Claude | 2026-09-17*
*อ้างอิงจาก: TODO.md, README.md, readme.md (ต้นฉบับทั้งหมดใน HangUp.Mac/)*
