// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
namespace InfinityAudioEqualizerPro;

/// <summary>
/// Transfer audio effects without disclosing or replacing the recipient's AutoEQ
/// measurements, endpoint identities or personal data.
/// </summary>
public static class EffectTransfer
{
    public const string CurrentFormat = "InfinityAudioEqualizerPro-Effects-v2";
    public const string LegacyFormat = "InfinityAudioEqualizerPro-Effects-v1";
    private sealed class Envelope
    {
        [JsonPropertyName("format")] public string Format { get; set; } = CurrentFormat;
        [JsonPropertyName("profile")] public DeviceProfile? Profile { get; set; }
    }
    public static string ExportShare(DeviceProfile source)
    {
        var root=JsonSerializer.SerializeToNode(new Envelope { Profile=CloneSafe(source) }, SettingsStore.JsonOptions)!.AsObject();
        var profile=root["profile"]!.AsObject();
        // These keys are intentionally absent rather than serialized as empty values.
        foreach(var key in new[]{"source","auto_enabled","auto_filters","graphic","auto_preamp"})
            profile.Remove(key);
        return root.ToJsonString(SettingsStore.JsonOptions);
    }

    public static bool IsShare(string content)
    {
        try
        {
            using var json = JsonDocument.Parse(content);
            return json.RootElement.TryGetProperty("format", out var x) &&
                (x.GetString() == CurrentFormat || x.GetString() == LegacyFormat);
        }
        catch (JsonException) { return false; }
    }

    public static DeviceProfile ImportShare(string content)
    {
        var envelope = JsonSerializer.Deserialize<Envelope>(content, SettingsStore.JsonOptions);
        if ((envelope?.Format != CurrentFormat && envelope?.Format != LegacyFormat) || envelope.Profile is null)
            throw new InvalidDataException("공유용 음향 설정 파일이 아닙니다.");
        // Also sanitize legacy v1 files that contained embedded AutoEQ measurements.
        return CloneSafe(envelope.Profile);
    }

    /// <summary>Deep-copy only transferable effect parameters, excluding all AutoEQ.</summary>
    public static DeviceProfile CloneSafe(DeviceProfile p)
    {
        return new DeviceProfile
        {
            Name="공유 음향 설정", Enabled=p.Enabled,
            AutoEnabled=false, AutoFilters=[], Graphic=[], AutoPreamp=0, Source="",
            CustomEnabled=p.CustomEnabled, Custom=p.Custom.ToList(),
            ManualPreamp=p.ManualPreamp,
            // Source/automatic depends on AutoEQ, which is intentionally not shared.
            PreampMode="manual",
            LowLatency=p.LowLatency,
            AmbienceEnabled=p.AmbienceEnabled, AmbienceStrength=p.AmbienceStrength,
            AmbienceDecay=p.AmbienceDecay, AmbienceRate=p.AmbienceRate,
            AmbienceRateAuto=p.AmbienceRateAuto,
            StereoWidthEnabled=p.StereoWidthEnabled, StereoWidth=p.StereoWidth,
            SurroundEnabled=p.SurroundEnabled, SurroundMode=p.SurroundMode,
            SurroundStrength=p.SurroundStrength, SurroundCenter=p.SurroundCenter,
            SurroundSpread=p.SurroundSpread,
            Surround3Enabled=p.Surround3Enabled,
            Surround3Depth=p.Surround3Depth, Surround3Rear=p.Surround3Rear,
            Surround3Front=p.Surround3Front, Surround3Separation=p.Surround3Separation,
            SurroundBinaural=p.SurroundBinaural, SurroundStable=p.SurroundStable,
            SurroundBalanced=p.SurroundBalanced,
            SurroundBalanceStability=p.SurroundBalanceStability,
            SurroundBalanceSpace=p.SurroundBalanceSpace,
            SurroundBalanceDepth=p.SurroundBalanceDepth,
            CompressorEnabled=p.CompressorEnabled,
            CompressorTheme=p.CompressorTheme,
            CompressorStrength=p.CompressorStrength,
            CompressorCustomBands=p.CompressorCustomBands.Select(x=>CompressorBand.From(x.ToRuntime())).ToList(),
            Extra=null
        };
    }

    /// <summary>
    /// Apply transferable sound settings to the currently selected endpoint without
    /// discarding its AutoEQ identity or measured frequency response.
    /// </summary>
    public static DeviceProfile MergeIntoDevice(DeviceProfile current, DeviceProfile shared)
    {
        var result=CloneSafe(shared);
        result.Name=current.Name;
        result.AutoEnabled=current.AutoEnabled;
        result.Source=current.Source;
        result.AutoPreamp=current.AutoPreamp;
        result.AutoFilters=current.AutoFilters.Select(x=>new AutoFilter{
            Type=x.Type,Frequency=x.Frequency,Gain=x.Gain,Q=x.Q}).ToList();
        result.Graphic=current.Graphic.Select(x=>x.ToArray()).ToList();
        // Preserve the recipient's selected AutoEQ-dependent preamp mode.
        // Without a recipient AutoEQ, manual preamp from the share is safe.
        result.PreampMode=(current.PreampMode is "source" or "automatic") && current.AutoEnabled
            ? current.PreampMode : "manual";
        return result;
    }
}
