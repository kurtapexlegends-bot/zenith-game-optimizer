using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZenithOptimizer.Services.Launcher
{
    public static class IconExtractor
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        private static readonly Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _iconCacheLock = new object();

        public static void ClearCache()
        {
            lock (_iconCacheLock)
            {
                _iconCache.Clear();
            }
        }

        public static ImageSource Extract(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath)) return null;

            lock (_iconCacheLock)
            {
                ImageSource cached;
                if (_iconCache.TryGetValue(targetPath, out cached))
                {
                    return cached;
                }
            }

            try
            {
                string resolved = ResolveExecutablePath(targetPath);
                if (string.IsNullOrEmpty(resolved) || !File.Exists(resolved)) return null;

                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(resolved))
                {
                    if (icon != null)
                    {
                        using (System.Drawing.Bitmap bmp = icon.ToBitmap())
                        {
                            IntPtr hBitmap = bmp.GetHbitmap();
                            try
                            {
                                BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap,
                                    IntPtr.Zero,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                                if (bs != null)
                                {
                                    if (bs.CanFreeze) bs.Freeze();

                                    lock (_iconCacheLock)
                                    {
                                        _iconCache[targetPath] = bs;
                                    }

                                    return bs;
                                }
                            }
                            finally
                            {
                                if (hBitmap != IntPtr.Zero)
                                {
                                    DeleteObject(hBitmap);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            return null;
        }

        public static string ResolveExecutablePath(string detectedPath)
        {
            if (string.IsNullOrEmpty(detectedPath)) return null;

            try
            {
                if (File.Exists(detectedPath) && (detectedPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || detectedPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)))
                {
                    return detectedPath;
                }

                if (File.Exists(detectedPath) && detectedPath.EndsWith(".acf", StringComparison.OrdinalIgnoreCase))
                {
                    string dir = Path.GetDirectoryName(detectedPath);
                    string[] lines = File.ReadAllLines(detectedPath);
                    string installDir = null;
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("\"installdir\"", StringComparison.OrdinalIgnoreCase))
                        {
                            int q1 = trimmed.IndexOf('"', 12);
                            if (q1 >= 0)
                            {
                                int q2 = trimmed.IndexOf('"', q1 + 1);
                                if (q2 > q1)
                                {
                                    installDir = trimmed.Substring(q1 + 1, q2 - q1 - 1);
                                    break;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(installDir) && dir != null)
                    {
                        string commonDir = Path.Combine(dir, "common", installDir);
                        if (Directory.Exists(commonDir))
                        {
                            string[] rootExes = Directory.GetFiles(commonDir, "*.exe", SearchOption.TopDirectoryOnly);
                            string candidate = FindBestExecutable(rootExes);
                            if (!string.IsNullOrEmpty(candidate)) return candidate;

                            string[] candidateSubfolders = new string[]
                            {
                                "bin", @"bin\win64", @"bin\x64", @"Binaries\Win64", @"Binaries\Win32", @"game\bin\win64", @"game\bin", "x64"
                            };

                            foreach (var sub in candidateSubfolders)
                            {
                                string subPath = Path.Combine(commonDir, sub);
                                if (Directory.Exists(subPath))
                                {
                                    string[] subExes = Directory.GetFiles(subPath, "*.exe", SearchOption.TopDirectoryOnly);
                                    candidate = FindBestExecutable(subExes);
                                    if (!string.IsNullOrEmpty(candidate)) return candidate;
                                }
                            }

                            try
                            {
                                foreach (string subDir in Directory.GetDirectories(commonDir))
                                {
                                    string dirName = Path.GetFileName(subDir).ToLowerInvariant();
                                    if (dirName.StartsWith("_") || dirName.Contains("redist") || dirName.Contains("directx") || dirName.Contains("support"))
                                        continue;

                                    string[] subExes = Directory.GetFiles(subDir, "*.exe", SearchOption.TopDirectoryOnly);
                                    candidate = FindBestExecutable(subExes);
                                    if (!string.IsNullOrEmpty(candidate)) return candidate;
                                }
                            }
                            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }
                        }
                    }
                }

                if (File.Exists(detectedPath) && detectedPath.EndsWith(".item", StringComparison.OrdinalIgnoreCase))
                {
                    string json = File.ReadAllText(detectedPath);
                    Match mLoc = Regex.Match(json, @"""InstallLocation""\s*:\s*""([^""]+)""");
                    Match mExe = Regex.Match(json, @"""LaunchExecutable""\s*:\s*""([^""]+)""");
                    if (mLoc.Success && mExe.Success)
                    {
                        string exe = Path.Combine(mLoc.Groups[1].Value.Replace(@"\\", @"\"), mExe.Groups[1].Value.Replace(@"\\", @"\"));
                        if (File.Exists(exe)) return exe;
                    }
                }
            }
            catch (Exception exDiag) { System.Diagnostics.Debug.WriteLine(exDiag.Message); }

            return detectedPath;
        }

        public static string FindBestExecutable(string[] exes)
        {
            if (exes == null || exes.Length == 0) return null;
            foreach (string exe in exes)
            {
                string fname = Path.GetFileName(exe).ToLowerInvariant();
                if (!fname.Contains("crash") && !fname.Contains("install") && !fname.Contains("setup") &&
                    !fname.Contains("update") && !fname.Contains("unitycrash") && !fname.Contains("redist") &&
                    !fname.Contains("unins") && !fname.Contains("launcher") && !fname.Contains("helper"))
                {
                    return exe;
                }
            }
            return exes[0];
        }
    }
}
