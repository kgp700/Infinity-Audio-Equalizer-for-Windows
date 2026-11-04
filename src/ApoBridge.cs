// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace InfinityAudioEqualizerPro;

/// <summary>
/// Explicit, reversible EqualizerAPO-XT bridge.  Does not alter a device APO registration.
/// A one-time administrator action edits config.txt, with a timestamped backup.
/// Routine EQ writes target only our own APO config subdirectory.
/// </summary>
public static class ApoBridge
{
    public const string Folder = SettingsStore.ProductDirectoryName;
    public const string Marker = "Include: InfinityAudioEqualizerPro\\managed.txt";
    // Old bridge markers are recognized for a one-time elevated bridge upgrade.
    public const string PreviousMarker = "Include: InfinityAudioEqualizerProCS\\managed.txt";
    private const string SuspendedPrefix = "# InfinityAudioEqualizerPro-suspended: ";
    private const string PausedRootPrefix = "# InfinityAudioEqualizerPro-root-paused: ";
    private const string OldSuspendedPrefix = "# InfinityCS-suspended: ";
    private const string OldPausedRootPrefix = "# InfinityCS-root-paused: ";
    private const string BridgeComment = "# Infinity Audio Equalizer Pro for Windows";
    private const string OldBridgeComment = "# Infinity Audio Equalizer Pro for Windows - C#";
    public const string BackupNamePrefix = "config.infinity-audio-equalizer-pro-backup-";
    private const string OldBackupNamePrefix = "config.infinity-cs-backup-";
    private static readonly string[] OtherMarkers = [
        PreviousMarker,
        "Include: InfinityAutoEQStudio\\managed.txt"
    ];
    private static readonly object Gate = new();
    private static readonly int[] TransientRetryMs = [0, 120, 260, 500, 850];

    public static string? DiscoverConfig(AppSettings state)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string? regInstall = null, regConfig = null;
        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\EqualizerAPO");
            regInstall = key?.GetValue("InstallPath") as string;
            regConfig = key?.GetValue("ConfigPath") as string;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException) { }
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(state.XtInstallDir)) candidates.Add(state.XtInstallDir);
        if (!string.IsNullOrWhiteSpace(regInstall))
        {
            bool xtNamed = regInstall.Contains("EqualizerAPO-XT", StringComparison.OrdinalIgnoreCase);
            bool verifiedCustom = File.Exists(Path.Combine(regInstall, "Editor.exe"))
                && (string.IsNullOrWhiteSpace(regConfig) || !regConfig.Contains("EqualizerAPO", StringComparison.OrdinalIgnoreCase)
                    || regConfig.Contains("EqualizerAPO-XT", StringComparison.OrdinalIgnoreCase));
            if (xtNamed || verifiedCustom) candidates.Add(regInstall);
        }
        candidates.Add(Path.Combine(local, "EqualizerAPO-XT", "current"));
        foreach (var name in new[] {"EqualizerAPO-XT-x64-avx2", "EqualizerAPO-XT-x64-avx512", "EqualizerAPO-XT-x64", "EqualizerAPO-XT-x64-sse2"})
            candidates.Add(Path.Combine(local, name, "current"));
        try { candidates.AddRange(Directory.GetDirectories(local, "EqualizerAPO-XT-*").Select(x => Path.Combine(x, "current"))); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        bool verified = candidates.Where(x=>!string.IsNullOrWhiteSpace(x)).Any(x => File.Exists(Path.Combine(x, "DeviceSelector.exe")));
        if (!verified) return null;
        // The registry ConfigPath is the active EqualizerAPO-XT configuration root.
        // Older C# settings could pin a stale path that still contains config.txt;
        // writing it succeeds but never changes the actual sound. Do not prefer
        // that saved path over the installed engine's registered configuration.
        return ChooseConfigDirectory(
            state.ApoConfig,
            regConfig,
            [Path.Combine(local, "EqualizerAPO-XT", "config"),
             .. candidates.Select(x => Path.Combine(x, "config"))]);
    }

    /// <summary>
    /// Select the config folder the installed engine is registered to read.
    /// An old explicit folder is only a fallback, never an authority over the
    /// current registry path or the stable XT config directory.
    /// </summary>
    public static string? ChooseConfigDirectory(string? savedPath, string? registeredPath, IEnumerable<string> fallbackPaths)
    {
        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(registeredPath)) paths.Add(registeredPath);
        paths.AddRange(fallbackPaths.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (!string.IsNullOrWhiteSpace(savedPath)) paths.Add(savedPath);
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(Path.Combine(path, "config.txt"))) return Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or IOException or UnauthorizedAccessException) { }
        }
        return null;
    }

    public static string ManagedPath(string configDirectory) => Path.Combine(configDirectory, Folder, "managed.txt");
    private static string ConfigPath(string configDirectory) => Path.Combine(configDirectory, "config.txt");
    private static bool IsMarker(string line, string marker) => line.Trim().Replace('/', '\\').Equals(marker, StringComparison.OrdinalIgnoreCase);
    private static bool IsOwnedMarker(string line) => IsMarker(line, Marker) || IsMarker(line, PreviousMarker);
    private static bool IsLegacyInclude(string line)
    {
        string normalized = line.Trim().Replace('/', '\\');
        if (OtherMarkers.Any(m => normalized.Equals(m, StringComparison.OrdinalIgnoreCase))) return true;
        // Python's modern engine may use an absolute Documents path.
        return normalized.StartsWith("Include:", StringComparison.OrdinalIgnoreCase)
            && normalized.EndsWith("\\Infinity Audio Equalizer Pro\\EQ\\managed.txt", StringComparison.OrdinalIgnoreCase);
    }
    
    // Root-level processing is global in Equalizer APO. The C# session temporarily
    // pauses only known filter directives and restores their exact original text.
    // Unknown active global directives are never automatically modified.
    public static IReadOnlyList<string> GetUnmanagedRootCommands(string configDirectory)
    {
        var result = new List<string>();
        foreach (string line in File.ReadAllLines(ConfigPath(configDirectory)))
        {
            string x = line.Trim();
            if (x.StartsWith("Device:", StringComparison.OrdinalIgnoreCase)) break;
            if (x.Length == 0 || x.StartsWith('#') || IsMarker(x, Marker)) continue;
            if (!IsLegacyInclude(x) && !IsKnownGlobalEffect(x)) result.Add(x);
        }
        return result;
    }

    private static bool IsKnownGlobalEffect(string line) =>
        Regex.IsMatch(line.Trim(), @"^(Preamp|GraphicEQ|Filter|Copy|Convolution|VSTPlugin|Include|Delay|Channel|Balance):", RegexOptions.IgnoreCase);

    public static bool IsConnected(string configDirectory)
    {
        try
        {
            return File.ReadAllLines(ConfigPath(configDirectory)).Any(line => IsMarker(line, Marker))
                && File.Exists(ManagedPath(configDirectory));
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static IReadOnlyList<string> GetRootEffects(string configDirectory)
    {
        // Effects before the first Device: apply globally. C# bypass cannot
        // cancel them. Report them to the user instead of claiming bit-perfect bypass.
        var found = new List<string>();
        try
        {
            foreach (string raw in File.ReadAllLines(ConfigPath(configDirectory)))
            {
                string line = raw.Trim();
                if (line.StartsWith("#") || line.Length == 0) continue;
                if (line.StartsWith("Device:", StringComparison.OrdinalIgnoreCase)) break;
                if (IsMarker(line, Marker) || IsLegacyInclude(line)) continue;
                if (Regex.IsMatch(line, @"^Preamp:\s*[+\-]?0+(?:\.0+)?\s*dB$", RegexOptions.IgnoreCase)) continue;
                if (Regex.IsMatch(line, @"^(Preamp|GraphicEQ|Filter|Copy|Convolution|VSTPlugin|Include):", RegexOptions.IgnoreCase))
                    found.Add(line);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return found;
    }

    public static bool HasActiveLegacyBridge(string configDirectory)
    {
        try { return File.ReadAllLines(ConfigPath(configDirectory)).Any(line => IsLegacyInclude(line)); }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static bool IsSessionIsolated(string root)
    {
        try
        {
            var lines=File.ReadAllLines(ConfigPath(root));
            int marker=Array.FindIndex(lines,l=>IsMarker(l,Marker));
            int device=Array.FindIndex(lines,l=>l.TrimStart().StartsWith("Device:",StringComparison.OrdinalIgnoreCase));
            if (marker<0 || (device>=0 && marker>device)) return false;
            foreach(string raw in lines)
            {
                string line=raw.Trim();
                if (line.Length==0 || line.StartsWith('#') || IsMarker(line,Marker)
                    || line.StartsWith("Device:",StringComparison.OrdinalIgnoreCase)) continue;
                // Any processing instruction outside our managed.txt can produce
                // audible sound that is absent from the C# UI.
                return false;
            }
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static void Apply(string configDirectory, AppSettings state)
    {
        if (!IsConnected(configDirectory)) throw new InvalidOperationException("APO 연결이 해제되어 있습니다. 먼저 'APO 연결'을 선택하세요.");
        if (HasActiveLegacyBridge(configDirectory))
            throw new InvalidOperationException("기존 음향 설정이 동시에 활성화되어 있습니다. 중복 적용을 방지하기 위해 새 설정을 적용하지 않았습니다.");
        if (!IsSessionIsolated(configDirectory))
            throw new InvalidOperationException("기존 APO 필터가 여전히 활성화되거나 음향 설정 연결 위치가 올바르지 않습니다. 'APO 연결 재구성'을 실행해 주세요.");
        if (!state.BypassAllEffects && state.GlobalEnabled && state.Profiles.Values.Any(p => p.Enabled && p.CompressorEnabled && !p.LowLatency &&
            !p.AmbienceDiagnostic && p.DepthComparisonMode is not ("front" or "rear")))
        {
            if (!File.Exists(Path.Combine(configDirectory, Folder, "InfinityCompressor.vst3", "Contents", "x86_64-win", "InfinityCompressor.vst3")))
                throw new InvalidOperationException("컴프레서 플러그인이 XT 설정 폴더에 설치되지 않았습니다. APO 연결 해제 후 다시 연결해 주세요.");
        }
        lock (Gate)
        {
            ImpulseResponseService.Ensure(Path.Combine(configDirectory,Folder),state);
            string expected = ApoRenderer.Render(state, configDirectory);
            WriteAtomic(ManagedPath(configDirectory), expected);
            string actual = File.ReadAllText(ManagedPath(configDirectory), Encoding.UTF8);
            if (!StringComparer.Ordinal.Equals(expected, actual))
                throw new IOException("APO 설정 저장 후 읽기 검증에 실패했습니다.");
            if (!IsConnected(configDirectory))
                throw new IOException("APO 설정 파일은 저장됐지만 연결 상태가 해제됐습니다.");
        }
    }

    public static string ReadManagedHash(string root)
    {
        byte[] bytes = File.ReadAllBytes(ManagedPath(root));
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }

    private static void WriteAtomic(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = Path.Combine(Path.GetDirectoryName(path)!, ".infinity-audio-equalizer-pro-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            // EqualizerAPO-XT may hold the previous config open for a short period.
            // Keep its last valid contents until the replacement can be committed.
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            for (int attempt = 0; attempt < TransientRetryMs.Length; attempt++)
            {
                if (attempt > 0) Thread.Sleep(TransientRetryMs[attempt]);
                try
                {
                    File.Move(temp, path, true);
                    return;
                }
                catch (IOException) when (attempt < TransientRetryMs.Length - 1) { }
                catch (UnauthorizedAccessException) when (attempt < TransientRetryMs.Length - 1) { }
            }
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (IOException) { } // Leave old config intact; temporary file can be cleaned later.
            catch (UnauthorizedAccessException) { }
        }
    }

    public static void RunElevated(string operation, string configDirectory)
    {
        if (operation is not ("--bridge-install" or "--bridge-remove")) throw new ArgumentException("알 수 없는 작업입니다.");
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("프로그램 실행 경로를 찾지 못했습니다.");
        string sid = WindowsIdentity.GetCurrent().User?.Value ?? throw new InvalidOperationException("사용자 SID를 확인할 수 없습니다.");
        Process? launched;
        try
        {
            launched = Process.Start(new ProcessStartInfo(exe)
            {
                UseShellExecute = true, Verb = "runas", WorkingDirectory = AppContext.BaseDirectory,
                ArgumentList = { operation, configDirectory, sid }
            });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            throw new InvalidOperationException("관리자 권한 승인이 취소되어 APO 설정을 변경하지 않았습니다.", ex);
        }
        using var proc = launched ?? throw new InvalidOperationException("관리자 권한 작업을 시작하지 못했습니다.");
        proc.WaitForExit();
        if (proc.ExitCode != 0) throw new InvalidOperationException("APO 연결 변경을 완료하지 못했습니다. 관리자 권한 승인 및 설치 경로를 확인하세요.");
    }

    public static int ElevatedEntry(string[] args)
    {
        if (args.Length != 3) return 2;
        if (!Regex.IsMatch(args[2], @"^S-1-\d+(?:-\d+)+$")) return 2;
        try
        {
            // The helper is only permitted to touch the discovered XT config root.
            var discovered = DiscoverConfig(SettingsStore.Load());
            if (discovered is null || !Path.GetFullPath(discovered).Equals(Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("등록된 EqualizerAPO-XT 설정 폴더가 아닙니다.");
            var path = ConfigPath(discovered);
            if (!File.Exists(path)) throw new FileNotFoundException("config.txt가 없습니다.", path);
            if (args[0] == "--bridge-install") Install(discovered, args[2]);
            else if (args[0] == "--bridge-remove") Remove(discovered);
            else return 2;
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Infinity Audio Equalizer Pro - APO 연결", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    public static string? LastConnectionBackup(string root)
    {
        try
        {
            string path = Path.Combine(root, Folder, "last_connection_backup.txt");
            if (!File.Exists(path)) return null;
            string candidate = File.ReadAllText(path).Trim();
            // Do not trust arbitrary paths inside the receipt.
            string name = Path.GetFileName(candidate) ?? string.Empty;
            if (!name.StartsWith(BackupNamePrefix, StringComparison.OrdinalIgnoreCase)
                && !name.StartsWith(OldBackupNamePrefix, StringComparison.OrdinalIgnoreCase)) return null;
            string? directory = Path.GetDirectoryName(candidate);
            if (string.IsNullOrWhiteSpace(directory) || !Path.GetFullPath(directory)
                .Equals(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)) return null;
            return File.Exists(candidate) ? candidate : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { return null; }
    }

    private static void StoreBackupReceipt(string root, string backup)
    {
        try { WriteAtomic(Path.Combine(root, Folder, "last_connection_backup.txt"), backup); }
        catch (IOException) { /* The connection backup itself has already been saved. */ }
        catch (UnauthorizedAccessException) { }
    }

    private static string Backup(string root)
    {
        string file = ConfigPath(root);
        string backup = Path.Combine(root, BackupNamePrefix + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".txt");
        File.Copy(file, backup, false);
        return backup;
    }

    private static void GrantManagedPermissions(string root, string sid)
    {
        var folder = Path.Combine(root, Folder);
        using var p = Process.Start(new ProcessStartInfo("icacls.exe")
        {
            UseShellExecute = false, CreateNoWindow = true,
            ArgumentList = { folder, "/grant:r", "*" + sid + ":(OI)(CI)M" }
        }) ?? throw new InvalidOperationException("APO 설정 쓰기 권한을 설정하지 못했습니다.");
        p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException("APO 설정 폴더의 쓰기 권한을 설정하지 못했습니다.");
    }

    // Deterministic root transformation, also used by smoke tests. The original
    // config must remain recoverable, including global preamp and example include.
    public static string PrepareConfigForSession(string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Any(x => IsMarker(x, Marker)))
            throw new InvalidOperationException("이미 음향 설정이 연결되어 있습니다.");
        for (int i=0; i<lines.Count; i++)
        {
            string trimmed=lines[i].Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            // Device selectors are retained, but their processing commands must not
            // leak through the C# bypass or contaminate profiles after reconnect.
            if (trimmed.StartsWith("Device:", StringComparison.OrdinalIgnoreCase)) continue;
            if (!IsKnownGlobalEffect(trimmed))
                throw new InvalidOperationException("자동으로 분리할 수 없는 APO 명령이 있습니다: " + trimmed);
            lines[i] = IsLegacyInclude(trimmed) ? SuspendedPrefix + lines[i].Trim() : PausedRootPrefix + lines[i];
        }
        // Include first: a trailing Include after Device: may inherit the last
        // device selector and silently apply filters to the wrong device.
        return BridgeComment + "\n" + Marker + "\n\n" + string.Join("\n", lines).TrimEnd() + "\n";
    }

    public static string RestoreConfigFromSession(string content)
    {
        var lines = new List<string>();
        bool found = false;
        foreach (string line in content.Replace("\r\n", "\n").Split('\n'))
        {
            if (IsOwnedMarker(line)) { found = true; continue; }
            if (line.Trim().Equals(BridgeComment, StringComparison.OrdinalIgnoreCase)
                || line.Trim().Equals(OldBridgeComment, StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith(SuspendedPrefix, StringComparison.Ordinal)
                || line.StartsWith(OldSuspendedPrefix, StringComparison.Ordinal))
            {
                string prefix = line.StartsWith(SuspendedPrefix, StringComparison.Ordinal)
                    ? SuspendedPrefix : OldSuspendedPrefix;
                string original = line[prefix.Length..];
                if (IsLegacyInclude(original)) { lines.Add(original); continue; }
            }
            if (line.StartsWith(PausedRootPrefix, StringComparison.Ordinal)
                || line.StartsWith(OldPausedRootPrefix, StringComparison.Ordinal))
            {
                string prefix = line.StartsWith(PausedRootPrefix, StringComparison.Ordinal)
                    ? PausedRootPrefix : OldPausedRootPrefix;
                lines.Add(line[prefix.Length..]);
                continue;
            }
            lines.Add(line);
        }
        if (!found) throw new InvalidOperationException("APO 연결 기록이 없습니다.");
        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private static void DeployCompressorPlugin(string root)
    {
        string source = Path.Combine(AppContext.BaseDirectory, "InfinityCompressor.vst3", "Contents", "x86_64-win", "InfinityCompressor.vst3");
        if (!File.Exists(source) || new FileInfo(source).Length < 20_000)
            throw new FileNotFoundException("InfinityCompressor.vst3 배포 파일이 없습니다.", source);
        string destination = Path.Combine(root, Folder, "InfinityCompressor.vst3", "Contents", "x86_64-win", "InfinityCompressor.vst3");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var sourceHash = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(source));
        if (!File.Exists(destination) || !System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(destination)).SequenceEqual(sourceHash))
            File.Copy(source, destination, true);
    }

    private static void Install(string root, string sid)
    {
        var path = ConfigPath(root);
        var raw = File.ReadAllText(path, Encoding.UTF8);
        if (raw.Split('\n').Any(IsOwnedMarker))
        {
            // Upgrade older bridge sessions, which placed Include after Device:
            // and left device-specific filters active, to isolated C# mode.
            string recovered = RestoreConfigFromSession(raw);
            string upgraded = PrepareConfigForSession(recovered);
            string backup = Backup(root);
            Directory.CreateDirectory(Path.Combine(root, Folder));
            GrantManagedPermissions(root, sid);
            DeployCompressorPlugin(root);
            if (!File.Exists(ManagedPath(root))) WriteAtomic(ManagedPath(root), "# Awaiting audio settings\n");
            WriteAtomic(path, upgraded);
            StoreBackupReceipt(root, backup);
            return;
        }
        // Prepare completely before modifying either config.txt or the ACL.
        string updated = PrepareConfigForSession(raw);
        string firstBackup = Backup(root);
        Directory.CreateDirectory(Path.Combine(root, Folder));
        GrantManagedPermissions(root, sid);
        DeployCompressorPlugin(root);
        WriteAtomic(ManagedPath(root), "# Awaiting audio settings\n");
        WriteAtomic(path, updated);
        StoreBackupReceipt(root, firstBackup);
    }

    /// <summary>
    /// Development-mode disconnect: stop ALL DSP originating in config.txt, not
    /// restore older Python/XT effects that the UI cannot account for. The old
    /// full config remains available in a timestamped backup. Device APO
    /// registration is NOT modified; users may remove it with XT DeviceSelector.
    /// </summary>
    public static string NeutralConfigAfterDisconnect(string source)
    {
        if (!source.Replace("\r", "").Split('\n').Any(IsOwnedMarker))
            throw new InvalidOperationException("APO 연결 기록이 없습니다. 다른 프로그램 설정은 변경하지 않았습니다.");
        return "# EqualizerAPO-XT configuration - no active DSP commands.\n"
             + "# Infinity Audio Equalizer Pro for Windows was disconnected.\n"
             + "# Former configuration was saved separately before this cleanup.\n";
    }

    /// <summary>
    /// Called only by the installer before removing application binaries. This does
    /// not rely on a user profile (the elevated uninstaller can run under another
    /// account). Only an XT config explicitly marked as ours may be modified.
    /// The original config is backed up and the device APO remains installed.
    /// </summary>
    public static int UninstallDetachEntry()
    {
        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\EqualizerAPO");
            string? root = key?.GetValue("ConfigPath") as string;
            if (string.IsNullOrWhiteSpace(root)) return 0; // XT not installed
            string path = Path.Combine(Path.GetFullPath(root), "config.txt");
            if (!File.Exists(path)) return 0;
            string raw = File.ReadAllText(path, Encoding.UTF8);
            if (!raw.Replace("\r", "").Split('\n').Any(IsOwnedMarker)) return 0;
            Remove(root); // Back up first, disconnect only our Include, leave XT registered.
            return 0;
        }
        catch (Exception e)
        {
            DiagnosticLog.Error("Uninstall APO disconnect", e);
            return 1; // Let uninstall give the user a meaningful choice.
        }
    }

    private static void Remove(string root)
    {
        var path = ConfigPath(root);
        var raw = File.ReadAllText(path, Encoding.UTF8);
        if (!raw.Split('\n').Any(IsOwnedMarker)) return;
        // Never restore suspended filters. Older preamps (e.g. -6 dB) would
        // otherwise silently remain active after uninstalling the C# app.
        string neutral = NeutralConfigAfterDisconnect(raw);
        Backup(root);
        WriteAtomic(path, neutral);
        // Clean up the C#-owned configuration folder, but only after the active
        // include is removed. A loaded DLL might still be locked by Windows.
        // In that situation the root remains neutral and removal is safe.
        try
        {
            var managedFolder = Path.Combine(root, Folder);
            if (Directory.Exists(managedFolder)) Directory.Delete(managedFolder, recursive:true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
