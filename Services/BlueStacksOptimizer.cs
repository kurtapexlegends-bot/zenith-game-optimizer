using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ZenithOptimizer.Services
{
    public class BlueStacksOptimizer
    {
        private static readonly string[] PossibleConfigPaths = new string[]
        {
            @"C:\ProgramData\BlueStacks_nxt\bluestacks.conf",
            @"C:\ProgramData\BlueStacks\bluestacks.conf"
        };

        public bool IsInstalled()
        {
            string configPath = GetConfigFilePath();
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                return true;
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\BlueStacks_nxt"))
                {
                    if (key != null) return true;
                }
            }
            catch { }

            return false;
        }

        public string GetConfigFilePath()
        {
            foreach (string path in PossibleConfigPaths)
            {
                if (File.Exists(path)) return path;
            }
            return null;
        }

        public string AnalyzeConfiguration()
        {
            string path = GetConfigFilePath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return "BlueStacks not detected. Install BlueStacks 5 to play Mobile Legends with optimized virtualization.";
            }

            try
            {
                string content = File.ReadAllText(path);
                int cores = ExtractInt(content, @"\.cpu=""?(\d+)""?");
                int ram = ExtractInt(content, @"\.ram=""?(\d+)""?");
                bool highFps = content.Contains("enable_high_fps=\"1\"");

                string status = string.Format("Allocated Cores: {0} | RAM: {1} MB | High-FPS Mode: {2}",
                    cores > 0 ? cores.ToString() : "Default",
                    ram > 0 ? ram.ToString() : "Default",
                    highFps ? "Enabled" : "Disabled");

                return status;
            }
            catch (Exception ex)
            {
                return "Error reading config: " + ex.Message;
            }
        }

        public bool OptimizeForMobileLegends(out string message)
        {
            string path = GetConfigFilePath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                message = "BlueStacks configuration file (bluestacks.conf) not found.";
                return false;
            }

            try
            {
                // Create backup first
                string backupPath = path + ".zenith.bak";
                if (!File.Exists(backupPath))
                {
                    File.Copy(path, backupPath);
                }

                string content = File.ReadAllText(path);

                // Preserve high RAM if already 4GB or more
                int currentRam = ExtractInt(content, @"\.ram=""?(\d+)""?");
                int targetRam = currentRam >= 4096 ? currentRam : 4096;

                int targetCores = Environment.ProcessorCount >= 6 ? 4 : 2;
                content = Regex.Replace(content, @"(\.cpu=)""?\d+""?", "$1\"" + targetCores + "\"");
                content = Regex.Replace(content, @"(\.ram=)""?\d+""?", "$1\"" + targetRam + "\"");

                // Enable High FPS mode
                content = Regex.Replace(content, @"(\.enable_high_fps=)""?\d+""?", "$1\"1\"");
                content = Regex.Replace(content, @"(\.fps=)""?\d+""?", "$1\"120\"");

                File.WriteAllText(path, content);
                message = string.Format("BlueStacks configuration tuned ({0} Cores, {1}MB RAM, 120 FPS enabled).", targetCores, targetRam);
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to update BlueStacks configuration: " + ex.Message;
                return false;
            }
        }

        public bool RestoreConfig(out string message)
        {
            string path = GetConfigFilePath();
            if (string.IsNullOrEmpty(path))
            {
                message = "BlueStacks configuration file not found.";
                return false;
            }

            try
            {
                string backupPath = path + ".zenith.bak";
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, path, true);
                    message = "Restored original BlueStacks configuration from backup.";
                    return true;
                }
                else
                {
                    message = "No previous backup found to restore.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = "Restore failed: " + ex.Message;
                return false;
            }
        }

        private int ExtractInt(string content, string pattern)
        {
            try
            {
                Match match = Regex.Match(content, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    int val;
                    if (int.TryParse(match.Groups[1].Value, out val))
                    {
                        return val;
                    }
                }
            }
            catch { }
            return -1;
        }
    }
}
