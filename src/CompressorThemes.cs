// C# port of Python compressor_themes.py (same five-band presets and normalized VST params).
using System.Globalization;
namespace InfinityAudioEqualizerPro;
public static class CompressorThemes
{
    public sealed record Band(double Cutoff, double Threshold, double Ratio, double Attack, double Release, double Knee, double Gain);
    public sealed record Shape(double Shift, double Mult, double Gain, double Attack, double Release, double Knee);
    public sealed record Theme(double BaseThreshold, double BaseRatio, double[] Cutoffs, Shape[] Shapes);
    private static readonly Dictionary<string, Theme> Themes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VOCAL_FOCUS"] = new Theme(-18, 1.55, new double[] { 140, 700, 3500, 8000, 20000 }, new Shape[] {
            new Shape(5, 0.78, 0, 45, 230, 6),
            new Shape(3, 0.88, 0.1, 34, 210, 8),
            new Shape(-1, 1.04, 0.25, 26, 180, 8),
            new Shape(4, 0.82, 0, 18, 150, 6),
            new Shape(7, 0.72, 0, 15, 140, 6),
        }),
        ["PUNCH"] = new Theme(-18, 1.55, new double[] { 160, 800, 4000, 8000, 20000 }, new Shape[] {
            new Shape(14, 0.65, 0, 50, 240, 6),
            new Shape(14, 0.65, 0, 40, 210, 6),
            new Shape(12, 0.7, 0.25, 48, 150, 6),
            new Shape(-7, 1.85, 0.9, 32, 92, 4),
            new Shape(12, 0.7, 0, 15, 130, 6),
        }),
        ["VOCAL_SMOOTH"] = new Theme(-20, 1.5, new double[] { 160, 800, 3800, 8000, 20000 }, new Shape[] {
            new Shape(6, 0.75, 0, 46, 250, 6),
            new Shape(3, 0.85, 0, 32, 230, 8),
            new Shape(0, 1.04, 0.1, 20, 210, 10),
            new Shape(1, 1.02, 0, 16, 170, 10),
            new Shape(5, 0.8, 0, 18, 160, 8),
        }),
        ["AMBIENCE"] = new Theme(-17, 1.18, new double[] { 160, 850, 4000, 8500, 20000 }, new Shape[] {
            new Shape(8, 0.82, 0, 50, 290, 8),
            new Shape(7, 0.85, 0, 48, 280, 8),
            new Shape(6, 0.88, 0, 44, 260, 8),
            new Shape(5, 0.94, 0.1, 35, 230, 8),
            new Shape(6, 0.86, 0.05, 30, 230, 8),
        }),
        ["OPEN"] = new Theme(-14, 1.2, new double[] { 120, 700, 3500, 8500, 20000 }, new Shape[] {
            new Shape(7, 0.88, 0, 55, 240, 6),
            new Shape(6, 0.88, 0, 48, 220, 6),
            new Shape(6, 0.92, 0, 42, 200, 6),
            new Shape(7, 0.94, 0, 35, 180, 6),
            new Shape(8, 0.88, 0, 30, 170, 6),
        }),
        ["RHYTHM"] = new Theme(-21, 1.7, new double[] { 140, 600, 3500, 8000, 20000 }, new Shape[] {
            new Shape(1, 1.06, 0.1, 38, 200, 6),
            new Shape(2, 1.02, 0.1, 34, 210, 8),
            new Shape(5, 0.82, 0, 34, 220, 8),
            new Shape(6, 0.82, 0, 24, 170, 6),
            new Shape(8, 0.72, 0, 20, 150, 6),
        }),
        ["SPATIAL_DETAIL"] = new Theme(-21, 1.32, new double[] { 160, 850, 4000, 8500, 20000 }, new Shape[] {
            new Shape(12, 0.78, 0, 50, 240, 8),
            new Shape(11, 0.8, 0, 45, 220, 8),
            new Shape(10, 0.82, 0, 38, 190, 8),
            new Shape(-2, 1.18, 0.45, 25, 150, 6),
            new Shape(1, 1.06, 0.2, 20, 135, 6),
        }),
        ["PREMIUM_BALANCE"] = new Theme(-20, 1.3, new double[] { 160, 850, 4000, 8500, 20000 }, new Shape[] {
            new Shape(12, 0.83, 0, 52, 230, 8),
            new Shape(11, 0.83, 0, 48, 220, 8),
            new Shape(11, 0.83, 0, 42, 205, 8),
            new Shape(-2, 1.04, 0.25, 30, 165, 6),
            new Shape(8, 0.84, 0, 26, 180, 6),
        }),
    };
    // Public names match the original Python edition. JSON/APO retain stable IDs.
    private static readonly Dictionary<string,string> Names=new(StringComparer.OrdinalIgnoreCase)
    {
        ["VOCAL_FOCUS"]="보컬 단단함",["PUNCH"]="타격감",["VOCAL_SMOOTH"]="부드러운 보컬",
        ["AMBIENCE"]="은은한 여운",["OPEN"]="자연스러운 개방감",
        ["RHYTHM"]="리듬 밀도",["SPATIAL_DETAIL"]="입체적 디테일",
        ["PREMIUM_BALANCE"]="프리미엄 밸런스",["CUSTOM"]="사용자 설정"
    };
    public static string DisplayName(string id)=>Names.TryGetValue(id,out string? label)?label:Names["PREMIUM_BALANCE"];
    public static string IdFromDisplayName(string? name)=>Names.FirstOrDefault(p=>p.Value==name).Key ?? "PREMIUM_BALANCE";
    public static string[] ThemeIds => Themes.Keys.OrderBy(x=>x).ToArray();
    private static double Clamp(double n,double min,double max) => Math.Clamp(n,min,max);
    private static double Reduction(double r,double t)=> Math.Max(0,48-t)*(1-1/r);

    public static Band[] MakeBands(string name,int level)
    {
        if (!Themes.TryGetValue(name,out Theme? t)) t=Themes["PREMIUM_BALANCE"];
        level=Math.Clamp(level,1,12);
        int original=Math.Min(8,level);
        double strength=original<=4 ? .25+(original-1)*.25 : 1+(original-4)*.20;
        var result = new Band[5];
        for(int i=0;i<5;i++)
        {
            var shape=t.Shapes[i];
            double threshold=Clamp(Clamp(t.BaseThreshold+shape.Shift,-48,0)-(strength-1)*4,-48,0);
            double ratio=Clamp(1+(Clamp(t.BaseRatio*shape.Mult,1,6)-1)*strength,1,6);
            double post=Clamp(Clamp(shape.Gain,-6,6)*strength,-6,3);
            double attack=shape.Attack, release=shape.Release;
            if(level>8 && (i>0?t.Cutoffs[i-1]:0)>=4000)
            {
                double extra=(level-8)/4d;
                double targetR=Math.Min(6,1+(ratio-1)*(1+extra*.12));
                double targetT=Math.Max(-48,threshold-extra*.4);
                double budget=extra*((i>0?t.Cutoffs[i-1]:0)>=8000?.20:.35);
                double before=Reduction(ratio,threshold),lo=0,hi=1;
                for(int j=0;j<14;j++)
                {
                    double mix=(lo+hi)/2;
                    if(Reduction(ratio+(targetR-ratio)*mix,threshold+(targetT-threshold)*mix)-before<=budget)lo=mix;
                    else hi=mix;
                }
                ratio+=(targetR-ratio)*lo;
                threshold+=(targetT-threshold)*lo;
                attack=Math.Min(100,attack+extra*4);
                release=Math.Max(40,release-extra*12);
            }
            result[i]=new Band(t.Cutoffs[i],threshold,ratio,attack,release,shape.Knee,post);
        }
        return result;
    }

    public static string Parameters(string theme,int strength) => Parameters(MakeBands(theme,strength));
    public static string Parameters(IReadOnlyList<Band> bands)
    {
        if(bands.Count!=5) throw new ArgumentException("5개 컴프레서 대역이 필요합니다.");
        var args=new List<string>();
        string F(double n)=>n.ToString("0.0000000",CultureInfo.InvariantCulture);
        for(int i=0;i<5;i++)
        {
            var b=bands[i];
            int index=i+1;
            args.Add("T"+index+" "+F(Clamp((b.Threshold+48)/48,0,1)));
            args.Add("R"+index+" "+F(Clamp((b.Ratio-1)/5,0,1)));
            args.Add("A"+index+" "+F(Clamp((b.Attack-2)/98,0,1)));
            args.Add("D"+index+" "+F(Clamp((b.Release-40)/460,0,1)));
            args.Add("K"+index+" "+F(Clamp(b.Knee/12,0,1)));
            args.Add("G"+index+" "+F(Clamp((b.Gain+6)/9,0,1)));
        }
        for(int i=0;i<4;i++) args.Add("X"+(i+1)+" "+F(Clamp((bands[i].Cutoff-30)/19970,0,1)));
        return string.Join(' ',args);
    }
}
