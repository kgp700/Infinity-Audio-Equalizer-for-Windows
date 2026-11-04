// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Text.Json;
using System.Text.Json.Serialization;
namespace InfinityAudioEqualizerPro;

public sealed class AppSettings
{
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("profiles")] public Dictionary<string, DeviceProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    [JsonPropertyName("startup")] public bool Startup { get; set; }
    [JsonPropertyName("global_enabled")] public bool GlobalEnabled { get; set; } = false;
    // Safe default for both new and previously saved migration-preview settings.
    [JsonPropertyName("bypass_all_effects")] public bool BypassAllEffects { get; set; } = true;
    [JsonPropertyName("apo_config")] public string ApoConfig { get; set; } = "";
    [JsonPropertyName("xt_install_dir")] public string XtInstallDir { get; set; } = "";
    [JsonPropertyName("default_id")] public string DefaultId { get; set; } = "";
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
public sealed class DeviceProfile
{
    [JsonPropertyName("name")] public string Name { get; set; } = "새 장치";
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("auto_enabled")] public bool AutoEnabled { get; set; } = false;
    [JsonPropertyName("custom_enabled")] public bool CustomEnabled { get; set; } = true;
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("auto_filters")] public List<AutoFilter> AutoFilters { get; set; } = [];
    [JsonPropertyName("graphic")] public List<double[]> Graphic { get; set; } = [];
    [JsonPropertyName("custom")] public List<double> Custom { get; set; } = Enumerable.Repeat(0d,31).ToList();
    [JsonPropertyName("manual_preamp")] public double ManualPreamp { get; set; } = -5;
    [JsonPropertyName("auto_preamp")] public double AutoPreamp { get; set; }
    [JsonPropertyName("preamp_mode")] public string PreampMode { get; set; } = "manual";
    [JsonPropertyName("low_latency")] public bool LowLatency { get; set; }
    [JsonPropertyName("ambience_enabled")] public bool AmbienceEnabled { get; set; }
    [JsonPropertyName("stereo_width_enabled")] public bool StereoWidthEnabled { get; set; }
    [JsonPropertyName("stereo_width")] public int StereoWidth { get; set; } = 30;
    [JsonPropertyName("surround_mode")] public string SurroundMode { get; set; } = "cinema";
    [JsonPropertyName("surround_strength")] public int SurroundStrength { get; set; } = 45;
    [JsonPropertyName("surround_center")] public int SurroundCenter { get; set; } = 80;
    [JsonPropertyName("surround_spread")] public int SurroundSpread { get; set; } = 45;
    [JsonPropertyName("ambience_strength")] public int AmbienceStrength { get; set; } = 3;
    [JsonPropertyName("ambience_decay")] public int AmbienceDecay { get; set; } = 1000;
    [JsonPropertyName("ambience_rate")] public int AmbienceRate { get; set; } = 48000;
    // Default to the current Windows render endpoint's mix format, as the Python version does.
    [JsonPropertyName("ambience_rate_auto")] public bool AmbienceRateAuto { get; set; } = true;
    // A/B listening mode must never become a stored preset.
    [JsonIgnore] public string DepthComparisonMode { get; set; } = "";
    // Temporary echo diagnostic, never persisted or exported as an audio preset.
    [JsonIgnore] public bool AmbienceDiagnostic { get; set; }

    [JsonPropertyName("surround_enabled")] public bool SurroundEnabled { get; set; }
    [JsonPropertyName("surround3_enabled")] public bool Surround3Enabled { get; set; } = true;
    [JsonPropertyName("surround_binaural")] public bool SurroundBinaural { get; set; } = true;
    [JsonPropertyName("surround_stable")] public bool SurroundStable { get; set; }
    [JsonPropertyName("surround_balanced")] public bool SurroundBalanced { get; set; }
    [JsonPropertyName("surround_balance_stability")] public int SurroundBalanceStability { get; set; } = 65;
    [JsonPropertyName("surround_balance_space")] public int SurroundBalanceSpace { get; set; } = 80;
    [JsonPropertyName("surround_balance_depth")] public int SurroundBalanceDepth { get; set; } = 55;
    [JsonPropertyName("surround3_depth")] public int Surround3Depth { get; set; } = 80;
    [JsonPropertyName("surround3_rear")] public int Surround3Rear { get; set; } = 75;
    [JsonPropertyName("surround3_front")] public int Surround3Front { get; set; } = 75;
    [JsonPropertyName("surround3_separation")] public int Surround3Separation { get; set; } = 80;
    [JsonPropertyName("compressor_enabled")] public bool CompressorEnabled { get; set; }
    [JsonPropertyName("compressor_custom_bands")] public List<CompressorBand> CompressorCustomBands { get; set; } = [];
    [JsonPropertyName("compressor_theme")] public string CompressorTheme { get; set; } = "OPEN";
    [JsonPropertyName("compressor_strength")] public int CompressorStrength { get; set; } = 12;
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
public sealed class AutoFilter
{
    [JsonPropertyName("type")] public string Type { get; set; } = "PK";
    [JsonPropertyName("freq")] public double Frequency { get; set; }
    [JsonPropertyName("gain")] public double Gain { get; set; }
    [JsonPropertyName("q")] public double Q { get; set; } = .707;
}
public sealed record AudioDevice(string Id, string Name, string Guid);

public sealed class CompressorBand
{
    [JsonPropertyName("cutoff")] public double Cutoff { get; set; }
    [JsonPropertyName("threshold")] public double Threshold { get; set; }
    [JsonPropertyName("ratio")] public double Ratio { get; set; }
    [JsonPropertyName("attack")] public double Attack { get; set; }
    [JsonPropertyName("release")] public double Release { get; set; }
    [JsonPropertyName("knee")] public double Knee { get; set; }
    [JsonPropertyName("gain")] public double Gain { get; set; }
    public static CompressorBand From(CompressorThemes.Band b) => new() { Cutoff=b.Cutoff,Threshold=b.Threshold,Ratio=b.Ratio,Attack=b.Attack,Release=b.Release,Knee=b.Knee,Gain=b.Gain};
    public CompressorThemes.Band ToRuntime() => new(Cutoff,Threshold,Ratio,Attack,Release,Knee,Gain);
}
