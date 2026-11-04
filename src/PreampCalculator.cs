// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Numerics;

namespace InfinityAudioEqualizerPro;

/// <summary>
/// Mirrors the legacy Python headroom estimate. This is a static analytical
/// estimate, never a dynamic compressor/limiter. Manual mode stays absolute.
/// </summary>
public static class PreampCalculator
{
    private static double GraphicDb(IReadOnlyList<(double F, double G)> points,double frequency)
    {
        if(points.Count==0)return 0;
        if(frequency<=points[0].F)return points[0].G;
        for(int i=1;i<points.Count;i++)
        {
            var before=points[i-1];var after=points[i];
            if(frequency>after.F)continue;
            if(after.F<=before.F)return after.G;
            double interpolation=Math.Log(frequency/before.F)/Math.Log(after.F/before.F);
            return before.G+(after.G-before.G)*interpolation;
        }
        return points[^1].G;
    }
    private static double BiquadDb(AutoFilter filter,double freq)
    {
        double f=filter.Frequency,gain=filter.Gain,q=filter.Q;
        if(!double.IsFinite(f)||!double.IsFinite(gain)||!double.IsFinite(q)||f<=0||q<=0)return 0;
        const double sr=96000;
        double a=Math.Pow(10,gain/40),w=2*Math.PI*Math.Min(f,sr*.49)/sr;
        double c=Math.Cos(w),sn=Math.Sin(w);
        double b0,b1,b2,a0,a1,a2;
        switch(filter.Type.ToUpperInvariant())
        {
            case "PK":case "PEQ":
            {
                double alpha=sn/(2*Math.Max(q,.05));
                b0=1+alpha*a;b1=-2*c;b2=1-alpha*a;
                a0=1+alpha/a;a1=-2*c;a2=1-alpha/a;break;
            }
            case "LS":case "LSC":case "HS":case "HSC":
            {
                double slope=filter.Type.EndsWith('C')?Math.Clamp(q,.1,2):1;
                double alpha=sn/2*Math.Sqrt(Math.Max(0,(a+1/a)*(1/slope-1)+2));
                double root=2*Math.Sqrt(a)*alpha;
                if(filter.Type.StartsWith("LS",StringComparison.OrdinalIgnoreCase))
                {
                    b0=a*((a+1)-(a-1)*c+root);b1=2*a*((a-1)-(a+1)*c);b2=a*((a+1)-(a-1)*c-root);
                    a0=(a+1)+(a-1)*c+root;a1=-2*((a-1)+(a+1)*c);a2=(a+1)+(a-1)*c-root;
                }
                else
                {
                    b0=a*((a+1)+(a-1)*c+root);b1=-2*a*((a-1)+(a+1)*c);b2=a*((a+1)+(a-1)*c-root);
                    a0=(a+1)-(a-1)*c+root;a1=2*((a-1)-(a+1)*c);a2=(a+1)-(a-1)*c-root;
                }
                break;
            }
            default:return 0;
        }
        var z=Complex.FromPolarCoordinates(1,-2*Math.PI*freq/sr);
        var numerator=b0+b1*z+b2*z*z;
        var denominator=a0+a1*z+a2*z*z;
        if(denominator.Magnitude<1e-12)return 0;
        return 20*Math.Log10(Math.Max(1e-12,(numerator/denominator).Magnitude));
    }
    // Diagnostic response of the *EQ stages only*; it does not imply the
    // compressor, Windows mixer, sound card or headphones have this response.
    // The same valid-filter selection as the APO renderer must be used.
    public static (double AutoEqDb, double UserEqDb, double CombinedDb) ResponseAt(DeviceProfile p, double hz)
    {
        if (!p.Enabled || !double.IsFinite(hz) || hz < 10 || hz > 24000)
            return (0,0,0);
        var valid = p.AutoEnabled
            ? p.AutoFilters.Where(f => f.Frequency >= 10 && f.Frequency <= 24000 &&
                double.IsFinite(f.Frequency) && double.IsFinite(f.Gain) && double.IsFinite(f.Q) &&
                f.Gain >= -40 && f.Gain <= 40 && f.Q >= .05 && f.Q <= 40 &&
                f.Type.ToUpperInvariant() is "PK" or "PEQ" or "LS" or "LSC" or "HS" or "HSC").ToList()
            : new List<AutoFilter>();
        var graphic = p.AutoEnabled && valid.Count == 0
            ? p.Graphic.Where(a => a.Length >= 2 && a[0] >= 10 && a[0] <= 24000 &&
                double.IsFinite(a[0]) && double.IsFinite(a[1]) && a[1] >= -40 && a[1] <= 40)
                .GroupBy(a => a[0]).Select(g => (F:g.Key,G:g.Last()[1]))
                .OrderBy(a => a.F).ToList()
            : new List<(double F,double G)>();
        var user = p.CustomEnabled
            ? ApoRenderer.Bands.Select((f,i) => (F:f, G:i < p.Custom.Count && double.IsFinite(p.Custom[i])
                ? Math.Clamp(p.Custom[i],-24,24) : 0d)).ToList()
            : new List<(double F,double G)>();
        double automatic = valid.Sum(f => BiquadDb(f,hz)) + GraphicDb(graphic,hz);
        double custom = GraphicDb(user,hz);
        return (automatic,custom,automatic+custom);
    }
    /// <summary>Read-only graph preview, matching Python's three saved-response curves.
    /// It intentionally ignores ON/OFF toggles and excludes preamp/spatial/compressor.</summary>
    public static (double AutoEqDb, double UserEqDb, double CombinedDb) PreviewResponseAt(DeviceProfile p, double hz)
    {
        if (!double.IsFinite(hz) || hz <= 0) return (0,0,0);
        double automatic = p.AutoFilters.Where(f => double.IsFinite(f.Frequency) && f.Frequency>0 &&
            double.IsFinite(f.Gain) && double.IsFinite(f.Q) && f.Q>0).Sum(f => BiquadDb(f,hz));
        var graphic = p.Graphic.Where(g => g.Length>=2 && double.IsFinite(g[0]) && g[0]>0 && double.IsFinite(g[1]))
            .OrderBy(g=>g[0]).Select(g=>(F:g[0],G:g[1])).ToList();
        automatic += GraphicDb(graphic,hz);
        var user = ApoRenderer.Bands.Select((f,i) => (F:f,G:i<p.Custom.Count && double.IsFinite(p.Custom[i]) ? p.Custom[i] : 0d)).ToList();
        double custom = GraphicDb(user,hz);
        return (automatic,custom,automatic+custom);
    }
    public static (double PeakDb, double PeakFrequencyHz) AnalyzePeak(DeviceProfile p)
    {
        double peak = double.NegativeInfinity, peakHz = 1000;
        for(int i=0;i<1536;i++)
        {
            double hz=20*Math.Pow(1000,i/1535d);
            double db=ResponseAt(p,hz).CombinedDb;
            if(double.IsFinite(db) && db>peak) {peak=db;peakHz=hz;}
        }
        return (double.IsFinite(peak)?peak:0,peakHz);
    }
    public static double PeakResponseDb(DeviceProfile p) => Math.Max(0,AnalyzePeak(p).PeakDb);
    public static (double Final,double Baseline,double Peak) Calculate(DeviceProfile p)
    {
        if(!p.Enabled)return (0,0,0);
        double user=double.IsFinite(p.ManualPreamp)?p.ManualPreamp:0;
        if(p.PreampMode=="manual")return (user,0,0);
        double source=p.AutoEnabled&&double.IsFinite(p.AutoPreamp)?Math.Min(0,p.AutoPreamp):0;
        if(p.PreampMode=="source")return (source+user,source,0);
        double peak=PeakResponseDb(p);
        double safety=peak>.01?Math.Min(0,-peak-.2):0;
        double baseline=Math.Min(source,safety);
        return (baseline+user,baseline,peak);
    }
}
