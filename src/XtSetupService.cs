// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace InfinityAudioEqualizerPro;

/// <summary>Official XT release installation, byte-for-byte SHA-256 verification, and device selector launch.
/// The release checksum is retrieved from the same GitHub release; this protects against transport corruption,
/// but is NOT an independent publisher-signature verification.</summary>
internal static class XtSetupService
{
    private const string ReleaseUrl = "https://api.github.com/repos/115dkk/EqualizerAPO-XT/releases/latest";
    private const long MaxInstaller = 150_000_000;
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = true })
    { Timeout = TimeSpan.FromSeconds(120) };
    static XtSetupService()
    {
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("InfinityAudioEqualizerProWindows/1.0");
        Client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public static async Task<string> DownloadVerifiedAsync()
    {
        using var response = await Client.GetAsync(ReleaseUrl);
        response.EnsureSuccessStatusCode();
        using var release = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var assets = release.RootElement.GetProperty("assets").EnumerateArray().ToArray();
        string Asset(string name)
        {
            foreach (var asset in assets)
            {
                if (asset.GetProperty("name").GetString() != name) continue;
                var url = asset.GetProperty("browser_download_url").GetString() ?? "";
                if (!Uri.TryCreate(url, UriKind.Absolute, out var u)
                    || u.Scheme != Uri.UriSchemeHttps
                    || !u.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
                    || !u.AbsolutePath.StartsWith("/115dkk/EqualizerAPO-XT/releases/download/", StringComparison.Ordinal))
                    throw new InvalidOperationException("공식 XT 릴리스 주소가 아닙니다.");
                return url;
            }
            throw new InvalidOperationException("공식 릴리스 구성 파일이 없습니다: " + name);
        }
        string sums = System.Text.Encoding.UTF8.GetString(await DownloadBytes(Asset("SHA256SUMS.txt"), 250_000));
        var matches = Regex.Matches(sums, @"(?im)^([a-f0-9]{64})\s+\*?EqualizerAPO-XT-Setup\.exe\s*$");
        if (matches.Count != 1) throw new InvalidOperationException("공식 설치 파일 SHA-256 기록이 누락되었습니다.");
        byte[] bytes = await DownloadBytes(Asset("EqualizerAPO-XT-Setup.exe"), MaxInstaller);
        if (bytes.Length < 100_000 || bytes[0] != 'M' || bytes[1] != 'Z'
            || !Convert.ToHexString(SHA256.HashData(bytes)).Equals(matches[0].Groups[1].Value, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("XT 설치 파일 검증에 실패했습니다. 실행하지 않았습니다.");
        string directory = Path.Combine(Path.GetTempPath(), "InfinityAudioEqualizerPro", "XTInstall");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "EqualizerAPO-XT-Setup.exe");
        string temp = path + ".download";
        try { await File.WriteAllBytesAsync(temp, bytes); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return path;
    }

    private static async Task<byte[]> DownloadBytes(string url, long max)
    {
        using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > max)
            throw new InvalidOperationException("파일 다운로드 크기 제한을 초과했습니다.");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var output = new MemoryStream();
        var buffer = new byte[64 * 1024];
        int count;
        while ((count = await stream.ReadAsync(buffer)) > 0)
        {
            if (output.Length + count > max)
                throw new InvalidOperationException("파일 다운로드 크기 제한을 초과했습니다.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }

    public static Process RunInstaller(string installer)
    {
        return Process.Start(new ProcessStartInfo(installer)
        { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(installer)! })
            ?? throw new InvalidOperationException("XT 설치 프로그램을 시작하지 못했습니다.");
        // The caller awaits exit asynchronously before offering device registration.
    }

    // Velopack can leave a 'current' directory behind after uninstall. Editor.exe
    // and DeviceSelector.exe alone are NOT proof of an installed APO. The Windows
    // installed-app registration is checked first to avoid ghost installations.
    private static bool HasXtInstalledAppRegistration()
    {
        const string uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var root = baseKey.OpenSubKey(uninstall);
                if (root is null) continue;
                foreach (string subName in root.GetSubKeyNames())
                {
                    using var entry = root.OpenSubKey(subName);
                    string display = entry?.GetValue("DisplayName") as string ?? "";
                    // Windows 'Installed apps' shows builds as EQ APO XT AVX2,
                    // EQ APO XT SSE2, etc., not necessarily EqualizerAPO-XT.
                    if (display.StartsWith("EQ APO XT", StringComparison.OrdinalIgnoreCase)
                        || display.StartsWith("EqualizerAPO-XT", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
        }
        return false;
    }

    public static string? FindDeviceSelector(AppSettings state)
    {
        // A saved installation path and orphaned binaries must never suppress the
        // first-launch setup prompt after the user uninstalls EqualizerAPO-XT.
        if (!HasXtInstalledAppRegistration()) return null;
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roots = new List<string>();
        // Favor the engine registered with Windows over an old saved hint.
        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\EqualizerAPO");
            var path = key?.GetValue("InstallPath") as string;
            if (!string.IsNullOrWhiteSpace(path)
                && (path.Contains("EqualizerAPO-XT", StringComparison.OrdinalIgnoreCase)
                    || File.Exists(Path.Combine(path, "Editor.exe")))) roots.Add(path);
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
        if (!string.IsNullOrWhiteSpace(state.XtInstallDir)) roots.Add(state.XtInstallDir);
        roots.Add(Path.Combine(local, "EqualizerAPO-XT", "current"));
        foreach (var suffix in new[] { "x64-avx2", "x64-avx512", "x64-avx", "x64-avx10-1", "x64", "x64-sse2", "arm64-neon" })
            roots.Add(Path.Combine(local, "EqualizerAPO-XT-" + suffix, "current"));
        try { roots.AddRange(Directory.GetDirectories(local, "EqualizerAPO-XT-*").Select(x => Path.Combine(x, "current"))); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        foreach (var root in roots)
            foreach (var candidate in new[] { root, Path.Combine(root, "current") })
            {
                var exe = Path.Combine(candidate, "DeviceSelector.exe");
                if (File.Exists(exe) && File.Exists(Path.Combine(candidate, "Editor.exe"))) return exe;
            }
        return null;
    }

    public static string? ValidateInstallationFolder(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return null;
        foreach (var root in new[] { directory, Path.Combine(directory,"current") })
            if (File.Exists(Path.Combine(root,"Editor.exe")) && File.Exists(Path.Combine(root,"DeviceSelector.exe")))
                return root;
        return null;
    }

    public static Process RunDeviceSelector(string exe)
    {
        return Process.Start(new ProcessStartInfo(exe)
        { UseShellExecute = true, Verb = "runas", WorkingDirectory = Path.GetDirectoryName(exe)! })
            ?? throw new InvalidOperationException("XT 출력 장치 등록 도구를 시작하지 못했습니다.");
    }
}
