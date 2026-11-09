// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace InfinityAudioEqualizerPro;

/// <summary>Downloads the official GitHub installer and verifies its SHA-256 against the
/// release API asset digest. This protects integrity but does not independently authenticate the publisher.</summary>
internal static class XtSetupService
{
    private const string ReleaseUrl = "https://api.github.com/repos/115dkk/EqualizerAPO-XT/releases/latest";
    private const long MaxInstaller = 150_000_000;
    private const int MaxAttempts = 3;
    private const string AppUserAgent = "InfinityAudioEqualizerProWindows";
    // A browser-compatible fallback is used only after a transient request failure.
    private const string EdgeCompatibleUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0";
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = true })
    { Timeout = TimeSpan.FromSeconds(120) };

    internal readonly record struct DownloadProgress(string Stage, long Downloaded = 0,
        long? Total = null, int Attempt = 1, int MaxAttempts = 3, bool BrowserFallback = false);

    private static void Update(IProgress<DownloadProgress>? progress, string stage,
        int attempt, long downloaded = 0, long? total = null) =>
        progress?.Report(new DownloadProgress(stage, downloaded, total, attempt, MaxAttempts, attempt > 1));

    // A new request is built on every attempt, including both the metadata and binary requests.
    // Keep HTTPS, URL validation, maximum size, executable format and digest checks mandatory.
    public static async Task<string> DownloadVerifiedAsync(IProgress<DownloadProgress>? progress = null)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try { return await DownloadVerifiedOnceAsync(attempt, progress); }
            catch (HttpRequestException ex) when (attempt < MaxAttempts && IsTransient(ex.StatusCode))
            {
                Update(progress, "서버 응답 지연 · 재시도 준비 중", attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(attempt * 850));
            }
            catch (TaskCanceledException) when (attempt < MaxAttempts)
            {
                Update(progress, "연결 시간 초과 · 재시도 준비 중", attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(attempt * 850));
            }
        }
        throw new InvalidOperationException("EqualizerAPO-XT 다운로드 재시도 횟수를 초과했습니다.");
    }

    private static bool IsTransient(HttpStatusCode? code) =>
        code is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout or HttpStatusCode.InternalServerError
            || code is null;

    private static HttpRequestMessage Request(string url, int attempt, bool isApi)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(attempt > 1 ? EdgeCompatibleUserAgent : AppUserAgent);
        request.Headers.Accept.ParseAdd(isApi ? "application/vnd.github+json" : "application/octet-stream");
        if (isApi) request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private static async Task<HttpResponseMessage> SendAsync(string url, int attempt, bool isApi, string stage)
    {
        using var request = Request(url, attempt, isApi);
        try
        {
            var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            try { response.EnsureSuccessStatusCode(); }
            catch { response.Dispose(); throw; }
            return response;
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"{stage} 실패 (HTTP {(ex.StatusCode is null ? "네트워크" : ((int)ex.StatusCode.Value).ToString())}, URL: {url})",
                ex, ex.StatusCode);
        }
    }

    private static async Task<string> DownloadVerifiedOnceAsync(int attempt, IProgress<DownloadProgress>? progress)
    {
        Update(progress, "최신 릴리스 정보 확인 중", attempt);
        using var response = await SendAsync(ReleaseUrl, attempt, isApi: true, stage: "릴리스 정보 조회");
        using var release = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!release.RootElement.TryGetProperty("assets", out var assetList)
            || assetList.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("공식 릴리스의 다운로드 목록을 확인하지 못했습니다.");
        JsonElement? selected = null;
        foreach (var asset in assetList.EnumerateArray())
        {
            if (asset.TryGetProperty("name", out var name) && name.GetString() == "EqualizerAPO-XT-Setup.exe")
            {
                selected = asset;
                break;
            }
        }
        if (selected is null) throw new InvalidOperationException("공식 릴리스에 XT 설치 파일이 없습니다.");
        var installerAsset = selected.Value;
        string url = installerAsset.GetProperty("browser_download_url").GetString() ?? "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || parsed.Scheme != Uri.UriSchemeHttps
            || !parsed.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            || !parsed.AbsolutePath.StartsWith("/115dkk/EqualizerAPO-XT/releases/download/", StringComparison.Ordinal)
            || !parsed.AbsolutePath.EndsWith("/EqualizerAPO-XT-Setup.exe", StringComparison.Ordinal))
            throw new InvalidOperationException("공식 XT 릴리스 설치 파일 주소가 아닙니다.");

        // The API now provides the authoritative asset SHA-256 digest. No SHA256SUMS.txt request.
        string digest = installerAsset.TryGetProperty("digest", out var digestField)
            && digestField.ValueKind == JsonValueKind.String ? digestField.GetString() ?? "" : "";
        if (!Regex.IsMatch(digest, @"\Asha256:[a-fA-F0-9]{64}\z"))
            throw new InvalidOperationException("공식 릴리스에 유효한 설치 파일 SHA-256 정보가 없습니다.");

        Update(progress, "설치 파일 다운로드 중", attempt);
        byte[] bytes = await DownloadBytes(url, MaxInstaller, attempt, progress);
        Update(progress, "다운로드 파일 안전성 검사 중", attempt, bytes.Length, bytes.Length);
        if (bytes.Length < 100_000 || bytes[0] != 'M' || bytes[1] != 'Z'
            || !Convert.ToHexString(SHA256.HashData(bytes)).Equals(digest[7..], StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("XT 설치 파일 SHA-256 검증에 실패했습니다. 파일을 실행하지 않았습니다.");

        string directory = Path.Combine(Path.GetTempPath(), "InfinityAudioEqualizerPro", "XTInstall");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "EqualizerAPO-XT-Setup.exe");
        string temp = path + ".download";
        try { await File.WriteAllBytesAsync(temp, bytes); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return path;
    }

    private static async Task<byte[]> DownloadBytes(string url, long max, int attempt,
        IProgress<DownloadProgress>? progress)
    {
        using var response = await SendAsync(url, attempt, isApi: false, stage: "설치 파일 다운로드");
        long? total = response.Content.Headers.ContentLength;
        if (total > max) throw new InvalidOperationException("파일 다운로드 크기 제한을 초과했습니다.");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var output = new MemoryStream();
        var buffer = new byte[64 * 1024];
        int count;
        while ((count = await stream.ReadAsync(buffer)) > 0)
        {
            if (output.Length + count > max)
                throw new InvalidOperationException("파일 다운로드 크기 제한을 초과했습니다.");
            output.Write(buffer, 0, count);
            Update(progress, "설치 파일 다운로드 중", attempt, output.Length, total);
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
