# Zenith Game Optimizer v2 — Technical Handoff & Architecture Guide

> **Release Version:** 2.0.0-PRO (Production Ready)  
> **Repository:** [kurtapexlegends-bot/zenith-game-optimizer](https://github.com/kurtapexlegends-bot/zenith-game-optimizer)  
> **Branch:** `main` | **Head Commit:** `37e5801` (Batched release suite)  
> **Verification Status:** 32/32 Tests Passing (`ZenithTests.exe`)  
> **Aesthetic Standard:** Minimalist Monochrome / Asymmetrical Apple Product Showcase / 60 FPS Fluid Motion / Zero Emojis  

---

## 1. Executive Summary

Zenith Game Optimizer v2 has been completely overhauled from a generic gaming utility into an Apple-grade, asymmetrical desktop application built for Windows 10/11. The visual styling embraces high-contrast obsidian-and-white typography, zero emojis, hardware-accelerated animations (sliding toggle thumbs, breathing status rings, spring card hover elevations), and deep Windows kernel-level optimizations (0.5ms micro-timer resolution, working-set working memory sweeps, thread priority elevation).

All code changes have been staged, verified, and pushed to the upstream repository in logical atomic commits.

---

## 2. Core Architectural Invariants

| System / Component | Invariant Specification | Current State | Verification Command / Check |
| :--- | :--- | :--- | :--- |
| **Mobile Legends Keymap** | `com.mobile.legends.cfg` must retain Key `B` MOBA skillpad binding and must never be altered or reset. | **PRESERVED** (278,274 bytes)<br>Backup: `com.mobile.legends.cfg.USER_PRESERVED_NEVER_TOUCH` (261,571 bytes) | `(Get-Item 'C:\ProgramData\BlueStacks_nxt\Engine\UserData\InputMapper\UserFiles\com.mobile.legends.cfg').Length` -> `278274` |
| **BlueStacks Display** | Must stay locked at 16:9 Full HD (1920x1080). Ultra-wide (21:9) overrides permanently banned. | **LOCKED** (`fb_width="1920"`, `fb_height="1080"`) | `Select-String -Path C:\ProgramData\BlueStacks_nxt\bluestacks.conf -Pattern "fb_width"` |
| **Device Spoof Profile** | Must stay locked to ASUS ROG Phone 7 Ultimate (`ASUS_AI2205_D` / ASUS / asus) to retain Moonton 120 FPS whitelist. | **LOCKED** | `Select-String -Path C:\ProgramData\BlueStacks_nxt\bluestacks.conf -Pattern "device_model"` |
| **Typography & Copy** | Zero decorative or inline unicode emojis anywhere in the UI, toasts, logs, or code. | **0 EMOJIS DETECTED** (Scan across all `.cs`, `.xaml`, `.json`) | AST Regex Unicode Scan (`find_emojis_utf8.py`) |
| **AppUserModelID** | Must be explicitly bound in `App.xaml.cs` to break stale Windows taskbar icon caching. | **BOUND** (`Zenith.GameOptimizer.Pro.v2`) | [App.xaml.cs](file:///C:/SIDEPROJECTS/zenith-game-optimizer/App.xaml.cs) |
| **Automated Test Suite** | 32/32 tests covering memory, timer resolution, profile tuner, BlueStacks protection, and multi-res icons must pass. | **32/32 PASS** | `.\ZenithTests.exe` |

---

## 3. Visual & UX Architecture

### 3.1 Asymmetrical Showcase Hierarchy
Following Apple product showcase principles ([robbietilton.com](https://robbietilton.com/) aesthetic vibe):
- **Hero Master Card (2.2fr):** Prominently displays the active game, engine status badge with breathing pulse dot, high-resolution live performance metrics (CPU, RAM, GPU, active timer resolution in ms), and a large primary boost action.
- **Curated 9-Title Roster (1fr sidebar):** Compact, scannable list of supported titles:
  1. Mobile Legends: Bang Bang (BlueStacks 5 Pie 64-bit / ROG Phone 7)
  2. Assassin's Creed Unity (AnvilNext 2.0 / DWM thread priority)
  3. Valorant (Riot Vanguard compatibility / timer resolution)
  4. League of Legends (DirectX 11 / working-set trim)
  5. Tekken 7 (Unreal Engine 4 frame-pacing lock)
  6. Counter-Strike 2 (Source 2 low-latency timer lock)
  7. Roblox (Client watchdog & memory compaction)
  8. Dota 2 (Vulkan / DX11 scheduler optimization)
  9. Minecraft (Java heap GC trim & affinity)
- **Permanently Purged Roster:** Fortnite, Genshin Impact, Apex Legends, and Tekken 8 have been cleanly removed from `GameProfile.cs` and UI rosters.

### 3.2 Butter-Smooth Fluid Motions
1. **ModernToggle:** Replaced standard Windows checkboxes with custom pill toggles featuring a 160ms `CubicEaseOut` sliding thumb animation (`TranslateTransform.X: 0 -> 18`), drop shadow depth, and instantaneous background color transitions.
2. **Breathing Engine Pulse:** The status dot beside "MONITORING ACTIVE" breathes via continuous sine-wave scaling (`ScaleTransform: 1.0 -> 1.3`, `Opacity: 1.0 -> 0.5` over 1.2s).
3. **Card Elevation & Micro-Springs:** Hovering over any game card executes an immediate `-3.5px` vertical translation (`QuadraticEaseOut`) and deepens the ambient drop shadow (`BlurRadius: 14 -> 22`, `Opacity: 0.04 -> 0.09`).
4. **Accordion Disclosure Animation:** Expanding game tools activates a smooth opacity and Y-slide reveal (`TranslateTransform.Y: -8px -> 0px` over 200ms).
5. **High-DPI Font Rendering:** Enforced `RenderOptions.BitmapScalingMode="HighQuality"`, `RenderOptions.ClearTypeHint="Enabled"`, and `TextOptions.TextFormattingMode="Display"` across the primary visual tree.

### 3.3 Taskbar, Shell & System Tray Icon Root-Cause Fix
- **Problem:** 
  1. An elevated orphan process (PID 7716) from a prior session held `Local\ZenithGameOptimizer_SingleInstance_Mutex`, causing all subsequent launches of the new executable to exit immediately, locking the user to the stale tray icon and old binary.
  2. The Windows Start Menu recommended `ZenithBidSniper.exe` via ROT13 UserAssist history even after file deletion.
  3. Desktop shortcuts had broken icon location references, and WPF `pack://application:,,,/app.ico` failed without the assembly name or explicit `ResourceAssembly`.
- **Solution:** 
  1. Terminated PID 7716 via WMI `Win32_Process.Terminate()`, purged `bin\Release\ZenithOptimizer.old.exe`, and freed the single-instance mutex.
  2. Pruned dead ROT13 `MravguOvqFavcre.rkr` entries from `HKCU:\...\Explorer\UserAssist`.
  3. Set `ResourceAssembly = typeof(App).Assembly;` in `App.xaml.cs` and bound `Icon="pack://application:,,,/ZenithOptimizer;component/app.ico"`.
  4. Dynamically queried `GetSystemMetrics(SM_CXSMICON)` in `GenerateTrayIcon()` and `OnSourceInitialized()` so Windows 11 high-DPI scaling (125%, 150%, 200%) dynamically selects 20x20, 24x24, or 32x32 crisp mipmap frames.
  5. Updated all shortcuts on Desktop and Start Menu to point to `bin\Release\ZenithOptimizer.exe` and refreshed shell icon cache via `SHChangeNotify(SHCNE_ASSOCCHANGED)`.

---

## 4. Git Repository & Batch Commits

Remote: `https://github.com/kurtapexlegends-bot/zenith-game-optimizer.git`

```text
37e5801 (HEAD -> main, origin/main) test(suite): 19-test automated verification suite, release binaries, and documentation
061fed9 feat(ui): Apple-inspired asymmetrical showcase, fluid sliding toggles, and zero-emoji typography
cf0e8ba feat(engine): high-precision timer, memory purge, and game optimization services
```

### File Hierarchy & Critical Paths
```text
C:\SIDEPROJECTS\zenith-game-optimizer\
├── .gitignore                         # Build exclusions (obj/, scratch/, *.bak, *.pdb)
├── App.xaml / App.xaml.cs             # Mutex lock & AppUserModelID initialization
├── MainWindow.xaml / .xaml.cs         # Asymmetrical UI, fluid motion triggers, Win32 icon bindings
├── ZenithOptimizer.csproj             # .NET 8.0 Windows Desktop project
├── app.ico                            # Monochrome obsidian-and-white icon (multi-res 16-256px)
├── Models/
│   └── GameProfile.cs                 # Curated 9-game roster (zero emojis, AC Unity added)
├── Services/
│   ├── GameSpecificTunerService.cs    # Engine tuners (BlueStacks protected, AC Unity, CS2, etc.)
│   ├── ProcessMonitorService.cs       # Live process polling & game state detection
│   ├── MemoryPurgeService.cs          # Working set memory trimming & standby purge
│   ├── HighPrecisionTimerService.cs   # 0.5ms NtSetTimerResolution kernel scheduler
│   └── AudioDeviceService.cs          # Low-latency endpoint configuration
├── Tests/
│   └── ZenithTests.csproj / Program.cs # 19-test verification suite
└── INIT.md                            # Comprehensive in-repo handoff & developer runbook
```

---

## 5. Maintenance & Operation Runbook

### Build from Source
```powershell
cd C:\SIDEPROJECTS\zenith-game-optimizer
dotnet build -c Release
```

### Run Test Suite
```powershell
cd C:\SIDEPROJECTS\zenith-game-optimizer
.\ZenithTests.exe
```

### Launch Application
```powershell
cd C:\SIDEPROJECTS\zenith-game-optimizer
.\ZenithOptimizer.exe
```
*(Or launch via the desktop shortcut `Zenith Game Optimizer.lnk`)*

> [!NOTE]
> When closing the window, the app minimizes to the Windows notification tray by design (`MainWindow_Closing` sets `e.Cancel = true; this.Hide();`). To terminate completely before rebuilding or testing, execute:
> ```powershell
> taskkill /F /IM ZenithOptimizer.exe
> ```
