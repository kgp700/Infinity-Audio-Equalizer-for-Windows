// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
// Original algorithms ported from the project's Python surround_stage.py and ambience.py.
using System.Globalization;
using System.Text;
namespace InfinityAudioEqualizerPro;

/// <summary>
/// Reproducible stereo FIR files for Equalizer APO. No limiter or automatic volume pumping.
/// A .NET build may be validated without Windows; output is standard PCM WAVE.
/// </summary>
public static class ImpulseResponseService
{
    private static readonly int[] SupportedRates = [44100,48000,88200,96000,176400,192000];
    private static int Rate(DeviceProfile profile) => SupportedRates.Contains(profile.AmbienceRate) ? profile.AmbienceRate : 48000;
    private static string Number(double n) => n.ToString("0.#####",CultureInfo.InvariantCulture);

    public static string SurroundFile(DeviceProfile p) =>
        $"infinity_surround_stage_v2_{Mode(p)}_{Math.Clamp(p.SurroundStrength,0,100)}_{Math.Clamp(p.SurroundCenter,0,100)}_{Math.Clamp(p.SurroundSpread,0,100)}_{Rate(p)}.wav";
    public static string AmbienceFile(DeviceProfile p) =>
        $"studio_ambience_v4_{Math.Clamp(p.AmbienceStrength,0,10)}_{Math.Clamp(p.AmbienceDecay,100,3000)}_{Rate(p)}.wav";
    public static string DiagnosticAmbienceFile(DeviceProfile p) =>
        $"studio_ambience_diagnostic_v3_{Rate(p)}.wav";
    private static string Mode(DeviceProfile p) => p.SurroundMode is "cinema" or "stage" ? p.SurroundMode : "natural";
    public static (double Own,double Cross) SurroundMatrix(DeviceProfile p)
    {
        double factor=Mode(p) switch {"cinema"=>1.05,"stage"=>1.2,_=>.75};
        double side=factor*Math.Pow(Math.Clamp(p.SurroundStrength,0,100)/100.0,1.1)
            *Math.Pow(Math.Clamp(p.SurroundSpread,0,100)/100.0,1.1)
            *(1.3-.55*Math.Clamp(p.SurroundCenter,0,100)/100.0);
        return (1+side,side);
    }
    public static void Ensure(string folder,AppSettings state)
    {
        if(!state.GlobalEnabled || state.BypassAllEffects)return;
        Directory.CreateDirectory(folder);
        foreach(var p in state.Profiles.Values)
        {
            if(!p.Enabled || p.LowLatency)continue;
            if (p.AmbienceDiagnostic)
            {
                EnsureFile(Path.Combine(folder,DiagnosticAmbienceFile(p)),()=>MakeDiagnosticAmbience(p));
                continue;
            }
            if (p.DepthComparisonMode is "front" or "rear")
            {
                HrtfStageService.Ensure(folder,p);
                continue;
            }
            if(p.SurroundEnabled && p.SurroundStrength>0)
            {
                if(p.SurroundBinaural)BinauralStageService.Ensure(folder,p);
                else EnsureFile(Path.Combine(folder,SurroundFile(p)),()=>MakeSurround(p));
            }
            if(p.AmbienceEnabled && p.AmbienceStrength>0)
                EnsureFile(Path.Combine(folder,AmbienceFile(p)),()=>MakeAmbience(p));
            if(p.SurroundEnabled && p.SurroundStrength>0 && p.Surround3Enabled)
                HrtfStageService.Ensure(folder,p);
        }
    }
    private static void EnsureFile(string path,Func<(int rate,double[] left,double[] right)> generate)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 44) return;
        // Every mode/parameter tuple has its own immutable filename. Never rewrite a
        // valid impulse that the audio service may be reading at this instant.
        var (rate,left,right)=generate();
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var stream=File.Create(temp)) using(var writer=new BinaryWriter(stream,Encoding.ASCII,true))
            {
                int count=left.Length;
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*4);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);
                writer.Write((short)2);writer.Write(rate);writer.Write(rate*4);writer.Write((short)4);writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(count*4);
                for(int i=0;i<count;i++){writer.Write(Short(left[i]));writer.Write(Short(right[i]));}
            }
            for (int attempt=0;attempt<5;attempt++)
            {
                try
                {
                    if (File.Exists(path) && new FileInfo(path).Length>44) return;
                    File.Move(temp,path,true);
                    return;
                }
                catch(IOException) when (attempt<4) { Thread.Sleep(100*(attempt+1)); }
                catch(UnauthorizedAccessException) when (attempt<4) { Thread.Sleep(100*(attempt+1)); }
            }
        }
        finally
        {
            try { if(File.Exists(temp))File.Delete(temp); }
            catch(IOException) { }
            catch(UnauthorizedAccessException) { }
        }
    }
    private static short Short(double x)=>(short)Math.Round(Math.Clamp(x,-1,1)*32767);
    private static (int,double[],double[]) MakeSurround(DeviceProfile p)
    {
        int rate=Rate(p);
        int n=(int)(rate*.12)+1;
        var l=new double[n];var r=new double[n];l[0]=r[0]=1;
        (double delay,double gain)[] taps=Mode(p) switch
        {
            "cinema"=>[(16,.42),(32,.35),(59,.25),(94,.18)],
            "stage"=>[(9,.36),(20,.28),(38,.22),(67,.18)],
            _=>[(12,.36),(24,.22),(41,.14),(69,.10)]
        };
        double wet=.90*Math.Pow(Math.Clamp(p.SurroundStrength,0,100)/100d,.8)
            *Math.Pow(Math.Clamp(p.SurroundSpread,0,100)/100d,.8);
        for(int j=0;j<taps.Length;j++)
        {
            var (delay,gain)=taps[j];
            int i=(int)(delay*rate/1000);if(i<n)l[i]+=wet*gain*(j%2==0?1:-1);
            int k=(int)((delay+1.5+2.3*(j%3))*rate/1000);if(k<n)r[k]+=wet*gain*(j%2==0?-1:1);
        }
        return (rate,l,r);
    }
    private static (int,double[],double[]) MakeAmbience(DeviceProfile p)
    {
        int rate=Rate(p), strength=Math.Clamp(p.AmbienceStrength,0,10), decay=Math.Clamp(p.AmbienceDecay,100,3000);
        int n=Math.Max(4,(int)(rate*(decay/1000.0+.04)));
        var l=new double[n];var r=new double[n];l[0]=r[0]=1;
        if(strength==0)return (rate,l,r);
        double wet=Math.Pow(strength/10.0,1.25)*.65;
        var rng=new Random(unchecked((int)0x1A0B1E));
        double[] reflection=[13,19.2,27.6,39.8,55.7,78.1];
        for(int j=0;j<reflection.Length;j++)
        {
            double ms=reflection[j],env=Math.Exp(-2.5*ms/decay),gain=env*(j<3?1.2:.55);
            int left=(int)Math.Round(ms/1000*rate);
            int right=(int)Math.Round((ms+(j%2==1?1.4:-1))/1000*rate);
            if(left>0 && left<n)l[left]+=gain*(rng.Next(2)==1?1:-1);
            if(right>0 && right<n)r[right]+=gain*(rng.Next(2)==1?1:-1);
        }
        int spacing=Math.Max(1,(int)(rate*.0017)),first=(int)(rate*.023);
        double tail=decay/1000.0;
        for(int t=first;t<n-1;t+=spacing)
        {
            double seconds=t/(double)rate;
            double envelope=Math.Exp(-3.3*seconds/Math.Max(.1,tail));
            double fade=Math.Min(1,(n-t)/(double)Math.Max(1,(int)(.040*rate)));
            double tap=envelope*fade*.35;
            int il=Math.Min(n-1,t+rng.Next(spacing));l[il]+=tap*(rng.NextDouble()*2-1);
            int ir=Math.Min(n-1,t+rng.Next(spacing));r[ir]+=tap*(rng.NextDouble()*2-1);
        }
        NormalizeWet(l,wet);NormalizeWet(r,wet);
        return(rate,l,r);
    }
    // Intentionally audible echoes, matched to the Python diagnostic test.
    // Do not use these taps in a saved listening preset.
    private static (int,double[],double[]) MakeDiagnosticAmbience(DeviceProfile p)
    {
        int rate=Rate(p), n=(int)(.82*rate)+1;
        var left=new double[n]; var right=new double[n];
        left[0]=right[0]=1;
        (int delay,double gain)[] taps=[(115,.95),(230,.85),(395,.70),(610,.50),(770,.35)];
        foreach(var (delay,gain) in taps)
        {
            int li=(int)(delay*rate/1000d),ri=(int)((delay+18)*rate/1000d);
            if(li<n)left[li]+=gain;
            if(ri<n)right[ri]+=gain;
        }
        return (rate,left,right);
    }
    private static void NormalizeWet(double[] values,double wet)
    {
        double sum=0;
        for(int i=1;i<values.Length;i++)sum+=values[i]*values[i];
        if(sum==0)return;
        double scale=wet/Math.Sqrt(sum);
        for(int i=1;i<values.Length;i++)values[i]*=scale;
    }
}
