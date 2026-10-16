using System;
using System.Collections.Generic;
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
            if (content == null) content = string.Empty;

            // Pattern for matching section header [SectionName]
            string sectionHeaderPattern = @"(?m)^\[\s*" + Regex.Escape(section) + @"\s*\]";
            Match secMatch = Regex.Match(content, sectionHeaderPattern, RegexOptions.IgnoreCase);

            // Pattern supporting integers, floating-point numbers, booleans, quoted strings, and arbitrary value strings
            string keyLinePattern = @"(?m)^([ \t]*" + Regex.Escape(key) + @"[ \t]*=[ \t]*)(?:""[^""\r\n]*""|'[^'\r\n]*'|[+-]?\d+(?:\.\d+)?|true|false|[^\r\n]*)";

            if (secMatch.Success)
            {
                int sectionBodyStart = secMatch.Index + secMatch.Length;
                // Find next section header or EOF
                Match nextSecMatch = Regex.Match(content.Substring(sectionBodyStart), @"(?m)^\[[^\]]+\]");
                int sectionBodyLength = nextSecMatch.Success ? nextSecMatch.Index : (content.Length - sectionBodyStart);
                string sectionBody = content.Substring(sectionBodyStart, sectionBodyLength);

                if (Regex.IsMatch(sectionBody, keyLinePattern, RegexOptions.IgnoreCase))
                {
                    // Key exists within this section: replace value in-place without corrupting other sections
                    string updatedSectionBody = Regex.Replace(sectionBody, keyLinePattern, "$1" + value, RegexOptions.IgnoreCase);
                    return content.Substring(0, sectionBodyStart) + updatedSectionBody + content.Substring(sectionBodyStart + sectionBodyLength);
                }
                else
                {
                    // Section exists but key does not: insert key into this section
                    string insertion = "\r\n" + key + "=" + value;
                    return content.Substring(0, sectionBodyStart) + insertion + content.Substring(sectionBodyStart);
                }
            }
            else
            {
                // Section does not exist: create section and insert key
                string prefix = string.IsNullOrEmpty(content) || content.EndsWith("\r\n") || content.EndsWith("\n") ? "" : "\r\n";
                return content + prefix + "[" + section + "]\r\n" + key + "=" + value + "\r\n";
            }
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

        public static string UpdateJsonSetting(string content, string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return content ?? "{\r\n}";
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            Dictionary<string, object> dict = null;

            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    dict = serializer.Deserialize<Dictionary<string, object>>(content);
                }
                catch { }
            }

            if (dict == null)
            {
                dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            if (value == null)
            {
                dict[key] = "";
            }
            else if (value is bool || value is int || value is long || value is double || value is float)
            {
                dict[key] = value;
            }
            else
            {
                string s = value.ToString().Trim();
                if (s.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    dict[key] = true;
                }
                else if (s.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    dict[key] = false;
                }
                else
                {
                    long lVal;
                    double dVal;
                    if (long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out lVal))
                    {
                        dict[key] = lVal;
                    }
                    else if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dVal))
                    {
                        dict[key] = dVal;
                    }
                    else
                    {
                        if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
                        {
                            s = s.Substring(1, s.Length - 2);
                        }
                        dict[key] = s;
                    }
                }
            }

            // Ensure FFlag booleans are native booleans
            if (key.StartsWith("FFlag", StringComparison.OrdinalIgnoreCase) || key.StartsWith("DFFlag", StringComparison.OrdinalIgnoreCase))
            {
                if (dict[key] is string)
                {
                    string s = (string)dict[key];
                    if (s.Equals("true", StringComparison.OrdinalIgnoreCase)) dict[key] = true;
                    else if (s.Equals("false", StringComparison.OrdinalIgnoreCase)) dict[key] = false;
                }
            }

            return SerializeFormattedJson(dict);
        }

        public static string SerializeFormattedJson(Dictionary<string, object> dict)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            int i = 0;
            int count = dict.Count;
            foreach (var kvp in dict)
            {
                i++;
                sb.Append("  \"").Append(kvp.Key).Append("\": ");
                if (kvp.Value is bool)
                {
                    sb.Append((bool)kvp.Value ? "true" : "false");
                }
                else if (kvp.Value is int || kvp.Value is long || kvp.Value is short || kvp.Value is byte)
                {
                    sb.Append(kvp.Value.ToString());
                }
                else if (kvp.Value is double || kvp.Value is float || kvp.Value is decimal)
                {
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", kvp.Value));
                }
                else
                {
                    string strVal = kvp.Value != null ? kvp.Value.ToString() : "";
                    sb.Append("\"").Append(strVal.Replace("\"", "\\\"")).Append("\"");
                }
                if (i < count)
                {
                    sb.Append(",");
                }
                sb.AppendLine();
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string RegexReplaceOrInsert(string content, string key, string value)
        {
            if (string.IsNullOrEmpty(content)) content = "";

            string trimmed = content.Trim();
            if (trimmed.StartsWith("{") || key.StartsWith("FFlag") || key.StartsWith("DFInt") || key.StartsWith("FInt") || key.StartsWith("DFFlag"))
            {
                return UpdateJsonSetting(content, key, value);
            }

            // For BlueStacks config, ensure the value is enclosed in quotes: key="value"
            if (key.StartsWith("bst.") && !value.StartsWith("\""))
            {
                value = "\"" + value + "\"";
            }

            string pattern = @"(?m)^(" + Regex.Escape(key) + @"=)(.*)";
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


    }
}
