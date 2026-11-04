// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace InfinityAudioEqualizerPro;

/// <summary>
/// Generates only the effects explicitly enabled in the C# application.
/// Neutral/bypass output contains NO DSP commands (including Preamp or Copy).
/// </summary>
public static class ApoRenderer
{
    public static readonly double[] Bands = [20,25,31.5,40,50,63,80,100,125,160,200,250,315,400,500,630,800,1000,1250,1600,2000,2500,3150,4000,5000,6300,8000,10000,12500,16000,20000];
    private static string F(double n) => n.ToString("0.####", CultureInfo.InvariantCulture);
    private static bool Nonzero(double value) => double.IsFinite(value) && Math.Abs(value) > 0.00001;

    public static string Render(AppSettings state, string? configDirectory = null)
    {
        var sb = new StringBuilder("# Infinity Audio Equalizer Pro for Windows - C# generated settings.\n");
        if (state.BypassAllEffects || !state.GlobalEnabled)
        {
            sb.Append("# Original sound: all C# processing bypassed. No APO DSP commands.\n");
            return sb.ToString();
        }
        foreach (var pair in state.Profiles.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var id = Regex.Match(pair.Key, @"\{[0-9a-fA-F-]{36}\}\s*$");
            if (!id.Success) continue;
            var p = pair.Value;
            if (!p.Enabled) continue;
            string? pluginPath = configDirectory is null ? null : Path.Combine(configDirectory, ApoBridge.Folder, "InfinityCompressor.vst3");
            var lines = RenderProfile(p, pluginPath);
            if (lines.Count == 0) continue;
            sb.Append("\n# ").Append(p.Name.Replace('\n', ' ').Replace('\r', ' '))
              .Append("\nDevice: ").Append(id.Value.Trim()).Append('\n');
            foreach (var line in lines) sb.Append(line).Append('\n');
        }
        sb.Append("\nDevice: all\n# Reset device targeting.\n");
        return sb.ToString();
    }

    public static List<string> RenderProfile(DeviceProfile p, string? pluginPath = null)
    {
        // Diagnostic echo test replaces the normal chain for this one endpoint.
        // It never changes the saved profile's EQ, compressor or reverb settings.
        if (p.AmbienceDiagnostic && !p.LowLatency)
            return ["Convolution: " + ImpulseResponseService.DiagnosticAmbienceFile(p)];
        if (p.DepthComparisonMode is "front" or "rear" && !p.LowLatency)
            return HrtfStageService.ComparisonApoLines(p);
        var lines = new List<string>();
        double preamp=PreampCalculator.Calculate(p).Final;
        if (double.IsFinite(preamp) && Nonzero(preamp))
            lines.Add("Preamp: " + F(Math.Clamp(preamp, -60, 30)) + " dB");

        // AutoEQ exports can contain alternate PEQ and GraphicEQ descriptions.
        // Never apply both alternatives at once.
        var validAutoFilters = p.AutoEnabled
            ? p.AutoFilters.Where(IsValidFilter).ToList()
            : new List<AutoFilter>();
        var autoGraphic = (p.AutoEnabled && validAutoFilters.Count == 0)
            ? ValidGraphic(p.Graphic)
            : new List<(double Frequency, double Gain)>();
        var custom = p.CustomEnabled
            ? p.Custom.Concat(Enumerable.Repeat(0d, Bands.Length)).Take(Bands.Length)
                .Select(x => double.IsFinite(x) ? Math.Clamp(x, -24, 24) : 0).ToArray()
            : new double[Bands.Length];
        var hasCustom = custom.Any(Nonzero);

        // Follow the proven Python engine: AutoEQ graphic and the custom 31-band
        // curve remain separate stages. Combining anchor curves changes the
        // interpolated frequency response and did not reproduce the baseline.
        if (autoGraphic.Count > 0)
            lines.Add("GraphicEQ: " + string.Join("; ", autoGraphic.Select(x => F(x.Frequency) + " " + F(x.Gain))));
        foreach (var f in validAutoFilters)
        {
            string kind = f.Type.ToUpperInvariant().Replace("PEQ", "PK");
            if (kind is "LS" or "HS") kind += "C";
            lines.Add("Filter: ON " + kind + " Fc " + F(f.Frequency) + " Hz Gain " + F(f.Gain) + " dB Q " + F(f.Q));
        }
        if (hasCustom)
            lines.Add("GraphicEQ: " + string.Join("; ", Bands.Select((hz, i) => F(hz) + " " + F(custom[i]))));
        if (p.CompressorEnabled && !p.LowLatency && !string.IsNullOrWhiteSpace(pluginPath))
            lines.Add("VSTPlugin: Library \"" + pluginPath.Replace('"', ' ') + "\" Input Stereo Output Stereo "
                + (p.CompressorTheme == "CUSTOM" && p.CompressorCustomBands.Count == 5
                    ? CompressorThemes.Parameters(p.CompressorCustomBands.Select(b => b.ToRuntime()).ToArray())
                    : CompressorThemes.Parameters(p.CompressorTheme, p.CompressorStrength)));
        if (p.StereoWidthEnabled && p.StereoWidth > 0)
        {
            double cross = .85 * Math.Pow(Math.Clamp(p.StereoWidth, 0, 100) / 100.0, 1.3);
            double own = 1 + cross;
            lines.Add("Copy: L=" + F(own) + "*L+-" + F(cross) + "*R R=" + F(own) + "*R+-" + F(cross) + "*L");
        }
        if (!p.LowLatency && p.SurroundEnabled && p.SurroundStrength>0 && pluginPath is not null)
        {
            if (p.SurroundBinaural)
                lines.AddRange(BinauralStageService.ApoLines(p));
            else
            {
                var (a,b)=ImpulseResponseService.SurroundMatrix(p);
                lines.Add("Copy: L="+F(a)+"*L+-"+F(b)+"*R R="+F(a)+"*R+-"+F(b)+"*L");
                lines.Add("Convolution: "+ImpulseResponseService.SurroundFile(p));
            }
            if (p.Surround3Enabled)
                lines.AddRange(HrtfStageService.ApoLines(p));
        }
        if (!p.LowLatency && p.AmbienceEnabled && p.AmbienceStrength>0 && pluginPath is not null)
            lines.Add("Convolution: "+ImpulseResponseService.AmbienceFile(p));
        // HRTF/Stage3 still requires further source-to-source DSP porting.
        // Advanced settings remain in the JSON profile but are not executed.
        return lines;
    }

    private static bool IsValidFilter(AutoFilter f) =>
        f.Frequency >= 10 && f.Frequency <= 24000 &&
        double.IsFinite(f.Frequency) && double.IsFinite(f.Gain) && double.IsFinite(f.Q) &&
        f.Gain >= -40 && f.Gain <= 40 && f.Q >= .05 && f.Q <= 40 &&
        f.Type.ToUpperInvariant() is "PK" or "PEQ" or "LS" or "LSC" or "HS" or "HSC";

    private static List<(double Frequency, double Gain)> ValidGraphic(List<double[]> source) =>
        source.Where(x => x.Length >= 2 && double.IsFinite(x[0]) && double.IsFinite(x[1]) &&
            x[0] >= 10 && x[0] <= 24000 && x[1] >= -40 && x[1] <= 40)
            .GroupBy(x => x[0]).OrderBy(g => g.Key)
            .Select(g => (Frequency: g.Key, Gain: g.Last()[1])).ToList();

    private static List<(double Frequency, double Gain)> CombineGraphic(
        List<(double Frequency, double Gain)> auto, double[] custom, bool hasCustom)
    {
        if (auto.Count == 0 && !hasCustom) return [];
        if (auto.Count == 0)
            return Bands.Select((hz, i) => (Frequency: hz, Gain: custom[i])).ToList();
        if (!hasCustom) return auto;
        // Piecewise logarithmic interpolation corresponds to GraphicEQ anchor spacing.
        var anchors = auto.Select(x => x.Frequency).Concat(Bands).Distinct().OrderBy(x => x);
        var customPoints = Bands.Select((hz, i) => (Frequency: hz, Gain: custom[i])).ToList();
        return anchors.Select(hz => (Frequency: hz,
            Gain: Math.Clamp(Interpolate(auto, hz) + Interpolate(customPoints, hz), -40, 40))).ToList();
    }

    private static double Interpolate(List<(double Frequency, double Gain)> points, double hz)
    {
        if (hz <= points[0].Frequency) return points[0].Gain;
        if (hz >= points[^1].Frequency) return points[^1].Gain;
        for (int i = 1; i < points.Count; i++)
        {
            if (hz > points[i].Frequency) continue;
            double lo = Math.Log(points[i - 1].Frequency);
            double hi = Math.Log(points[i].Frequency);
            double t = (Math.Log(hz) - lo) / (hi - lo);
            return points[i - 1].Gain + (points[i].Gain - points[i - 1].Gain) * t;
        }
        return points[^1].Gain;
    }
}
