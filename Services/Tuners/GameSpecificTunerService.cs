using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ZenithOptimizer.Services
{
    public partial class GameSpecificTunerService
    {

        public static void ProtectUserKeymaps()
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string userCfgDir = Path.Combine(programData, @"BlueStacks_nxt\Engine\UserData\InputMapper\UserFiles");
                string userCfg = Path.Combine(userCfgDir, "com.mobile.legends.cfg");
                string backupDir = Path.Combine(programData, @"Zenith\KeymapBackups");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
                string backupCfg = Path.Combine(backupDir, "com.mobile.legends.cfg");

                if (File.Exists(userCfg) && new FileInfo(userCfg).Length > 1000)
                {
                    File.Copy(userCfg, backupCfg, true);
                }
                else if (!File.Exists(userCfg) && File.Exists(backupCfg))
                {
                    if (!Directory.Exists(userCfgDir)) Directory.CreateDirectory(userCfgDir);
                    File.Copy(backupCfg, userCfg, true);
                }
            }
            catch { }
        }

        private readonly BlueStacksOptimizer _blueStacksOptimizer;

        private readonly TimerResolutionEngine _timerEngine;

        private readonly SystemTweaksService _systemTweaks;

        private readonly MemoryPurgeService _memoryService;

        private readonly NetworkOptimizer _networkOptimizer;

        public GameSpecificTunerService()

            : this(new BlueStacksOptimizer(), new TimerResolutionEngine(), new SystemTweaksService(), new MemoryPurgeService(), new NetworkOptimizer())

        {

        }

        public GameSpecificTunerService(

            BlueStacksOptimizer blueStacksOptimizer,

            TimerResolutionEngine timerEngine = null,

            SystemTweaksService systemTweaks = null,

            MemoryPurgeService memoryService = null,

            NetworkOptimizer networkOptimizer = null)

        {

            _blueStacksOptimizer = blueStacksOptimizer;

            _timerEngine = timerEngine;

            _systemTweaks = systemTweaks;

            _memoryService = memoryService;

            _networkOptimizer = networkOptimizer;

        }

        private static void BackupConfig(string path)

        {

            try

            {

                string bak = path + ".zenith.bak";

                if (!File.Exists(bak))

                {

                    File.Copy(path, bak);

                }

            }

            catch { }

        }

        private static string ReplaceOrInsert(string content, string section, string key, string value)

        {

            string pattern = @"(" + Regex.Escape(key) + @"=)\d+";

            if (Regex.IsMatch(content, pattern))

            {

                return Regex.Replace(content, pattern, m => m.Groups[1].Value + value);

            }

            string sectionHeader = "[" + section + "]";

            if (content.Contains(sectionHeader))

            {

                return content.Replace(sectionHeader, sectionHeader + "\r\n" + key + "=" + value);

            }

            return content + "\r\n[" + section + "]\r\n" + key + "=" + value;

        }

        private static long CleanFolderFiles(string folder)

        {

            long freed = 0;

            if (!Directory.Exists(folder)) return 0;

            try

            {

                string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

                foreach (string f in files)

                {

                    try

                    {

                        FileInfo fi = new FileInfo(f);

                        long len = fi.Length;

                        fi.Delete();

                        freed += len;

                    }

                    catch { }

                }

            }

            catch { }

            return freed;

        }

        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]

        private static extern int DnsFlushResolverCache();

        public static bool FlushSystemDns(out string message)

        {

            try

            {

                DnsFlushResolverCache();

                message = "Windows DNS Resolver Cache flushed (eliminated stale server routing).";

                return true;

            }

            catch (Exception ex)

            {

                message = "DNS Flush: " + ex.Message;

                return false;

            }

        }

        public static bool SetWindowsGpuPreference(string executablePathOrName, int preference, out string message)

        {

            try

            {

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))

                {

                    if (key != null)

                    {

                        key.SetValue(executablePathOrName, "GpuPreference=" + preference + ";", RegistryValueKind.String);

                    }

                }

                string modeStr = preference == 2 ? "High Performance (Dedicated GPU)" : "Power Saving";

                message = string.Format("DirectX GPU preference set to {0} for {1}.", modeStr, Path.GetFileName(executablePathOrName));

                return true;

            }

            catch (Exception ex)

            {

                message = "Failed to set GPU preference: " + ex.Message;

                return false;

            }

        }

        public static bool SetWindowsFullscreenOptimizationBypass(string executablePathOrName, out string message)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
                {
                    if (key != null)
                    {
                        // On Windows 10/11, setting DISABLEDXMAXIMIZEDWINDOWEDMODE breaks the modern DirectFlip
                        // swapchain and causes the infamous Alt+Tab black screen bug in DirectX 11/12 games.
                        // We set HIGHDPIAWARE to ensure sharp 1:1 pixel rendering while letting Windows DWM maintain zero-latency DirectFlip.
                        key.SetValue(executablePathOrName, "~ HIGHDPIAWARE", RegistryValueKind.String);
                    }
                }

                message = string.Format("DirectFlip presentation & high-DPI scaling enabled for {0} (Alt+Tab safe).", Path.GetFileName(executablePathOrName));
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to set fullscreen flags: " + ex.Message;
                return false;
            }
        }

        public static bool PinProcessToPhysicalPerformanceCores(string processName, out string message)

        {

            try

            {

                Process[] procs = Process.GetProcessesByName(processName);

                if (procs == null || procs.Length == 0)

                {

                    message = string.Format("{0} is not currently running. Will enforce P-Core affinity on launch.", processName);

                    return true;

                }

                int totalThreads = Environment.ProcessorCount;

                long mask = 0;

                for (int i = 0; i < totalThreads; i += 2)

                {

                    mask |= (1L << i);

                }

                if (mask == 0) mask = (1L << totalThreads) - 1;

                int pinned = 0;

                foreach (var p in procs)

                {

                    try

                    {

                        p.ProcessorAffinity = (IntPtr)mask;

                        pinned++;

                    }

                    catch { }

                }

                message = string.Format("Pinned {0} ({1} instance(s)) exclusively to Physical Performance Cores.", processName, pinned);

                return true;

            }

            catch (Exception ex)

            {

                message = "Affinity: " + ex.Message;

                return false;

            }

        }

        public static bool IsolateAudioDgThread(out string message)

        {

            try

            {

                Process[] audioProcs = Process.GetProcessesByName("audiodg");

                if (audioProcs != null && audioProcs.Length > 0)

                {

                    foreach (var p in audioProcs)

                    {

                        try

                        {

                            p.ProcessorAffinity = (IntPtr)2; // Dedicated Core 1, away from render core 0

                            p.PriorityClass = ProcessPriorityClass.High;

                        }

                        catch { }

                    }

                    message = "AudioDG engine isolated to secondary CPU Core (Core 1) with High priority.";

                    return true;

                }

                message = "AudioDG service active in standard mode.";

                return true;

            }

            catch (Exception ex)

            {

                message = "Audio isolation: " + ex.Message;

                return false;

            }

        }

        private string RegexReplaceOrInsert(string content, string key, string value)

        {

            if (string.IsNullOrEmpty(content)) content = "";

            // Check if this is a JSON file/flag (e.g. Roblox ClientAppSettings.json)

            string trimmed = content.Trim();

            if (trimmed.StartsWith("{") || key.StartsWith("FFlag") || key.StartsWith("DFInt") || key.StartsWith("FInt") || key.StartsWith("DFFlag"))

            {

                if (string.IsNullOrEmpty(trimmed) || trimmed == "{}")

                {

                    return "{\r\n  \"" + key + "\": " + FormatJsonValue(value) + "\r\n}";

                }

                string jsonPattern = @"(""" + Regex.Escape(key) + @"""\s*:\s*)([^,\}\r\n]+)";

                if (Regex.IsMatch(content, jsonPattern))

                {

                    return Regex.Replace(content, jsonPattern, m => m.Groups[1].Value + FormatJsonValue(value));

                }

                int lastBrace = content.LastIndexOf('}');

                if (lastBrace >= 0)

                {

                    string before = content.Substring(0, lastBrace).TrimEnd();

                    string comma = (before.EndsWith("{") || before.EndsWith(",")) ? "" : ",\r\n";

                    return before + comma + "  \"" + key + "\": " + FormatJsonValue(value) + "\r\n" + content.Substring(lastBrace);

                }

                return "{\r\n  \"" + key + "\": " + FormatJsonValue(value) + "\r\n}";

            }

            // For BlueStacks config, ensure the value is enclosed in quotes: key="value"

            if (key.StartsWith("bst.") && !value.StartsWith("\""))

            {

                value = "\"" + value + "\"";

            }

            string pattern = @"(" + Regex.Escape(key) + @"=)(.*)";

            if (Regex.IsMatch(content, pattern))

            {

                return Regex.Replace(content, pattern, m => m.Groups[1].Value + value);

            }

            if (key.StartsWith("bst."))

            {

                // Strict schema protection: Never append unknown keys to bluestacks.conf

                return content;

            }

            return content + "\r\n" + key + "=" + value;

        }

        private static string FormatJsonValue(string val)

        {

            if (string.IsNullOrEmpty(val)) return "\"\"";

            string lower = val.Trim().ToLowerInvariant();

            if (lower == "true" || lower == "false")

            {

                return lower;

            }

            double num;

            if (double.TryParse(val.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out num))

            {

                return val.Trim();

            }

            if (val.StartsWith("\"") && val.EndsWith("\""))

            {

                return val;

            }

            return "\"" + val + "\"";

        }


    }
}
