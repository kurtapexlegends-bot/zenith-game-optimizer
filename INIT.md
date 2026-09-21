# Zenith Game Optimizer — Project Initialization & Handoff (`INIT.md`)

> **Location:** `C:\Users\acost\.gemini\antigravity\scratch\zenith-game-optimizer`  
> **Platform / Framework:** Windows 11 x64 / .NET Framework 4.8 (WPF)  
> **Target Toolset:** MSBuild 4.0 / 12.0 (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`)  
> **Current Status:** Verified Build (0 Errors), 19/19 Automated Tests Passing  

---

## 1. Project Purpose & Scope

**Zenith Game Optimizer** is an esports performance desktop utility engineered to maximize frame rates, minimize input latency, and shield system resources for competitive games on Windows.

### Supported Titles (8 Core Titles)
* **Mobile Legends: Bang Bang** (BlueStacks 5 Pie 64-bit container)
* **Roblox** (Native Client)
* **League of Legends**
* **Valorant**
* **Counter-Strike 2**
* **Minecraft** (Vanilla / Modded / Hypixel PvP)
* **Tekken 7**
* **Dota 2**
*(Fortnite, Genshin Impact, Apex Legends, and Tekken 8 removed per explicit directive)*

---

## 2. Hard Invariants & Critical Safety Rules

1. **NEVER OVERWRITE BLUESTACKS USER CONTROLS:**
   * File: `C:\ProgramData\BlueStacks_nxt\Engine\UserData\InputMapper\UserFiles\com.mobile.legends.cfg`
   * The user customized the `B` key into a MOBA skillpad rather than a standard tap.
   * `GameSpecificTunerService.ProtectUserKeymaps()` runs in `MainWindow_Loaded` to prevent overwrite.
   * A preserved read-only safety backup is stored at:  
     `C:\ProgramData\BlueStacks_nxt\Engine\UserData\InputMapper\UserFiles\com.mobile.legends.cfg.USER_PRESERVED_NEVER_TOUCH`
   * **Rule:** Never alter, overwrite, or reset this file.

2. **PERMANENTLY BAN 21:9 ULTRA-WIDE OVERRIDES:**
   * Laptop display: **1920x1200 (16:10)** on Intel Core Ultra 5 115U.
   * 2560x1080 (21:9) causes letterboxing, squished UI, lower FPS, and misaligned skillshot clicks.
   * **Rule:** BlueStacks must always be kept at standard **1920x1080 (16:9 FHD)** (`fb_width="1920"`, `fb_height="1080"`). Do NOT re-add ultra-wide preset triggers.

3. **DEVICE PROFILE MUST BE ASUS ROG PHONE:**
   * Model: `ASUS_AI2205_D` (ASUS ROG Phone 7 Ultimate), brand: `ASUS`, manufacturer: `asus`.
   * **Why:** Moonton officially whitelists ASUS ROG in MLBB to unlock **120 FPS "Super High"** frame rates without thermal throttling. Emulating Xiaomi Black Shark causes micro-stuttering in teamfights.

4. **NO "GUIDE" BUTTONS OR POPUP MODALS:**
   * All 45 guide buttons and educational popups have been removed. The user strictly wants actionable tools only.

5. **UI FRAME MUST MATCH ORIGINAL SCREENSHOT:**
   * Reference: `Screenshot 2026-09-16 071318.png`.
   * Standard WPF window frame, radio navigation (`Nav_Checked`), collapsible category accordions (`SetupToolsPanelHeaderAndAccordion`), and category search filter chips.
   * Do NOT add custom WindowChrome titlebars or redesign card structs.

---

## 3. Project Directory Map

```
zenith-game-optimizer/
├── App.xaml / App.xaml.cs            # WPF Application lifecycle & single-instance mutex
├── MainWindow.xaml                   # Primary application interface
├── MainWindow.xaml.cs                # UI logic, tool creation, accordion filtering, search
├── ZenithOptimizer.csproj            # .NET Framework 4.8 Project definition
├── ZenithOptimizer.exe               # Compiled 64-bit production binary
├── ZenithTests.cs                    # Complete 19-suite test framework
├── ZenithTests.exe                   # Compiled test runner binary
├── app.ico                           # Application icon
├── Models/
│   ├── GameProfile.cs                # Game metadata, process names, configs
│   ├── OptimizationSettings.cs       # Persistent user toggles & active tools
│   └── SystemTelemetry.cs            # Live CPU, GPU, RAM, & Ping data contracts
└── Services/
    ├── BlueStacksOptimizer.cs        # Parser for bluestacks.conf
    ├── GameLauncherService.cs        # Executable detection & launch orchestration
    ├── GameSpecificTunerService.cs   # Game tuning engines (MLBB, Roblox, CS2, etc.)
    ├── MemoryPurgeService.cs         # Standby RAM & working set trimming
    ├── NetworkOptimizer.cs           # TCPNoDelay, MTU 1472, QoS DSCP 46 tagging
    ├── PowerPlanService.cs           # Windows power scheme switching
    ├── PriorityAffinityEngine.cs     # Realtime process priority & P-core pinning
    ├── ProcessMonitorService.cs      # Background game detection & telemetry poller
    ├── SystemTweaksService.cs        # AudioDG stutter shield, Game DVR disabling
    └── TimerResolutionEngine.cs      # Microsecond multimedia timer resolution (0.500ms)
```

---

## 4. Build, Verify & Test Commands

### Build & Clean (PowerShell)
```powershell
# 1. Stop any running instances
Get-Process -Name "*Zenith*" -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Clean intermediate BAML & output caches
Remove-Item -Path 'obj' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path 'bin\Release' -Recurse -Force -ErrorAction SilentlyContinue

# 3. Compile Release x64
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe' ZenithOptimizer.csproj /t:Rebuild /p:Configuration=Release /p:Platform=x64

# 4. Copy outputs to root
Copy-Item 'bin\Release\ZenithOptimizer.exe' 'ZenithOptimizer.exe' -Force
Copy-Item 'bin\Release\ZenithOptimizer.pdb' 'ZenithOptimizer.pdb' -Force
```

### Run Automated Tests
```powershell
.\ZenithTests.exe
```
*Expected Result:* `RESULTS: 19 PASSED, 0 FAILED`

### Launch App
```powershell
.\ZenithOptimizer.exe
```

---

## 5. Active BlueStacks 5 State (`bluestacks.conf`)

* **File Location:** `C:\ProgramData\BlueStacks_nxt\bluestacks.conf`
* **Resolution:** `1920x1080` (16:9 Standard FHD, 0% distortion on 1920x1200 laptop display)
* **Device Profile:** `ASUS` / `ASUS_AI2205_D` (ROG Phone 7 Ultimate, smooth 120 FPS)
* **Dedicated RAM:** 8192 MB (8GB)
* **ABI Translation:** ARM64-v8a enabled
* **Controls:** Protected (`com.mobile.legends.cfg`)
