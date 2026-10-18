using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace ZenithOptimizer
{
    public partial class App : Application
    {
        private static string ErrorLogPath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZenithOptimizer");
                try
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[App] Directory.CreateDirectory: " + ex.Message);
                }
                return Path.Combine(dir, "error.log");
            }
        }
        private const string MutexName = @"Local\ZenithGameOptimizer_SingleInstance_Mutex";
        private const string EventName = @"Local\ZenithGameOptimizer_ShowWindow_Event";

        private static Mutex _singleInstanceMutex;
        private static EventWaitHandle _showWindowEvent;
        private static RegisteredWaitHandle _registeredWaitHandle;
        public const string ShowWindowMessageName = "ZenithGameOptimizer_ShowWindow_Message";

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);
        private const int ASFW_ANY = -1;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                ResourceAssembly = typeof(App).Assembly;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[App] ResourceAssembly: " + ex.Message);
            }

            try
            {
                SetCurrentProcessExplicitAppUserModelID("Zenith.GameOptimizer.Pro.v2");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[App] SetAppUserModelID: " + ex.Message);
            }

            bool createdNew;
            try
            {
                _singleInstanceMutex = new Mutex(true, MutexName, out createdNew);
            }
            catch (AbandonedMutexException)
            {
                createdNew = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[App] SingleInstanceMutex: " + ex.Message);
                createdNew = false;
            }

            if (!createdNew)
            {
                // Another instance of Zenith is registered with the mutex
                bool restored = false;
                try
                {
                    AllowSetForegroundWindow(ASFW_ANY);

                    uint showMsg = RegisterWindowMessage(ShowWindowMessageName);
                    PostMessage(HWND_BROADCAST, showMsg, IntPtr.Zero, IntPtr.Zero);

                    EventWaitHandle evt;
                    if (EventWaitHandle.TryOpenExisting(EventName, out evt))
                    {
                        evt.Set();
                        evt.Close();
                    }

                    restored = BringExistingInstanceToFront();
                    if (!restored)
                    {
                        // Wait briefly up to 300ms for background process tray restore
                        for (int attempt = 0; attempt < 6; attempt++)
                        {
                            Thread.Sleep(50);
                            restored = BringExistingInstanceToFront();
                            if (restored) break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[App] BringExistingInstanceToFront: " + ex.Message);
                }

                if (restored)
                {
                    // Existing instance window was found and restored to front! Exit duplicate.
                    Environment.Exit(0);
                    return;
                }

                // If we get here, an existing instance holds the mutex or process name, BUT HAS NO WINDOW!
                // It is a headless or orphaned zombie process.
                // Self-Healing: Terminate the headless zombie process so the user is NEVER locked out!
                try
                {
                    Process cur = Process.GetCurrentProcess();
                    foreach (Process p in Process.GetProcessesByName(cur.ProcessName))
                    {
                        if (p.Id != cur.Id)
                        {
                            try
                            {
                                p.Kill();
                                p.WaitForExit(800);
                            }
                            catch (Exception exKill)
                            {
                                Debug.WriteLine("[App] Kill zombie: " + exKill.Message);
                            }
                        }
                    }

                    // Release old mutex handle and reacquire clean ownership
                    if (_singleInstanceMutex != null)
                    {
                        try { _singleInstanceMutex.Close(); } catch { }
                    }
                    _singleInstanceMutex = new Mutex(true, MutexName, out createdNew);
                }
                catch (Exception exRecover)
                {
                    Debug.WriteLine("[App] Zombie recovery: " + exRecover.Message);
                }
            }

            // Primary instance: Create named event to listen for subsequent launches
            try
            {
                _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
                _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(_showWindowEvent, (state, timedOut) =>
                {
                    if (Current != null && Current.Dispatcher != null)
                    {
                        Current.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            MainWindow mw = Current.MainWindow as MainWindow;
                            if (mw == null)
                            {
                                foreach (Window w in Current.Windows)
                                {
                                    if (w is MainWindow) { mw = (MainWindow)w; break; }
                                }
                            }
                            if (mw != null)
                            {
                                mw.RestoreFromTray();
                            }
                            else
                            {
                                mw = new MainWindow();
                                Current.MainWindow = mw;
                                mw.Show();
                            }
                        }));
                    }
                }, null, -1, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[App] ShowWindowEvent register: " + ex.Message);
            }

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                try
                {
                    File.AppendAllText(ErrorLogPath, string.Format("[{0}] [AppDomain] {1}\r\n", DateTime.Now, args.ExceptionObject));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[App] UnhandledException log failed: " + ex.Message);
                }
            };

            DispatcherUnhandledException += (s, args) =>
            {
                try
                {
                    File.AppendAllText(ErrorLogPath, string.Format("[{0}] [Dispatcher] {1}\r\n", DateTime.Now, args.Exception));
                    args.Handled = true; // Prevent app shutdown
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[App] DispatcherUnhandledException log failed: " + ex.Message);
                }
            };

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_registeredWaitHandle != null)
                {
                    _registeredWaitHandle.Unregister(null);
                    _registeredWaitHandle = null;
                }
                if (_showWindowEvent != null)
                {
                    _showWindowEvent.Close();
                    _showWindowEvent = null;
                }
                if (_singleInstanceMutex != null)
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Close();
                    _singleInstanceMutex = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[App] OnExit cleanup: " + ex.Message);
            }

            base.OnExit(e);
        }

        private static bool BringExistingInstanceToFront()
        {
            bool broughtToFront = false;
            try
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process proc in Process.GetProcessesByName(current.ProcessName))
                {
                    if (proc.Id == current.Id) continue;

                    // If existing process is frozen / not responding, kill it immediately!
                    if (!proc.Responding)
                    {
                        try { proc.Kill(); } catch { }
                        continue;
                    }

                    AllowSetForegroundWindow(proc.Id);

                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(proc.MainWindowHandle, SW_SHOW);
                        ShowWindow(proc.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(proc.MainWindowHandle);
                        broughtToFront = true;
                        break;
                    }
                    else
                    {
                        // Window is hidden in tray: enumerate top-level windows of the process
                        uint targetPid = (uint)proc.Id;
                        IntPtr foundHwnd = IntPtr.Zero;
                        EnumWindows((hWnd, lParam) =>
                        {
                            uint pid;
                            GetWindowThreadProcessId(hWnd, out pid);
                            if (pid == targetPid)
                            {
                                System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
                                GetWindowText(hWnd, sb, 256);
                                string title = sb.ToString();
                                if (!string.IsNullOrEmpty(title) && title.IndexOf("Zenith", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    foundHwnd = hWnd;
                                    return false; // stop enumeration
                                }
                            }
                            return true;
                        }, IntPtr.Zero);

                        if (foundHwnd != IntPtr.Zero)
                        {
                            ShowWindow(foundHwnd, SW_SHOW);
                            ShowWindow(foundHwnd, SW_RESTORE);
                            SetForegroundWindow(foundHwnd);
                            broughtToFront = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[App] BringExistingInstanceToFront proc enumerate: " + ex.Message);
            }
            return broughtToFront;
        }
    }
}
