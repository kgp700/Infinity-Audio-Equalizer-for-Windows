// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Runtime.InteropServices;
namespace InfinityAudioEqualizerPro;

// Native MMDevice COM enumeration; does not spawn PowerShell, cmd.exe, or Python.
public static class AudioDeviceEnumerator
{
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
        void GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        void RegisterEndpointNotificationCallback(IntPtr client);
        void UnregisterEndpointNotificationCallback(IntPtr client);
    }
    // IAudioClient::GetMixFormat returns the current Windows shared-mode mix format.
    // It can differ from a device's physical hardware clock; this is the format APO processes.
    [ComImport, Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        [PreserveSig] int Initialize(int shareMode, int streamFlags, long bufferDuration,
            long periodicity, IntPtr format, IntPtr audioSessionGuid);
        [PreserveSig] int GetBufferSize(out uint frames);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out uint padding);
        [PreserveSig] int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closestMatch);
        [PreserveSig] int GetMixFormat(out IntPtr format);
    }
    [ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        void GetCount(out uint count);
        void Item(uint index,out IMMDevice device);
    }
    [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        void Activate(ref Guid iid,int clsctx,IntPtr activationParams,out IntPtr iface);
        void OpenPropertyStore(int access,out IPropertyStore store);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        void GetState(out int state);
    }
    [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint count);
        void GetAt(uint index,out PropertyKey key);
        void GetValue(ref PropertyKey key,out PropVariant value);
        void SetValue(ref PropertyKey key,ref PropVariant value);
        void Commit();
    }
    [StructLayout(LayoutKind.Sequential)] private struct PropertyKey { public Guid Fmtid; public uint Pid; }
    [StructLayout(LayoutKind.Explicit,Size=24)] private struct PropVariant
    {
        [FieldOffset(0)] public ushort Type;
        [FieldOffset(8)] public IntPtr Pointer;
    }
    [DllImport("ole32.dll")] private static extern int PropVariantClear(ref PropVariant variant);

    /// <summary>Windows default multimedia render endpoint, not alphabetic first device.</summary>
    public static string? GetDefaultRenderDeviceId()
    {
        object? instance=null;
        IMMDevice? device=null;
        try
        {
            var type=Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"),true)!;
            instance=Activator.CreateInstance(type)!;
            ((IMMDeviceEnumerator)instance).GetDefaultAudioEndpoint(0,1,out device);
            device.GetId(out string id);
            return id;
        }
        catch(COMException) { return null; }
        finally
        {
            if(device is not null)Marshal.ReleaseComObject(device);
            if(instance is not null)Marshal.ReleaseComObject(instance);
        }
    }

    public static int? GetMixSampleRate(string endpointId)
    {
        if (string.IsNullOrWhiteSpace(endpointId)) return null;
        object? instance = null;
        IMMDevice? endpoint = null;
        IAudioClient? client = null;
        IntPtr clientPointer = IntPtr.Zero;
        IntPtr formatPointer = IntPtr.Zero;
        try
        {
            var type = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"), true)!;
            instance = Activator.CreateInstance(type)!;
            ((IMMDeviceEnumerator)instance).GetDevice(endpointId, out endpoint);
            Guid iid = new("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2");
            endpoint.Activate(ref iid, 23 /* CLSCTX_ALL */, IntPtr.Zero, out clientPointer);
            if (clientPointer == IntPtr.Zero) return null;
            client = (IAudioClient)Marshal.GetObjectForIUnknown(clientPointer);
            if (client.GetMixFormat(out formatPointer) < 0 || formatPointer == IntPtr.Zero) return null;
            int rate = Marshal.ReadInt32(formatPointer, 4); // WAVEFORMATEX.nSamplesPerSec
            return rate is >= 8000 and <= 384000 ? rate : null;
        }
        catch (COMException) { return null; }
        catch (InvalidCastException) { return null; }
        catch (ArgumentException) { return null; }
        finally
        {
            if (formatPointer != IntPtr.Zero) Marshal.FreeCoTaskMem(formatPointer);
            if (client is not null) Marshal.ReleaseComObject(client);
            if (clientPointer != IntPtr.Zero) Marshal.Release(clientPointer);
            if (endpoint is not null) Marshal.ReleaseComObject(endpoint);
            if (instance is not null) Marshal.ReleaseComObject(instance);
        }
    }

    public static List<AudioDevice> GetActiveRenderDevices()
    {
        var results=new List<AudioDevice>();
        object? o=null; IMMDeviceCollection? collection=null;
        try
        {
            var type=Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"),throwOnError:true)!;
            o=Activator.CreateInstance(type)!;
            var enumerator=(IMMDeviceEnumerator)o;
            enumerator.EnumAudioEndpoints(0,1,out collection); // eRender, DEVICE_STATE_ACTIVE
            collection.GetCount(out uint count);
            for(uint i=0;i<count;i++)
            {
                IMMDevice? dev=null;
                try
                {
                    collection.Item(i,out dev);
                    dev.GetId(out string id);
                    string guid=System.Text.RegularExpressions.Regex.Match(id,@"\{[0-9a-fA-F-]{36}\}\s*$").Value.Trim();
                    if(string.IsNullOrEmpty(guid)) continue;
                    string display=TryFriendlyName(dev)??id;
                    results.Add(new AudioDevice(id,display,guid));
                }
                finally { if(dev is not null) Marshal.ReleaseComObject(dev); }
            }
        }
        finally
        {
            if(collection is not null)Marshal.ReleaseComObject(collection);
            if(o is not null)Marshal.ReleaseComObject(o);
        }
        return results.OrderBy(x=>x.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
    }
    private static string? TryFriendlyName(IMMDevice dev)
    {
        IPropertyStore? store=null;
        try
        {
            dev.OpenPropertyStore(0,out store);
            var key=new PropertyKey{Fmtid=new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),Pid=14};
            store.GetValue(ref key,out PropVariant value);
            try { return value.Type==31 && value.Pointer!=IntPtr.Zero ? Marshal.PtrToStringUni(value.Pointer) : null; }
            finally { PropVariantClear(ref value); }
        }
        catch(COMException) { return null; }
        finally { if(store is not null)Marshal.ReleaseComObject(store); }
    }
}
