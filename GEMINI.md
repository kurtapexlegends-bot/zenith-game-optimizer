# Zenith Game Optimizer — Engineering & Architectural Directives (GEMINI.md)

> **Repository:** `zenith-game-optimizer` (WPF / .NET Framework 4.8 / x64 / C#)  
> **Core Purpose:** Ultra-low latency, zero-drawback Windows gaming optimizer and non-invasive esports utility.  
> **Core Invariant:** 100% Anti-Cheat Compliance (External Win32/DWM APIs Only — Zero Memory Mutation, Zero DLL Injection).

---

## 1. Absolute Task Fidelity & Anti-Overengineering (CORE DIRECTIVES)

1. **Absolute Task Fidelity (Strict User Alignment):**
   - Execute user requirements with 100% fidelity. Never second-guess, water down, or alter explicit instructions.
   - **Zero Unsolicited "Guardrails":** Never add artificial safety modes, confirmation steps, countdown timers, or re-locking behaviors that undo or gate what the user requested. If the user asks for something to be draggable, make it draggable—period.
2. **Anti-Overengineering (Occam’s Razor Architecture):**
   - Implement the simplest, most direct solution that solves the problem. If a 5-line event handler solves the requirement, never construct an elaborate state machine, timer loop, or multi-window subsystem around it.
   - **Zero Premature Abstraction:** Do not invent generic wrapper classes, synchronization bridges, or mirror state layers for straightforward operations.
3. **Banned: Text Helpers & Explanatory Clutter:**
   - **Self-Explanatory Interfaces:** Never add helper paragraphs, explanatory subtitle captions under buttons, instructional banners, or "Why does X do Y" callout boxes.
   - **Zero Jargon:** UI controls must use concise, industry-standard nouns and verbs. Let the visual design, contrast, and layout communicate affordances naturally without conversational text clutter.
4. **Direct Manipulation First (Zero Mode Clutter):**
   - **Natural Interaction:** Floating widgets, overlays, and tools must be immediately interactive (draggable, clickable) by default without requiring an "unlock mode", "edit mode", or "reposition mode".
   - **Banned State Badges:** Never render intrusive status text badges (e.g. `[DRAG]`, `[DRAGGABLE]`, `[LOCKED]`, `[ACTIVE MODE]`) onto floating overlays.
   - **Clean Pass-Through:** Click-through behavior (`WS_EX_TRANSPARENT`) must be a clean, opt-in hotkey or right-click toggle for gaming sessions, never a barrier to everyday interaction. Overlays must handle multi-monitor boundary clamping seamlessly.
5. **Single Source of Truth (State Hierarchy):**
   - Architecture hierarchy: Cold Storage (`settings.json`) $\rightarrow$ Runtime Service Singletons (`.Instance`) $\rightarrow$ Stateless UI Views.
   - Domain Service Singletons are the authoritative single sources of truth during execution. UI views must query or bind directly to services and settings, never maintaining parallel state caches or synchronization loops.
6. **Real-Time Telemetry Integrity (Anti-Jitter Pacing):**
   - Diagnostic numbers (FPS, frametime, ping, memory) must NEVER output raw per-frame numbers that cause millisecond digit flutter. Always apply rolling or exponential moving average (EMA) smoothing so metrics reflect true performance without visual vibration.
   - **Genuine Measurement:** Never output synthetic, mock, or hardcoded "improvement" statistics. Background presentation loops must be wrapped in `timeBeginPeriod(1)` to eliminate the 15.625ms Windows timer quantum stall.
7. **Ruthless Legacy & Dead Code Purge:**
   - When replacing or removing a feature, completely delete its classes, fields, buttons, and settings keys. Never leave commented-out blocks, unused helper methods, obsolete `.old` binaries, or vestigial UI buttons.

---

## 2. Anti-Cheat Safety & Non-Invasive Invariants (STRICT)

1. **Zero DLL Injection & Zero Memory Tampering:**
   - NEVER call `VirtualAllocEx`, `WriteProcessMemory`, `CreateRemoteThread`, or open game processes with `PROCESS_VM_WRITE` / `PROCESS_VM_READ` / `PROCESS_ALL_ACCESS`.
   - Maintain 100% strict compliance with Roblox Hyperion (Byfron), Riot Vanguard, Easy Anti-Cheat (EAC), BattlEye, and Valve Anti-Cheat (VAC).
2. **Authorized Interop Mechanisms Only:**
   - External screen sampling: `GetDC`, `BitBlt`, `GetPixel`, `DwmGetCompositionTimingInfo`.
   - External input simulation: `mouse_event`, `SendInput`, `PostMessage` (`WM_LBUTTONDOWN` / `WM_LBUTTONUP`).
   - Official game configuration hooks: Engine configuration files (`.json`, `.cfg`, `.ini`, `.conf`), registry keys, and launch arguments.
   - OS-level pacing: Multimedia timer resolution (`NtSetTimerResolution`), process priority / thread affinity (`SetPriorityClass`, `SetProcessAffinityMask`), power plans, and ACPI fan cooling.
3. **Zero Sensor Self-Contamination:**
   - Any optical screen-sampling, color-detection, or audio-capture sensor must be physically and logically decoupled from the application's presentation layer. Overlays must maintain 100% hollow/transparent apertures at active sampling coordinates so sensors capture genuine game output rather than Zenith's own UI.

---

## 3. Structural Modularity & Code Boundaries

1. **Strict File Size Limits & Anti-Metric Gaming:**
   - **Monolithic File Ceiling:** No file may exceed **500 lines**.
   - **Coordinators & Orchestrators:** Must stay strictly under **250 lines** (e.g. `RobloxBidSniperService.cs`, `RobloxSniperOverlay.cs`, `MainWindow.Profiles.cs`, `GameSpecificTunerService.cs`).
   - **Domain Components & Tuners:** Must stay strictly under **400 lines** (e.g. `OpticalSamplingEngine.cs`, `ColorClassifier.cs`, `GameCardRenderer.cs`, individual game tuners).
   - **Anti-Metric Gaming:** Never pack multiple statements onto single lines, strip necessary comments/spacing, or create superficial 1-method child files solely to dodge line count ceilings. Maintain clean, human-readable structure.
2. **Strategy Pattern for Game Tuners:**
   - All game-specific tuners must implement `Services/Tuners/IGameTuner.cs` (`Apply`, `Restore`, `GetStatus`).
   - `GameSpecificTunerService.cs` acts strictly as a lightweight dispatcher router and must maintain 100% backward compatibility for all public methods.
3. **Project File Registration:**
   - Every new `.cs` file MUST be explicitly registered under `<ItemGroup><Compile Include="..."/></ItemGroup>` in `ZenithOptimizer.csproj`.
4. **No Unsolicited Dependencies or Build Churn:**
   - Never add third-party NuGet packages, external DLLs, or modify target framework versions (.NET Framework 4.8) unless explicitly requested. Rely on native Win32/WPF APIs.

---

## 4. Performance, Memory & Resource Discipline

1. **RAM Working Set Ceiling:**
   - Zenith's private working set must remain under **25 MB** (target: $\le 10\text{ MB}$; verified in Test 50).
   - Periodically trim private working set after memory-intensive operations (icon extraction, game library scans) using `SetProcessWorkingSetSize(procHandle, -1, -1)`.
2. **CPU & Background Thread Pacing:**
   - Idle CPU overhead must remain $<0.1\%$.
   - Dedicated high-frequency loops (e.g. `FpsMeterService` running `DwmFlush`, optical corridor sampling in `SniperWorkerEngine`) MUST run on background threads (`ThreadPriority.AboveNormal`), NEVER on the WPF `DispatcherTimer`.
   - All background threads must immediately pause, idle, or terminate (`Thread.Interrupt` / `CancellationToken`) when their respective overlays are hidden or disarmed.
3. **GDI & Win32 Handle Hygiene:**
   - Every Win32 handle (`HDC`, `HBITMAP`, `HICON`) MUST be released or destroyed in strict `try...finally` blocks (`ReleaseDC`, `DeleteDC`, `DeleteObject`, `DestroyIcon`).
   - Freeze all WPF brushes, images, and geometries (`Freeze()`) before assigning them to UI elements or caches.
4. **No Silent Error Swallowing:**
   - Bare or empty `catch { }` blocks are strictly BANNED. Every handled exception must log diagnostic information to `error.log` or debug telemetry (`System.Diagnostics.Debug.WriteLine`).

---

## 5. UI/UX & Design Token Standards ("Anti-AI Slop")

1. **Zero Decorative Emojis:**
   - Decorative or inline emojis are strictly BANNED across the entire application (UI buttons, card headers, labels, toasts, logs, and system tray).
   - Use clean WPF vector paths, system glyphs, or SVG icons.
2. **Monochrome Obsidian & Functional Accents:**
   - Base theme: Deep obsidian/slate dark mode (`#0B0E14` background, `#121722` cards, `#1E2536` borders, `#E2E8F0` text).
   - Functional accent tokens:
     - Emerald (`#10B981`): Active, Boosted, Confirmed Lock, Sweet-Spot Trigger.
     - Blue (`#3B82F6`): Armed, Locked, System Priority.
     - Amber (`#F59E0B`): Standby, Manual Calibration.
     - Crimson (`#EF4444`): Disarmed, Thermal Ceiling Warning, Error.
   - BANNED: Multi-color gradient borders, rainbow cards, or arbitrary inline CSS-like styles.

---

## 6. Configuration & Persistence Resilience

1. **Atomic Settings Storage:**
   - Settings file: `%APPDATA%\ZenithOptimizer\settings.json`.
   - Always write changes to `.tmp` first, then atomically replace the target file (`File.Replace` or `File.Move`) to prevent zero-byte corruptions on force-kill.
2. **Scoped Mutation Boundaries:**
   - Configuration file edits must be scoped strictly to the target game profile, section, or emulator instance.
   - Global engine settings, adjacent instance profiles, and user-bound custom keymaps must NEVER be overwritten, mutated, or reset.
3. **Parser Integrity:**
   - NEVER use naive string-splitting or integer-only regexes to parse configuration files (`.ini`, `.json`, `.cfg`, `.conf`).
   - JSON configurations require typed native values (booleans as `true`/`false`, numbers as numeric literals), never quoted string booleans.

---

## 7. Build, Test & Release Standards

1. **Toolchain & Compilation:**
   - Compiler: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`.
   - Command: `MSBuild.exe ZenithOptimizer.csproj /p:Configuration=Release /p:Platform="x64" /v:m`.
   - Must compile with **0 errors**.
2. **Deterministic Verification (Anti-"Verification Theater"):**
   - Verification suite: `ZenithTests.cs` (100% of all registered automated test categories must pass; zero failures allowed).
   - Never report a test, build, or feature as passing without inspecting actual process exit codes, log outputs, and live state values. Cosmetic assertions or mock passes are strictly prohibited.
3. **Binary Synchronization & Active Process Recovery:**
   - Both `bin\Release\ZenithOptimizer.exe` and root `ZenithOptimizer.exe` must match identically.
   - Zero temporary `.old`, `.bak`, `.tmp`, or `ZenithTests.exe` files allowed in the repository.
   - If MSBuild encounters file lock conflicts (`MSB3021/MSB3026`) due to an active elevated process, rename the locked executable (`*.running`) to immediately free the output target without corrupting running processes.
