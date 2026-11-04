// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
namespace InfinityAudioEqualizerPro;

public static class DeviceSelectionPolicy
{
    public static string Signature(IEnumerable<AudioDevice> devices) =>
        string.Join("|",devices.Select(d=>d.Id));

    // Follow the *new* Windows default device (e.g. newly connected Bluetooth).
    // Otherwise preserve a manual selection until it disappears.
    public static int ChooseIndex(IReadOnlyList<AudioDevice> devices,
        string? selectedId,string? defaultId,string? lastDefaultId)
    {
        int Find(string? id)
        {
            if(string.IsNullOrWhiteSpace(id))return -1;
            for(int i=0;i<devices.Count;i++)
                if(string.Equals(devices[i].Id,id,StringComparison.OrdinalIgnoreCase))return i;
            return -1;
        }
        bool changedDefault=!string.IsNullOrWhiteSpace(lastDefaultId) &&
            !string.IsNullOrWhiteSpace(defaultId) &&
            !string.Equals(defaultId,lastDefaultId,StringComparison.OrdinalIgnoreCase);
        int preferred=changedDefault?Find(defaultId):-1;
        if(preferred>=0)return preferred;
        preferred=Find(selectedId);
        if(preferred>=0)return preferred;
        preferred=Find(defaultId);
        return preferred>=0?preferred:(devices.Count>0?0:-1);
    }
}
