// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
// Port of the measured-HRIR front/rear wet-path renderer from surround_stage3.py.
// All source channels preserve unity dry. This is *not* a substitute for
// binaural_stage.py, which is a separate surround processing mode.
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace InfinityAudioEqualizerPro;

public static class HrtfStageService
{
    private const int Version = 6;
    private const int SourceRate = 44100;
    private static readonly object CacheLock = new();
    private static Dictionary<string, (double[] Near, double[] Far)>? _raw;
    private static readonly Dictionary<string, (double[] FrontNear, double[] FrontFar, double[] RearNear, double[] RearFar)> _basis = new();

    private static int Rate(DeviceProfile p) => new[] {44100,48000,88200,96000,176400,192000}.Contains(p.AmbienceRate) ? p.AmbienceRate : 48000;
    private static string Mode(DeviceProfile p) => p.SurroundMode is "cinema" or "stage" ? p.SurroundMode : "natural";
    private static double Percent(int value) => Math.Clamp(value,0,100)/100d;
    private static string Tag(DeviceProfile p) =>
        $"infinity_stage3_v{Version}_{Mode(p)}_{Rate(p)}_{Math.Clamp(p.SurroundStrength,0,100)}_{Math.Clamp(p.Surround3Depth,0,100)}_{Math.Clamp(p.Surround3Rear,0,100)}_{Math.Clamp(p.Surround3Front,0,100)}_{Math.Clamp(p.Surround3Separation,0,100)}_normal";
    public static (string Left,string Right) Files(DeviceProfile p) => (Tag(p)+"_left.wav",Tag(p)+"_right.wav");

    public static string ComparisonFile(DeviceProfile p) =>
        $"infinity_stage3_v{Version}_compare_{Mode(p)}_{Rate(p)}_{p.DepthComparisonMode}.wav";

    public static List<string> ComparisonApoLines(DeviceProfile p)
    {
        if (p.DepthComparisonMode is not ("front" or "rear")) return [];
        // Only one centred virtual source; no user's EQ, width or compressor in the A/B path.
        return [
            "Preamp: 0 dB",
            "Copy: XLA=0.5*L+0.5*R XLB=0.5*L+0.5*R",
            "Channel: XLA XLB",
            "Convolution: " + ComparisonFile(p),
            "Channel: L R",
            "Copy: L=XLA R=XLB"
        ];
    }

    private static (int Rate, (double[] L, double[] R) Channels) MakeComparison(DeviceProfile p)
    {
        var source = ReadData();
        string direction = p.DepthComparisonMode == "front" ? "front" : "rear_" + Mode(p);
        if (!source.TryGetValue(direction, out var raw))
            throw new InvalidDataException("선택한 공간 음향 비교 데이터가 없습니다.");
        int rate = Rate(p);
        double[] near = Resample(raw.Near, rate);
        double[] far = Resample(raw.Far, rate);
        var left = near.Zip(far, (a, b) => .60 * a + .40 * b).ToArray();
        var right = near.Zip(far, (a, b) => .40 * a + .60 * b).ToArray();
        // Preserve pinna cues: do not apply the regular 8.5kHz wet-path low-pass.
        double normalizer = Math.Min(100d, 1d / PeakResponse(left, right));
        for (int i = 0; i < left.Length; ++i)
        {
            left[i] *= normalizer;
            right[i] *= normalizer;
        }
        return (rate, (left, right));
    }

    public static List<string> ApoLines(DeviceProfile p)
    {
        var result = new List<string>();
        if (!p.Surround3Enabled || p.LowLatency || !p.SurroundEnabled || p.SurroundStrength<=0 ||
            p.Surround3Depth<=0 || p.Surround3Separation<=0 ||
            (p.Surround3Front<=0 && p.Surround3Rear<=0)) return result;
        var (left,right)=Files(p);
        result.Add("# Infinity Audio Equalizer Pro Surround Stage3: measured front/rear HRTF, centre bypass");
        result.Add("Copy: XSL=0.5*L+-0.5*R XSR=0.5*R+-0.5*L");
        result.Add("Copy: XLA=XSL XLB=XSL XRA=XSR XRB=XSR");
        result.Add("Channel: XLA XLB");
        result.Add("Convolution: "+left);
        result.Add("Channel: XRA XRB");
        result.Add("Convolution: "+right);
        result.Add("Channel: L R");
        result.Add("Copy: L=L+XLA+XRA R=R+XLB+XRB");
        return result;
    }

    private static string LocateData()
    {
        string? current=AppContext.BaseDirectory;
        for(int i=0;i<7 && current is not null;i++)
        {
            string file=Path.Combine(current,"surround3_hrir.bin");
            if (File.Exists(file)) return file;
            current=Directory.GetParent(current)?.FullName;
        }
        throw new FileNotFoundException("Surround Stage3의 실측 HRTF 데이터(surround3_hrir.bin)가 없습니다.");
    }
    private static Dictionary<string,(double[] Near,double[] Far)> ReadData()
    {
        if (_raw is not null) return _raw;
        using var stream = File.OpenRead(LocateData());
        using var compressed = new ZLibStream(stream,CompressionMode.Decompress);
        using var document = JsonDocument.Parse(compressed);
        var dictionary=new Dictionary<string,(double[],double[])>(StringComparer.Ordinal);
        foreach(var pair in document.RootElement.EnumerateObject())
        {
            var channels=pair.Value.EnumerateArray().ToArray();
            if(channels.Length!=2)throw new InvalidDataException("HRTF 데이터 채널 수가 맞지 않습니다.");
            dictionary.Add(pair.Name,(channels[0].EnumerateArray().Select(x=>x.GetDouble()).ToArray(),
                channels[1].EnumerateArray().Select(x=>x.GetDouble()).ToArray()));
        }
        _raw=dictionary;
        return dictionary;
    }
    private static double[] Resample(double[] input,int rate)
    {
        if(rate==SourceRate)return (double[])input.Clone();
        var result=new double[(int)Math.Ceiling(input.Length*rate/(double)SourceRate)];
        for(int i=0;i<result.Length;i++)
        {
            double at=i*SourceRate/(double)rate;
            int index=(int)at;
            double a=input[Math.Min(index,input.Length-1)];
            double b=input[Math.Min(index+1,input.Length-1)];
            result[i]=a+(b-a)*(at-index);
        }
        return result;
    }
    private static double[] BandLimit(double[] input,int rate)
    {
        double a=Math.Exp(-2*Math.PI*180/rate), b=1-Math.Exp(-2*Math.PI*8500/rate);
        double hp=0,previous=0,lp=0;
        var output=new double[input.Length];
        for(int i=0;i<input.Length;i++)
        {
            double x=input[i];hp=a*(hp+x-previous);previous=x;lp+=b*(hp-lp);
            output[i]=lp;
        }
        return output;
    }
    private static double PeakResponse(double[] a,double[] b)
    {
        double peak=0;
        foreach(var samples in new[]{a,b})
        {
            for(int k=0;k<=256;k++)
            {
                double theta=-Math.PI*k/256;
                Complex step=new(Math.Cos(theta),Math.Sin(theta));
                Complex phase=Complex.One, sum=Complex.Zero;
                foreach(double value in samples){sum+=phase*value;phase*=step;}
                peak=Math.Max(peak,sum.Magnitude);
            }
        }
        return Math.Max(1e-8,peak);
    }
    private static (double[] FrontNear,double[] FrontFar,double[] RearNear,double[] RearFar) Basis(DeviceProfile p)
    {
        string key=Mode(p)+":"+Rate(p);
        lock(CacheLock)
        {
            if(_basis.TryGetValue(key,out var cached))return cached;
            var data=ReadData();
            (double[],double[]) Process((double[] Near,double[] Far) raw)
            {
                var near=BandLimit(Resample(raw.Near,Rate(p)),Rate(p));
                var far=BandLimit(Resample(raw.Far,Rate(p)),Rate(p));
                double peak=PeakResponse(near,far);
                for(int i=0;i<near.Length;i++){near[i]/=peak;far[i]/=peak;}
                return (near,far);
            }
            var f=Process(data["front"]);
            var r=Process(data["rear_"+Mode(p)]);
            cached=(f.Item1,f.Item2,r.Item1,r.Item2);
            _basis[key]=cached;
            return cached;
        }
    }
    private static void Add(double[] dst,double[] source,double delay,double gain,int rate)
    {
        if(gain==0)return;
        int offset=(int)Math.Round(delay*rate,MidpointRounding.ToEven);
        for(int i=0;i<source.Length && i+offset<dst.Length;i++)dst[i+offset]+=gain*source[i];
    }
    private static (int Rate,(double[] L,double[] R) Left,(double[] L,double[] R) Right) Generate(DeviceProfile p)
    {
        int rate=Rate(p),length=(int)Math.Round(rate*.105,MidpointRounding.ToEven);
        var ll=new double[length];var lr=new double[length];var rl=new double[length];var rr=new double[length];
        double strength=Percent(p.SurroundStrength),depth=Percent(p.Surround3Depth),
            rear=Percent(p.Surround3Rear),front=Percent(p.Surround3Front),sep=Percent(p.Surround3Separation);
        if(strength<=0 || depth<=0 || sep<=0 || (rear<=0 && front<=0))return(rate,(ll,lr),(rl,rr));
        var basis=Basis(p);
        double wet=strength*depth*(.45+.55*sep);
        double rg=.85*wet*rear, fg=.37*wet*front;
        foreach(var (near,far) in new[]{(ll,lr),(rr,rl)})
        {
            Add(near,basis.FrontNear,.0028,fg,rate);Add(far,basis.FrontFar,.0035,fg,rate);
            Add(near,basis.RearNear,.0090,rg,rate);Add(far,basis.RearFar,.0102,rg,rate);
            Add(near,basis.RearNear,.0250,rg*.24,rate);Add(far,basis.RearFar,.0271,rg*.23,rate);
            Add(near,basis.RearNear,.0380,rg*.12,rate);Add(far,basis.RearFar,.0403,rg*.12,rate);
        }
        double peak=new[]{ll,lr,rl,rr}.Max(channel=>channel.Max(x=>Math.Abs(x)));
        if(peak>.82)
        {
            double factor=.82/peak;
            foreach(var channel in new[]{ll,lr,rl,rr})for(int i=0;i<channel.Length;i++)channel[i]*=factor;
        }
        return(rate,(ll,lr),(rl,rr));
    }
    private static short Pcm(double x)=>(short)Math.Round(Math.Clamp(x,-1,1)*32767,MidpointRounding.ToEven);
    private static void WriteWavIfMissing(string file,int rate,(double[] L,double[] R) channels)
    {
        if(File.Exists(file) && new FileInfo(file).Length>44)return;
        string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var stream=File.Create(temp))using(var writer=new BinaryWriter(stream,Encoding.ASCII,true))
            {
                int count=channels.L.Length;
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*4);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);
                writer.Write((short)2);writer.Write(rate);writer.Write(rate*4);writer.Write((short)4);writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(count*4);
                for(int i=0;i<count;i++){writer.Write(Pcm(channels.L[i]));writer.Write(Pcm(channels.R[i]));}
            }
            for(int attempt=0;attempt<5;attempt++)
            {
                try
                {
                    if(File.Exists(file) && new FileInfo(file).Length>44)return;
                    File.Move(temp,file,true);return;
                }
                catch(IOException) when(attempt<4){Thread.Sleep(120*(attempt+1));}
                catch(UnauthorizedAccessException) when(attempt<4){Thread.Sleep(120*(attempt+1));}
            }
        }
        finally
        {
            try{if(File.Exists(temp))File.Delete(temp);}catch(IOException){}catch(UnauthorizedAccessException){}
        }
    }
    public static void Ensure(string folder,DeviceProfile p)
    {
        if (p.DepthComparisonMode is "front" or "rear")
        {
            Directory.CreateDirectory(folder);
            var comparison = MakeComparison(p);
            WriteWavIfMissing(Path.Combine(folder, ComparisonFile(p)),comparison.Rate,comparison.Channels);
            return;
        }
        if(ApoLines(p).Count==0)return;
        Directory.CreateDirectory(folder);
        var names=Files(p);
        if(File.Exists(Path.Combine(folder,names.Left)) && File.Exists(Path.Combine(folder,names.Right)))return;
        var result=Generate(p);
        WriteWavIfMissing(Path.Combine(folder,names.Left),result.Rate,result.Left);
        WriteWavIfMissing(Path.Combine(folder,names.Right),result.Rate,result.Right);
    }
}
