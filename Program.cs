// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace InfinityAudioEqualizerPro;

internal static class Program
{
    // Each Windows logon session has one interactive instance. Elevated APO
    // helper invocations intentionally bypass this lock and never open the UI.
    private const string MutexName = @"Local\InfinityAudioEqualizerProWindows.Instance";
    private const string WakeName = @"Local\InfinityAudioEqualizerProWindows.Restore";

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(IntPtr window, int command);

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 1 && args[0] == "--uninstall-detach")
        {
            Environment.ExitCode = ApoBridge.UninstallDetachEntry();
            return;
        }
        if (args.Length > 0 && args[0] is "--bridge-install" or "--bridge-remove")
        {
            Environment.ExitCode = ApoBridge.ElevatedEntry(args);
            return;
        }

        // The Windows Run key launches with --startup; ordinary EXE/shortcut launches
        // deliberately keep their existing foreground behavior.
        bool startInTray = args.Any(arg => string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase));

        using var wake = new EventWaitHandle(false, EventResetMode.AutoReset, WakeName);
        using var instance = new Mutex(false, MutexName, out bool firstInstance);
        if (!firstInstance)
        {
            // Windows logon can race with an already-running app. An automatic
            // launch must never bring that instance's main window to the front.
            if (!startInTray)
            {
                TryShowExistingWindow();
                wake.Set();
            }
            return;
        }

        try
        {
            using var main = new MainForm(startInTray);
            var registration = ThreadPool.RegisterWaitForSingleObject(
                wake, (_, _) =>
                {
                    try
                    {
                        if (main.IsHandleCreated && !main.IsDisposed)
                            main.BeginInvoke(new Action(main.RestoreFromSecondLaunch));
                    }
                    catch (ObjectDisposedException) { } // window closed during exit
                    catch (InvalidOperationException) { }
                }, null, Timeout.Infinite, executeOnlyOnce: false);
            try { Application.Run(main); }
            finally { registration.Unregister(null); }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Infinity Audio Equalizer Pro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void TryShowExistingWindow()
    {
        try
        {
            using var self = Process.GetCurrentProcess();
            foreach (var other in Process.GetProcessesByName(self.ProcessName))
            {
                using (other)
                {
                    if (other.Id == self.Id || other.MainWindowHandle == IntPtr.Zero) continue;
                    ShowWindowAsync(other.MainWindowHandle, 9); // SW_RESTORE
                    SetForegroundWindow(other.MainWindowHandle);
                    break;
                }
            }
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
    }
}
