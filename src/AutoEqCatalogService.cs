// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityAudioEqualizerPro;

/// <summary>Public AutoEq catalog; no custom remote endpoints, no executable downloads.</summary>
public static class AutoEqCatalogService
{
    public sealed record Model(string Name, string Folder)
    {
        public override string ToString() => $"{Name}   |   {Folder[8..]}";
    }
    public sealed record ProfileDownload(string Text, string Filename);
    private const string RawBase = "https://raw.githubusercontent.com/jaakkopasanen/AutoEq/master/";
    private const string ApiBase = "https://api.github.com/repos/jaakkopasanen/AutoEq/contents/";
    private static readonly HttpClient Client = CreateClient();
    private static readonly Regex IndexRow = new(@"^\s*-\s+\[([^\]\r\n]+)\]\((.+)\)\s+by\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static HttpClient CreateClient()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        { Timeout = TimeSpan.FromSeconds(35) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("InfinityAudioEqualizerProWindows/1.0");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
    private static bool ValidPath(string path)
    {
        if (!path.StartsWith("results/", StringComparison.Ordinal) || path.Contains('\\') || path.Contains('?') || path.Contains('#')) return false;
        var parts = path.Split('/');
        return parts.Length >= 3 && parts.All(p => p.Length > 0 && p != "." && p != ".." && !p.Any(char.IsControl));
    }
    private static string EncodePath(string path) => string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
    private static async Task<string> DownloadString(Uri address, int maxBytes, CancellationToken ct)
    {
        if (address.Scheme != Uri.UriSchemeHttps ||
            !(address.Host is "raw.githubusercontent.com" or "api.github.com") ||
            !address.AbsolutePath.StartsWith(address.Host == "api.github.com" ? "/repos/jaakkopasanen/AutoEq/contents/" : "/jaakkopasanen/AutoEq/master/", StringComparison.Ordinal))
            throw new InvalidOperationException("공식 AutoEq 저장소 이외의 주소는 사용할 수 없습니다.");
        using var response = await Client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
            throw new InvalidOperationException("GitHub 다운로드 제한에 도달했습니다. 잠시 후 다시 시도하세요.");
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is long length && length > maxBytes)
            throw new InvalidDataException("AutoEq 데이터 크기가 허용 범위를 초과했습니다.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var bytes = new byte[8192];
        while (true)
        {
            int count = await stream.ReadAsync(bytes, ct).ConfigureAwait(false);
            if (count == 0) break;
            if (buffer.Length + count > maxBytes) throw new InvalidDataException("AutoEq 데이터 크기가 허용 범위를 초과했습니다.");
            buffer.Write(bytes, 0, count);
        }
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray()).TrimStart('\ufeff');
    }
    public static IReadOnlyList<Model> ParseIndex(string index)
    {
        var results = new List<Model>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in index.Split('\n'))
        {
            var match = IndexRow.Match(line.TrimEnd('\r'));
            if (!match.Success) continue;
            string name = match.Groups[1].Value.Trim();
            string link = Uri.UnescapeDataString(match.Groups[2].Value.Split('#')[0].Trim());
            if (!link.StartsWith("./", StringComparison.Ordinal)) continue;
            string folder = "results/" + link[2..];
            if (!ValidPath(folder) || name.Length == 0 || !seen.Add(folder)) continue;
            results.Add(new Model(name, folder));
        }
        return results;
    }
    public static async Task<IReadOnlyList<Model>> FetchIndexAsync(CancellationToken ct)
    {
        var text = await DownloadString(new Uri(RawBase + "results/INDEX.md"), 4_000_000, ct);
        var entries = ParseIndex(text);
        if (entries.Count == 0) throw new InvalidDataException("공식 AutoEq 모델 목록을 읽을 수 없습니다.");
        return entries;
    }
    public static async Task<ProfileDownload> FetchProfileAsync(Model model, CancellationToken ct)
    {
        if (!ValidPath(model.Folder)) throw new InvalidDataException("잘못된 모델 경로입니다.");
        string json = await DownloadString(new Uri(ApiBase + EncodePath(model.Folder)), 1_500_000, ct);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("AutoEq 모델 파일 목록 형식이 잘못되었습니다.");
        var candidates = new List<(string Name, string Path, int Priority)>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var kind) || kind.GetString() != "file" ||
                !item.TryGetProperty("name", out var nameElement) || !item.TryGetProperty("path", out var pathElement)) continue;
            string name = nameElement.GetString() ?? "", path = pathElement.GetString() ?? "";
            if (!name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || !ValidPath(path) ||
                !path.StartsWith(model.Folder + "/", StringComparison.Ordinal)) continue;
            int priority = name.Contains("ParametricEQ", StringComparison.OrdinalIgnoreCase) ? 0 :
                name.Contains("GraphicEQ", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
            if (priority < 2) candidates.Add((name, path, priority));
        }
        var selected = candidates.OrderBy(x => x.Priority).ThenBy(x => x.Name.Length).FirstOrDefault();
        if (string.IsNullOrEmpty(selected.Path)) throw new InvalidDataException("선택한 모델의 ParametricEQ / GraphicEQ 파일이 없습니다.");
        var text = await DownloadString(new Uri(RawBase + EncodePath(selected.Path)), 250_000, ct);
        return new ProfileDownload(text, selected.Name);
    }
}
