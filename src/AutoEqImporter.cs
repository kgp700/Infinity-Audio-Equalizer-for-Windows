// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Globalization;
using System.Text.RegularExpressions;
namespace InfinityAudioEqualizerPro;
public static class AutoEqImporter
{
    private static readonly Regex Preamp = new(@"^Preamp:\s*([-+]?\d+(?:\.\d+)?)\s*dB\s*$", RegexOptions.IgnoreCase);
    private static readonly Regex Filter = new(@"^Filter\s*(?:\d+)?\s*:\s*(ON|OFF)\s+(PK|PEQ|LS|LSC|HS|HSC)\s+Fc\s+([\d.]+)\s*Hz\s+Gain\s+([-+\d.]+)\s*dB(?:\s+Q\s+([\d.]+))?", RegexOptions.IgnoreCase);
    public static void Import(string text, DeviceProfile profile, string source)
    {
        var filters = new List<AutoFilter>(); var graphic = new List<double[]>(); double preamp = 0;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim(); if (line.Length == 0 || line.StartsWith('#')) continue;
            var p = Preamp.Match(line);
            if (p.Success) { preamp = Parse(p.Groups[1].Value); continue; }
            var f = Filter.Match(line);
            if (f.Success && f.Groups[1].Value.Equals("ON",StringComparison.OrdinalIgnoreCase))
            {
                var kind=f.Groups[2].Value.ToUpperInvariant().Replace("PEQ","PK");
                double freq=Parse(f.Groups[3].Value), gain=Parse(f.Groups[4].Value);
                double q=f.Groups[5].Success?Parse(f.Groups[5].Value):.707;
                if (freq is >=10 and <=24000 && gain is >=-40 and <=40 && q is >=.05 and <=40)
                    filters.Add(new AutoFilter {Type=kind,Frequency=freq,Gain=gain,Q=q});
            }
            if (line.StartsWith("GraphicEQ:",StringComparison.OrdinalIgnoreCase))
                foreach (string pair in line[(line.IndexOf(':')+1)..].Split(';'))
                {
                    var terms=pair.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
                    if (terms.Length != 2 || !double.TryParse(terms[0],NumberStyles.Float,CultureInfo.InvariantCulture,out double freq) ||
                        !double.TryParse(terms[1],NumberStyles.Float,CultureInfo.InvariantCulture,out double gain)) continue;
                    if (freq is >=10 and <=24000 && gain is >=-40 and <=40) graphic.Add([freq,gain]);
                }
        }
        if (filters.Count==0 && graphic.Count==0) throw new InvalidDataException("지원되는 AutoEQ PEQ 또는 GraphicEQ 데이터가 없습니다.");
        // As in Python engine: parametric and graphic in one export are alternatives, not additive.
        if (filters.Count>0) graphic.Clear();
        profile.AutoFilters=filters; profile.Graphic=graphic; profile.AutoPreamp=preamp; profile.Source=source; profile.AutoEnabled=true;
        // A downloaded EQ is a filter preset, NOT a volume or preamp preset.
        // Preserve the user's explicit preamp ON/OFF, mode and gain.  The
        // source-supplied preamp is stored for opt-in use in Source/Automatic
        // modes only; manual gain remains unchanged across repeated imports.
        // Do not change compressor, stage or custom 31-band settings either.
    }
    private static double Parse(string number) => double.Parse(number,CultureInfo.InvariantCulture);
}
