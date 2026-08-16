# HangUp for macOS 🍎

โปรเจกต์นี้คือ **HangUp** เวอร์ชันสำหรับ **macOS** ซึ่งพัฒนาแยกต่างหากจากเวอร์ชัน Windows เนื่องจากสถาปัตยกรรมระบบ (โดยเฉพาะเรื่อง Firewall และ Background Services) ของ Mac แตกต่างจาก Windows อย่างสิ้นเชิง

## 🏗 โครงสร้างโฟลเดอร์ (Project Structure)

โปรเจกต์ถูกแบ่งออกเป็น 2 ส่วนหลัก เพื่อให้ง่ายต่อการดูแล:

*   **`HangUp.Mac.sln`** - Solution หลักของระบบ
*   **`src/HangUp.Mac.Core/`** - **(หัวใจของระบบ)** จัดการระบบบล็อก, คอนฟิก, โมเดล
    *   `Models/` - เก็บโครงสร้างข้อมูล เช่น `AppProfile.cs` (แอปที่จะบล็อก)
    *   `Config/` - จัดการไฟล์ `apps.json` และการตั้งค่าต่างๆ
    *   `Firewall/` - **(สำคัญมาก)** โค้ดที่ใช้ยุ่งกับระบบ Mac เช่น `MacFirewallManager.cs` จะอยู่ที่นี่
*   **`src/HangUp.Mac.App/`** - **(หน้าตาแอป)** พัฒนาด้วย **Avalonia UI** เพื่อให้ได้หน้าตาแบบ Glassmorphism สวยงามเหมือน Mac แท้ๆ และสามารถเขียน/ทดสอบ UI บน Windows ได้
    *   `Views/` - เก็บไฟล์ XAML สำหรับหน้าจอ
    *   `ViewModels/` - เก็บ C# ลอจิกสำหรับผูก Data เข้ากับ UI (MVVM)
    *   `Assets/` - เก็บรูปภาพไอคอน `.png`

---

## 🤖 คำแนะนำสำหรับ AI ตัวอื่นๆ (AI Developer Guidelines)
**(For AI Assistants like Cline, Cursor, or ChatGPT)**

If you are an AI assistant continuing this project, **PLEASE READ THIS CAREFULLY**:

### 1. Architecture & Blocking Strategy
macOS does NOT allow path-based outgoing firewall blocking easily without a Kernel/Network extension. **DO NOT attempt to use `pfctl` to block `.app` paths.** 
Our strategy for macOS is:
1.  **Domain Blocking:** Append domain rules to `/etc/hosts`.
2.  **Service Blocking:** Unload background agents using `launchctl bootout` or `killall`.
3.  **Privilege Escalation:** Since modifying `/etc/hosts` requires `root`, the app MUST use `osascript -e 'do shell script "..." with administrator privileges'` to prompt the user natively for their password. This logic is already stubbed in `MacFirewallManager.cs`.

### 2. UI Framework (Avalonia)
This is an **Avalonia UI** project, NOT WPF and NOT MAUI. 
*   Use `<Window TransparencyLevelHint="AcrylicBlur">` for the glassmorphism effect.
*   The primary design language is "Dark Mode Neo-Brutalism/Modern".
*   Always bind data using `ReactiveUI` or standard `INotifyPropertyChanged` in the ViewModels.

### 3. How to Build (Cross-Compiling from Windows)
Since we are building a macOS app on Windows, standard `dotnet publish` will output Unix executables, but NOT a `.app` bundle. 
To build the `.app` bundle, use the following command:
```bash
# Publish for Apple Silicon (M1/M2/M3)
dotnet publish "src/HangUp.Mac.App/HangUp.Mac.App.csproj" -c Release -r osx-arm64 --self-contained true

# Publish for Intel Mac
dotnet publish "src/HangUp.Mac.App/HangUp.Mac.App.csproj" -c Release -r osx-x64 --self-contained true
```
*Note: To create the actual `HangUp.app` folder structure (Contents/MacOS, Contents/Resources, Info.plist), you must manually structure the folders or use a tool like `dotnet-bundle`.*

### 4. How to Test
*   **UI Testing:** You can run the Avalonia app directly on Windows to test the UI! Just run `dotnet run --project src/HangUp.Mac.App/HangUp.Mac.App.csproj`. The `MacFirewallManager` is designed to "mock" the `osascript` execution if it detects it is running on Windows.
*   **Core Logic Testing:** The core logic MUST be tested on a real Mac or macOS VM.

### 5. Current State & Next Steps
*   [x] Project Structure Initialized
*   [x] `MacFirewallManager` osascript logic implemented
*   [x] Basic `MainWindow.axaml` UI structured
*   [ ] Finish binding `MainWindowViewModel` to the UI
*   [ ] Implement a build script (`build-mac.ps1`) to automatically generate the `.app` folder structure and `Info.plist` on Windows.
*   [ ] Add logic to cleanly parse and block specific LaunchDaemons.
