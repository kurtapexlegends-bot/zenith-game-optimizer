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
        private const string ErrorLogPath = @"C:\Users\acost\.gemini\antigravity\scratch\zenith-game-optimizer\error.log";
        private const string MutexName = @"Local\ZenithGameOptimizer_SingleInstance_Mutex";
        private const string EventName = @"Local\ZenithGameOptimizer_ShowWindow_Event";

        private static Mutex _singleInstanceMutex;
        private static EventWaitHandle _showWindowEvent;
        private static RegisteredWaitHandle _registeredWaitHandle;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                SetCurrentProcessExplicitAppUserModelID("Zenith.GameOptimizer.Pro.v2");
            }
            catch { }

            bool createdNew;
            try
            {
                _singleInstanceMutex = new Mutex(true, MutexName, out createdNew);
            }
            catch (AbandonedMutexException)
            {
                createdNew = true;
            }
            catch
            {
                createdNew = false;
            }

            if (!createdNew)
            {
                // Another instance of Zenith is already running!
                try
                {
                    // Signal the primary instance to restore and show its window
                    EventWaitHandle evt;
                    if (EventWaitHandle.TryOpenExisting(EventName, out evt))
                    {
                        evt.Set();
                        evt.Close();
                    }
                    else
                    {
                        // Fallback: Bring main window to front via process handle
                        BringExistingInstanceToFront();
                    }
                }
                catch { }

                // Exit immediately without initializing UI or tray icon
                Environment.Exit(0);
                return;
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
                            if (mw != null)
                            {
                                mw.RestoreFromTray();
                            }
                        }));
                    }
                }, null, -1, false);
            }
            catch { }

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                try
                {
                    File.AppendAllText(ErrorLogPath, string.Format("[{0}] [AppDomain] {1}\r\n", DateTime.Now, args.ExceptionObject));
                }
                catch { }
            };

            DispatcherUnhandledException += (s, args) =>
            {
                try
                {
                    File.AppendAllText(ErrorLogPath, string.Format("[{0}] [Dispatcher] {1}\r\n", DateTime.Now, args.Exception));
                    args.Handled = true; // Prevent app shutdown
                }
                catch { }
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
            catch { }

            base.OnExit(e);
        }

        private static void BringExistingInstanceToFront()
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process proc in Process.GetProcessesByName(current.ProcessName))
                {
                    if (proc.Id != current.Id && proc.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(proc.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(proc.MainWindowHandle);
                        break;
                    }
                }
            }
            catch { }
        }
    }
}
