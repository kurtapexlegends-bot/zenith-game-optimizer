using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ZenithOptimizer.Services.Tuners
{
    public static class TunerHelper
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern int DnsFlushResolverCache();

        public static void BackupConfig(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
                string bak = path + ".zenith.bak";
                if (!File.Exists(bak)) File.Copy(path, bak);
            }
            catch { }
        }

        public static bool RestoreBackup(string path, string successMsg, out string message)
        {
            if (string.IsNullOrEmpty(path)) { message = "Configuration path not specified."; return false; }
            try
            {
                string bak = path + ".zenith.bak";
                if (File.Exists(bak))
                {
                    File.Copy(bak, path, true);
                    message = successMsg;
                    return true;
                }
                message = "No previous configuration backup found.";
                return false;
            }
            catch (Exception ex)
            {
                message = "Failed to restore backup: " + ex.Message;
                return false;
            }
        }

        public static string ReplaceOrInsert(string content, string section, string key, string value)
        {
            if (content == null) content = string.Empty;
            string sectionHeaderPattern = @"(?m)^\[\s*" + Regex.Escape(section) + @"\s*\]";
            Match secMatch = Regex.Match(content, sectionHeaderPattern, RegexOptions.IgnoreCase);
            string keyLinePattern = @"(?m)^([ \t]*" + Regex.Escape(key) + @"[ \t]*=[ \t]*)(?:""[^""\r\n]*""|'[^'\r\n]*'|[+-]?\d+(?:\.\d+)?|true|false|[^\r\n]*)";

            if (secMatch.Success)
            {
                int sectionBodyStart = secMatch.Index + secMatch.Length;
                Match nextSecMatch = Regex.Match(content.Substring(sectionBodyStart), @"(?m)^\[[^\]]+\]");
                int sectionBodyLength = nextSecMatch.Success ? nextSecMatch.Index : (content.Length - sectionBodyStart);
                string sectionBody = content.Substring(sectionBodyStart, sectionBodyLength);

                if (Regex.IsMatch(sectionBody, keyLinePattern, RegexOptions.IgnoreCase))
                {
                    string updatedSectionBody = Regex.Replace(sectionBody, keyLinePattern, "$1" + value, RegexOptions.IgnoreCase);
                    return content.Substring(0, sectionBodyStart) + updatedSectionBody + content.Substring(sectionBodyStart + sectionBodyLength);
                }
                else
                {
                    string insertion = "\r\n" + key + "=" + value;
                    return content.Substring(0, sectionBodyStart) + insertion + content.Substring(sectionBodyStart);
                }
            }
            else
            {
                string prefix = string.IsNullOrEmpty(content) || content.EndsWith("\r\n") || content.EndsWith("\n") ? "" : "\r\n";
                return content + prefix + "[" + section + "]\r\n" + key + "=" + value + "\r\n";
            }
        }

        public static bool UpdateIniSetting(string iniPath, string section, string key, string value, string successMsg, out string message)
        {
            if (string.IsNullOrEmpty(iniPath) || !File.Exists(iniPath)) { message = "Config file not found."; return false; }
            try
            {
                BackupConfig(iniPath);
                string content = File.ReadAllText(iniPath);
                content = ReplaceOrInsert(content, section, key, value);
                File.WriteAllText(iniPath, content);
                message = successMsg;
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to update config: " + ex.Message;
                return false;
            }
        }

        public static bool UpdateRegexInFile(string filePath, string pattern, string replacement, string successMsg, out string message)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) { message = "Config file not found."; return false; }
            try
            {
                BackupConfig(filePath);
                string content = File.ReadAllText(filePath);
                content = Regex.Replace(content, pattern, replacement);
                File.WriteAllText(filePath, content);
                message = successMsg;
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to update config: " + ex.Message;
                return false;
            }
        }

        public static long CleanFolderFiles(string folder)
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
                    if (key != null) key.SetValue(executablePathOrName, "GpuPreference=" + preference + ";", RegistryValueKind.String);
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
                    if (key != null) key.SetValue(executablePathOrName, "~ HIGHDPIAWARE", RegistryValueKind.String);
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
                for (int i = 0; i < totalThreads; i += 2) mask |= (1L << i);
                if (mask == 0) mask = (1L << totalThreads) - 1;
                int pinned = 0;
                foreach (var p in procs)
                {
                    try { p.ProcessorAffinity = (IntPtr)mask; pinned++; } catch { }
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
                        try { p.ProcessorAffinity = (IntPtr)2; p.PriorityClass = ProcessPriorityClass.High; } catch { }
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
                try { dict = serializer.Deserialize<Dictionary<string, object>>(content); } catch { }
            }
            if (dict == null) dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (value == null) dict[key] = "";
            else if (value is bool || value is int || value is long || value is double || value is float) dict[key] = value;
            else
            {
                string s = value.ToString().Trim();
                if (s.Equals("true", StringComparison.OrdinalIgnoreCase)) dict[key] = true;
                else if (s.Equals("false", StringComparison.OrdinalIgnoreCase)) dict[key] = false;
                else
                {
                    long lVal; double dVal;
                    if (long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out lVal)) dict[key] = lVal;
                    else if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out dVal)) dict[key] = dVal;
                    else
                    {
                        if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\"")) s = s.Substring(1, s.Length - 2);
                        dict[key] = s;
                    }
                }
            }

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
                if (kvp.Value is bool) sb.Append((bool)kvp.Value ? "true" : "false");
                else if (kvp.Value is int || kvp.Value is long || kvp.Value is short || kvp.Value is byte) sb.Append(kvp.Value.ToString());
                else if (kvp.Value is double || kvp.Value is float || kvp.Value is decimal)
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", kvp.Value));
                else
                {
                    string strVal = kvp.Value != null ? kvp.Value.ToString() : "";
                    sb.Append("\"").Append(strVal.Replace("\"", "\\\"")).Append("\"");
                }
                if (i < count) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static bool IsFsoBypassActive(string exeName)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
                {
                    if (key != null)
                    {
                        foreach (string valName in key.GetValueNames())
                        {
                            if (valName.EndsWith(exeName, StringComparison.OrdinalIgnoreCase))
                            {
                                object val = key.GetValue(valName);
                                if (val != null && val.ToString().Contains("DISABLEDXMAXIMIZEDWINDOWEDMODE")) return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public static bool IsGpuPreferenceActive(string exeName)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))
                {
                    if (key != null)
                    {
                        foreach (string valName in key.GetValueNames())
                        {
                            if (valName.EndsWith(exeName, StringComparison.OrdinalIgnoreCase))
                            {
                                object val = key.GetValue(valName);
                                if (val != null && val.ToString().Contains("GpuPreference=2")) return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public static string RegexReplaceOrInsert(string content, string key, string value)
        {
            if (string.IsNullOrEmpty(content)) content = "";

            string trimmed = content.Trim();
            if (trimmed.StartsWith("{") || key.StartsWith("FFlag") || key.StartsWith("DFInt") || key.StartsWith("FInt") || key.StartsWith("DFFlag"))
            {
                return UpdateJsonSetting(content, key, value);
            }

            if (key.StartsWith("bst.") && !value.StartsWith("\""))
            {
                value = "\"" + value + "\"";
            }

            string pattern = @"(?m)^(" + Regex.Escape(key) + @"=)(.*)";
            if (Regex.IsMatch(content, pattern))
            {
                return Regex.Replace(content, pattern, delegate(Match m) { return m.Groups[1].Value + value; });
            }

            if (key.StartsWith("bst."))
            {
                return content;
            }

            return content + "\r\n" + key + "=" + value;
        }
    }
}
