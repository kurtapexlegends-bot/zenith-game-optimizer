using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Security.Principal;
using Microsoft.Win32;

namespace ZenithOptimizer.Services
{
    public class NetworkOptimizer
    {
        private const string SystemProfileKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        private const string TcpInterfacesKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";

        public string TestPing(out long roundtripMs)
        {
            roundtripMs = -1;
            try
            {
                using (Ping p = new Ping())
                {
                    PingReply reply = p.Send("1.1.1.1", 1500);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        roundtripMs = reply.RoundtripTime;
                        string quality = roundtripMs < 30 ? "Ultra-Low Ping (Esports Ready)" : (roundtripMs < 60 ? "Good Ping (Smooth)" : "Moderate Ping");
                        return string.Format("{0} ms • {1}", roundtripMs, quality);
                    }
                }
            }
            catch { }
            return "Unable to test ping. Please check your internet connection.";
        }

        public string BenchmarkConnection(string host, out long avgPing, out long jitterMs, out int packetLossPercent)
        {
            avgPing = -1;
            jitterMs = 0;
            packetLossPercent = 100;

            if (string.IsNullOrEmpty(host)) host = "1.1.1.1";

            long totalMs = 0;
            long minMs = long.MaxValue;
            long maxMs = long.MinValue;
            int successfulReplies = 0;
            int samples = 4;

            try
            {
                using (Ping p = new Ping())
                {
                    for (int i = 0; i < samples; i++)
                    {
                        try
                        {
                            PingReply reply = p.Send(host, 1200);
                            if (reply != null && reply.Status == IPStatus.Success)
                            {
                                successfulReplies++;
                                totalMs += reply.RoundtripTime;
                                if (reply.RoundtripTime < minMs) minMs = reply.RoundtripTime;
                                if (reply.RoundtripTime > maxMs) maxMs = reply.RoundtripTime;
                            }
                        }
                        catch { }
                    }
                }

                if (successfulReplies > 0)
                {
                    avgPing = totalMs / successfulReplies;
                    jitterMs = (maxMs >= minMs) ? (maxMs - minMs) : 0;
                    packetLossPercent = (int)Math.Round(((double)(samples - successfulReplies) / samples) * 100.0);

                    string rating = "Esports Ready (Flawless)";
                    if (avgPing > 60 || jitterMs > 15 || packetLossPercent > 0)
                    {
                        rating = "Good (Minor Jitter)";
                    }
                    if (avgPing > 100 || packetLossPercent > 10)
                    {
                        rating = "Moderate / High Latency";
                    }

                    return string.Format("{0} ms avg (Min: {1}ms, Max: {2}ms, Jitter: ±{3}ms, Loss: {4}%) • {5}",
                        avgPing, minMs, maxMs, jitterMs, packetLossPercent, rating);
                }
            }
            catch { }

            return "Ping test timed out. Verify your internet connection.";
        }

        public bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public bool IsOptimized()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(SystemProfileKey))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("NetworkThrottlingIndex");
                        if (val is int && (int)val == unchecked((int)0xFFFFFFFF))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public bool ApplyNetworkOptimizations(out string message)
        {
            if (!IsAdministrator())
            {
                message = "Administrator privileges required to update network registry keys.";
                return false;
            }

            try
            {
                // 1. Multimedia network responsiveness
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(SystemProfileKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                        key.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                    }
                }

                // 2. Disable Nagle's Algorithm for active network adapters
                using (RegistryKey interfaces = Registry.LocalMachine.OpenSubKey(TcpInterfacesKey, true))
                {
                    if (interfaces != null)
                    {
                        string[] subKeys = interfaces.GetSubKeyNames();
                        foreach (string sub in subKeys)
                        {
                            using (RegistryKey adapter = interfaces.OpenSubKey(sub, true))
                            {
                                if (adapter != null)
                                {
                                    // Only apply to adapters with an IP address configured
                                    object ip = adapter.GetValue("IPAddress");
                                    object dhcpIp = adapter.GetValue("DhcpIPAddress");
                                    if (ip != null || dhcpIp != null)
                                    {
                                        adapter.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                                        adapter.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                                        adapter.SetValue("TcpDelAckTicks", 0, RegistryValueKind.DWord);
                                    }
                                }
                            }
                        }
                    }
                }

                message = "Low-latency network packet delivery active (TCPNoDelay & unthrottled networking enabled).";
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to apply network tweaks: " + ex.Message;
                return false;
            }
        }

        public bool RestoreDefaultNetworkSettings(out string message)
        {
            if (!IsAdministrator())
            {
                message = "Administrator privileges required to restore network settings.";
                return false;
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(SystemProfileKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue("NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
                        key.SetValue("SystemResponsiveness", 20, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey interfaces = Registry.LocalMachine.OpenSubKey(TcpInterfacesKey, true))
                {
                    if (interfaces != null)
                    {
                        string[] subKeys = interfaces.GetSubKeyNames();
                        foreach (string sub in subKeys)
                        {
                            using (RegistryKey adapter = interfaces.OpenSubKey(sub, true))
                            {
                                if (adapter != null)
                                {
                                    try { adapter.DeleteValue("TcpAckFrequency", false); } catch { }
                                    try { adapter.DeleteValue("TCPNoDelay", false); } catch { }
                                    try { adapter.DeleteValue("TcpDelAckTicks", false); } catch { }
                                }
                            }
                        }
                    }
                }

                message = "Windows network defaults restored successfully.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to restore defaults: " + ex.Message;
                return false;
            }
        }
    }
}
