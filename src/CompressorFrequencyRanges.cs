// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
// Safe five-band crossover boundaries for the compressor editor.
namespace InfinityAudioEqualizerPro;

public static class CompressorFrequencyRanges
{
    // The fifth band's upper edge is the fixed end of the audio range.
    // The first four ranges overlap intentionally; adjacent bands dynamically
    // restrict each other so invalid ordering is impossible through the UI.
    public static (decimal Min, decimal Max) BandRange(int index) => index switch
    {
        0 => (30m, 500m),
        1 => (60m, 2500m),
        2 => (300m, 8000m),
        3 => (1000m, 16000m),
        4 => (20000m, 20000m),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    // Existing imported or saved presets may predate the editor's range checks.
    // Correct their crossover points once before presenting editable controls.
    public static decimal[] Normalize(IReadOnlyList<double> values)
    {
        if (values.Count != 5) throw new ArgumentException("5개 대역이 필요합니다.", nameof(values));
        var result = new decimal[5];
        for (int i = 0; i < 5; i++)
        {
            var (min, max) = BandRange(i);
            double value = double.IsFinite(values[i]) ? values[i] : (double)min;
            result[i] = (decimal)Math.Clamp(value, (double)min, (double)max);
            result[i] = decimal.Round(result[i], 0, MidpointRounding.AwayFromZero);
        }
        result[4] = 20000m;
        for (int i = 1; i < 5; i++)
            result[i] = Math.Max(result[i], result[i - 1] + 1m);
        for (int i = 3; i >= 0; i--)
            result[i] = Math.Min(result[i], result[i + 1] - 1m);
        return result;
    }

    // Current permitted values for one tab: both its own natural range and the
    // two adjacent crossover values determine its upper/lower endpoints.
    public static (decimal Min, decimal Max) Allowed(IReadOnlyList<decimal> values, int index)
    {
        if (values.Count != 5) throw new ArgumentException("5개 대역이 필요합니다.", nameof(values));
        var (min, max) = BandRange(index);
        if (index > 0) min = Math.Max(min, values[index - 1] + 1m);
        if (index < 4) max = Math.Min(max, values[index + 1] - 1m);
        // Defensive fallback if a malformed external preset is still being loaded.
        if (min > max) min = max = Math.Clamp(values[index], BandRange(index).Min, BandRange(index).Max);
        return (min, max);
    }
}
