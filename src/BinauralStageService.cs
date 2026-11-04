// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
// Port of binaural_stage.py: KEMAR side-only HRTF, stable and balanced modes.
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace InfinityAudioEqualizerPro;

public static class BinauralStageService
{
    private const int Version=5;
    private static readonly object Gate=new();
    private static Dictionary<string,(double[] Near,double[] Far)>? _data;
    private static int Rate(DeviceProfile p)=>p.AmbienceRate is >=8000 and <=192000 ? p.AmbienceRate : 48000;
    private static string Mode(DeviceProfile p)=>p.SurroundMode is "cinema" or "stage" ? p.SurroundMode : "natural";
    private static double Pct(int value)=>Math.Clamp(value,0,100)/100d;
    private static string N(double value)=>value.ToString("0.######",CultureInfo.InvariantCulture);
    private static string WetFile(DeviceProfile p)=>$"infinity_parallel_hrtf_v{Version}_{Mode(p)}_{Rate(p)}.wav";
    private static string WidthFile(DeviceProfile p)=>$"infinity_parallel_width_v{Version}_{Rate(p)}.wav";
    private static string BalancedFile(DeviceProfile p)=>$"infinity_balanced_hrtf_v1_{Mode(p)}_{Math.Clamp(p.SurroundBalanceStability,0,100)}_{Math.Clamp(p.SurroundBalanceDepth,0,100)}_{Rate(p)}.wav";

    public static List<string> ApoLines(DeviceProfile p)
    {
        var result=new List<string>();
        if(!p.SurroundEnabled || !p.SurroundBinaural || p.LowLatency || p.SurroundStrength<=0)return result;
        double strength=Pct(p.SurroundStrength), spread=Pct(p.SurroundSpread), focus=Pct(p.SurroundCenter);
        if(p.SurroundBalanced)
        {
            double stability=Pct(p.SurroundBalanceStability),space=Pct(p.SurroundBalanceSpace),depth=Pct(p.SurroundBalanceDepth);
            double boost=.44*strength*space*(.4+.6*spread)*(1-.20*stability);
            double cross=boost/2;
            result.Add($"Copy: L={N(1+cross)}*L+-{N(cross)}*R R={N(1+cross)}*R+-{N(cross)}*L");
            if(depth>0 && space>0)
            {
                double wet=.30*strength*space*(1-.50*stability);
                result.Add("Copy: BSL=0.5*L+-0.5*R BSR=0.5*L+-0.5*R");
                result.Add("Channel: BSL BSR");
                result.Add("Convolution: "+BalancedFile(p));
                result.Add("Channel: L R");
                result.Add($"Copy: L=1*L+{N(wet)}*BSL R=1*R+{N(wet)}*BSR");
            }
            return result;
        }
        if(p.SurroundStable)
        {
            double sideGain=.34*strength*(.42+.58*spread)*(1.06-.06*focus),cross=sideGain*.5;
            result.Add($"Copy: L={N(1+cross)}*L+-{N(cross)}*R R={N(1+cross)}*R+-{N(cross)}*L");
            return result;
        }
        double width=1+strength*(.60+.95*spread),wetAmount=strength*(.26+.32*spread)*(1.14-.14*focus);
        result.Add("Copy: ISD=0.5*L+-0.5*R");
        result.Add("Copy: HWL=1*ISD HWR=1*ISD SWL=1*ISD SWR=1*ISD");
        result.Add("Channel: SWL SWR");result.Add("Convolution: "+WidthFile(p));
        result.Add("Channel: HWL HWR");result.Add("Convolution: "+WetFile(p));
        result.Add("Channel: L R");
        result.Add($"Copy: L=1*L+{N(width-1)}*SWL+{N(wetAmount)}*HWL R=1*R+-{N(width-1)}*SWR+{N(wetAmount)}*HWR");
        return result;
    }
    private static string FindData()
    {
        string? current=AppContext.BaseDirectory;
        for(int i=0;i<7 && current is not null;i++)
        {
            string candidate=Path.Combine(current,"kemar_hrir_data.bin");
            if(File.Exists(candidate))return candidate;
            current=Directory.GetParent(current)?.FullName;
        }
        throw new FileNotFoundException("Surround Stage KEMAR 측정 데이터(kemar_hrir_data.bin)가 없습니다.");
    }
    private static Dictionary<string,(double[] Near,double[] Far)> ReadData()
    {
        lock(Gate)
        {
            if(_data is not null)return _data;
            using var source=File.OpenRead(FindData());using var z=new ZLibStream(source,CompressionMode.Decompress);
            using var doc=JsonDocument.Parse(z);
            var dict=new Dictionary<string,(double[],double[])>();
            foreach(var property in doc.RootElement.EnumerateObject())
            {
                var channels=property.Value.EnumerateArray().ToArray();
                dict.Add(property.Name,(channels[0].EnumerateArray().Select(e=>e.GetDouble()).ToArray(),
                    channels[1].EnumerateArray().Select(e=>e.GetDouble()).ToArray()));
            }
            return _data=dict;
        }
    }
    private static double[] Resample(double[] samples,int rate)
    {
        if(rate==44100)return (double[])samples.Clone();
        var output=new double[(int)Math.Ceiling(samples.Length*rate/44100d)];
        for(int i=0;i<output.Length;i++)
        {
            double at=i*44100d/rate;int k=(int)at;double frac=at-k;
            output[i]=samples[Math.Min(k,samples.Length-1)]*(1-frac)+samples[Math.Min(k+1,samples.Length-1)]*frac;
        }
        return output;
    }
    private static double[] Soften(double[] input,int rate,double cutoff=9300,double originalMix=.28)
    {
        double alpha=1-Math.Exp(-2*Math.PI*cutoff/rate),a=0,b=0;
        var output=new double[input.Length];
        for(int i=0;i<input.Length;i++)
        {
            double sample=input[i];a+=alpha*(sample-a);b+=alpha*(a-b);
            output[i]=originalMix*sample+(1-originalMix)*b;
        }
        return output;
    }
    private static (int Rate,double[] L,double[] R) GenerateWet(DeviceProfile p)
    {
        int rate=Rate(p);var measured=ReadData()[Mode(p)];
        var near=Resample(measured.Near,rate);var far=Resample(measured.Far,rate);
        double[] smooth=new double[near.Length];double v=0;
        for(int i=0;i<near.Length;i++){v=.48*(near[i]-far[i])+.52*v;smooth[i]=v;}
        double peak=smooth.Max(x=>Math.Abs(x));
        if(peak>0)for(int i=0;i<smooth.Length;i++)smooth[i]*=.62/peak;
        int delay=(int)Math.Round(rate*(Mode(p) switch{"cinema"=>.020,"stage"=>.028,_=>.014}));
        int n=delay+smooth.Length+(int)Math.Round(rate*.055);
        var l=new double[n];var r=new double[n];
        for(int i=0;i<smooth.Length;i++){l[delay+i]+=.75*smooth[i];r[delay+i]-=.75*smooth[i];}
        foreach(var (ms,amp) in new[]{(39,.25),(57,-.18)})
        {
            int k=(int)Math.Round(rate*ms/1000d);
            if(k<n){l[k]+=amp;r[k]-=amp;}
        }
        return(rate,Soften(l,rate),Soften(r,rate));
    }
    private static (int Rate,double[] L,double[] R) GenerateBalanced(DeviceProfile p)
    {
        int rate=Rate(p);var measured=ReadData()[Mode(p)];
        var near=Resample(measured.Near,rate);var far=Resample(measured.Far,rate);
        var diff=near.Zip(far,(a,b)=>a-b).ToArray();
        double stability=Pct(p.SurroundBalanceStability),depth=Pct(p.SurroundBalanceDepth);
        double peak=diff.Max(x=>Math.Abs(x)),scale=Math.Min(1,.25/Math.Max(peak,1e-9));
        int delay=(int)Math.Round(rate*(.010+.009*depth));
        var l=new double[delay+diff.Length+1];var r=new double[l.Length];
        double gain=(.10+.18*depth)*(1-.70*stability);
        for(int i=0;i<diff.Length;i++){double v=diff[i]*scale*gain;l[delay+i]+=v;r[delay+i]-=v;}
        return(rate,Soften(l,rate,7200,.12),Soften(r,rate,7200,.12));
    }
    private static (int Rate,double[] L,double[] R) GenerateWidth(DeviceProfile p)
    {
        int rate=Rate(p);var data=new double[192];data[0]=1;
        var smoothed=Soften(data,rate,10500,.40);
        return(rate,smoothed,smoothed);
    }
    private static short ToPcm(double x)=>(short)Math.Round(Math.Clamp(x,-1,1)*32767);
    private static void Save(string file,(int Rate,double[] L,double[] R) data)
    {
        if(File.Exists(file)&&new FileInfo(file).Length>44)return;
        string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var stream=File.Create(temp))using(var writer=new BinaryWriter(stream,Encoding.ASCII,true))
            {
                int n=data.L.Length;writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+n*4);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);
                writer.Write((short)2);writer.Write(data.Rate);writer.Write(data.Rate*4);
                writer.Write((short)4);writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(n*4);
                for(int i=0;i<n;i++){writer.Write(ToPcm(data.L[i]));writer.Write(ToPcm(data.R[i]));}
            }
            for(int attempt=0;attempt<5;attempt++)
            {
                try
                {
                    if(File.Exists(file)&&new FileInfo(file).Length>44)return;
                    File.Move(temp,file,true);return;
                }
                catch(IOException) when(attempt<4){Thread.Sleep(120*(attempt+1));}
                catch(UnauthorizedAccessException) when(attempt<4){Thread.Sleep(120*(attempt+1));}
            }
        }
        finally{try{if(File.Exists(temp))File.Delete(temp);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    public static void Ensure(string folder,DeviceProfile p)
    {
        var commands=ApoLines(p);
        if(!commands.Any(x=>x.StartsWith("Convolution:",StringComparison.Ordinal)))return;
        Directory.CreateDirectory(folder);
        if(p.SurroundBalanced)
            Save(Path.Combine(folder,BalancedFile(p)),GenerateBalanced(p));
        else if(!p.SurroundStable)
        {
            Save(Path.Combine(folder,WetFile(p)),GenerateWet(p));
            Save(Path.Combine(folder,WidthFile(p)),GenerateWidth(p));
        }
    }
}
