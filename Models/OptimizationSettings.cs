using System;
using System.Collections.Generic;
using System.IO;

namespace ZenithOptimizer.Models
{
    public class OptimizationSettings
    {
        public bool AutoOptimizeOnGameLaunch { get; set; }
        public bool EnableHighPrecisionTimer { get; set; }
        public bool EnablePCoreAffinity { get; set; }
        public bool EnableHighPriority { get; set; }
        public bool EnableBackgroundThrottling { get; set; }
        public bool EnableAutoStandbyPurge { get; set; }
        public bool EnablePowerPlanSwitching { get; set; }
        public bool EnableNetworkOptimization { get; set; }
        public bool DisableGameDVR { get; set; }
        public bool EnableAudioStabilization { get; set; }
        public bool EnableGpuPriority { get; set; }
        public bool EnableWindowsGameMode { get; set; }
        public bool MinimizeToSystemTray { get; set; }
        public bool EnableLaptopThermalGuard { get; set; }
        public string Theme { get; set; }
        public Dictionary<string, bool> ToolActiveStates { get; set; }

        // View & Organization Preferences
        public string GameViewMode { get; set; }
        public string GameSortMode { get; set; }
        public List<string> FavoriteGameIds { get; set; }
        public List<string> CustomGameOrder { get; set; }

        // Roblox Auction Sweet-Spot Sniper Settings
        public int SniperSensorX { get; set; }
        public int SniperSensorY { get; set; }
        public bool SniperHasClickTarget { get; set; }
        public int SniperClickX { get; set; }
        public int SniperClickY { get; set; }
        public bool SniperUseCalibratedColor { get; set; }
        public int SniperTargetColorR { get; set; }
        public int SniperTargetColorG { get; set; }
        public int SniperTargetColorB { get; set; }
        public int SniperColorTolerance { get; set; }
        public string SniperTriggerMode { get; set; }
        public int SniperSamplingRadius { get; set; }
        public string SniperColorPreset { get; set; }
        public bool SniperDynamicTrackingEnabled { get; set; }
        public int SniperTrackCorridorWidth { get; set; }
        public int SniperTrackCorridorHeight { get; set; }
        public int SniperTrackScanStep { get; set; }
        public string SniperCaptureMethod { get; set; }
        public string SniperClickMode { get; set; }
        public int SniperAuctionCountdownSeconds { get; set; }
        public string SniperClickTargetMode { get; set; }
        public bool SniperRestoreCursor { get; set; }

        // On-Screen FPS & Frametime Meter Settings
        public bool IsFpsMeterEnabled { get; set; }
        public int FpsMeterX { get; set; }
        public int FpsMeterY { get; set; }

        // Sniper Compact / Minimized Mode Settings
        public bool SniperIsCompactMode { get; set; }
        public bool SniperMarkersHidden { get; set; }

        public OptimizationSettings()
        {
            AutoOptimizeOnGameLaunch = true;
            EnableHighPrecisionTimer = true;
            EnableHighPriority = true;
            EnablePCoreAffinity = false; // Safe default for mobile/hybrid CPUs & emulators
            EnableBackgroundThrottling = false; // Safe default
            EnableAutoStandbyPurge = false; // Safe default: NEVER purge RAM while game/VM is loading
            EnablePowerPlanSwitching = true;
            EnableNetworkOptimization = false;
            DisableGameDVR = true; // Turn off background screen recorder to save GPU encoder
            EnableAudioStabilization = true; // Safe priority elevation for audiodg.exe to prevent sound hitching
            EnableGpuPriority = false; // DirectX MMCSS scheduling (user opt-in)
            EnableWindowsGameMode = true;
            MinimizeToSystemTray = true;
            EnableLaptopThermalGuard = false;
            Theme = "Light";
            ToolActiveStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            GameViewMode = "Cards";
            GameSortMode = "Installed";
            FavoriteGameIds = new List<string>();
            CustomGameOrder = new List<string>();

            // Sniper Defaults
            SniperSensorX = 0;
            SniperSensorY = 0;
            SniperHasClickTarget = false;
            SniperClickX = 0;
            SniperClickY = 0;
            SniperUseCalibratedColor = false;
            SniperTargetColorR = 46;
            SniperTargetColorG = 204;
            SniperTargetColorB = 113;
            SniperColorTolerance = 45;
            SniperTriggerMode = "Edge"; // Edge trigger (entry into green)
            SniperSamplingRadius = 4;
            SniperColorPreset = "Green";
            SniperDynamicTrackingEnabled = true;
            SniperTrackCorridorWidth = 260;
            SniperTrackCorridorHeight = 32;
            SniperTrackScanStep = 4;
            SniperCaptureMethod = "Auto";
            SniperClickMode = "Dual";
            SniperAuctionCountdownSeconds = 5;
            SniperClickTargetMode = "FixedTarget";
            SniperRestoreCursor = false;

            // FPS Meter Defaults
            IsFpsMeterEnabled = false;
            FpsMeterX = -1;
            FpsMeterY = -1;

            // Compact Mode Defaults
            SniperIsCompactMode = false;
            SniperMarkersHidden = true;
        }

        public static string GetSettingsFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "ZenithOptimizer");
            return Path.Combine(dir, "settings.json");
        }

        public static OptimizationSettings Load()
        {
            try
            {
                string file = GetSettingsFilePath();
                string tempFile = file + ".tmp";

                string targetToRead = null;
                if (File.Exists(file))
                {
                    targetToRead = file;
                }
                else if (File.Exists(tempFile))
                {
                    targetToRead = tempFile;
                }

                if (targetToRead != null)
                {
                    string json = File.ReadAllText(targetToRead);
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var loaded = serializer.Deserialize<OptimizationSettings>(json);
                    if (loaded != null)
                    {
                        if (loaded.ToolActiveStates == null)
                        {
                            loaded.ToolActiveStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                        }
                        if (loaded.FavoriteGameIds == null)
                        {
                            loaded.FavoriteGameIds = new List<string>();
                        }
                        if (loaded.CustomGameOrder == null)
                        {
                            loaded.CustomGameOrder = new List<string>();
                        }
                        if (string.IsNullOrEmpty(loaded.GameViewMode))
                        {
                            loaded.GameViewMode = "Cards";
                        }
                        if (string.IsNullOrEmpty(loaded.GameSortMode))
                        {
                            loaded.GameSortMode = "Installed";
                        }
                        if (string.IsNullOrEmpty(loaded.SniperColorPreset))
                        {
                            loaded.SniperColorPreset = "Green";
                        }
                        if (loaded.SniperTrackCorridorWidth <= 0)
                        {
                            loaded.SniperTrackCorridorWidth = 260;
                        }
                        if (loaded.SniperTrackCorridorHeight <= 0)
                        {
                            loaded.SniperTrackCorridorHeight = 32;
                        }
                        if (loaded.SniperTrackScanStep <= 0)
                        {
                            loaded.SniperTrackScanStep = 4;
                        }
                        if (string.IsNullOrEmpty(loaded.SniperCaptureMethod))
                        {
                            loaded.SniperCaptureMethod = "Auto";
                        }
                        if (string.IsNullOrEmpty(loaded.SniperClickMode))
                        {
                            loaded.SniperClickMode = "Dual";
                        }
                        if (loaded.SniperAuctionCountdownSeconds <= 0)
                        {
                            loaded.SniperAuctionCountdownSeconds = 5;
                        }
                        if (string.IsNullOrEmpty(loaded.SniperClickTargetMode))
                        {
                            loaded.SniperClickTargetMode = "FixedTarget";
                        }

                        // If recovered from tempFile and main file is missing, restore main file
                        if (targetToRead == tempFile && !File.Exists(file))
                        {
                            try { File.Move(tempFile, file); } catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                        }

                        return loaded;
                    }
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            return new OptimizationSettings();
        }

        private static readonly object _saveLock = new object();

        public bool Save()
        {
            lock (_saveLock)
            {
                try
                {
                    string file = GetSettingsFilePath();
                    string dir = Path.GetDirectoryName(file);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    string json = serializer.Serialize(this);
                    string tempFile = file + ".tmp";
                    File.WriteAllText(tempFile, json);

                    if (File.Exists(file))
                    {
                        try
                        {
                            File.Replace(tempFile, file, null);
                        }
                        catch
                        {
                            File.Delete(file);
                            File.Move(tempFile, file);
                        }
                    }
                    else
                    {
                        File.Move(tempFile, file);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
