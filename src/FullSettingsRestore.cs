// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
namespace InfinityAudioEqualizerPro;

/// <summary>
/// Full backups retain their original endpoint profiles. A driver reinstall can
/// change the Windows endpoint ID, so an explicit user-approved mapping copies
/// a profile to a new ID without changing or deleting its archived original.
/// This code never registers APO endpoints or changes Windows settings.
/// </summary>
public static class FullSettingsRestore
{
    public static List<AudioDevice> FindUnmatchedCurrentDevices(AppSettings backup, IEnumerable<AudioDevice> currentDevices)
    {
        if (backup.Profiles is null) throw new InvalidDataException("백업에 장치별 설정이 없습니다.");
        return currentDevices.Where(d=>!backup.Profiles.ContainsKey(d.Id))
            .GroupBy(d=>d.Id,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
    }

    public static Dictionary<string,DeviceProfile> FindArchivedProfiles(AppSettings backup, IReadOnlyList<AudioDevice> currentDevices)
    {
        var liveIds=new HashSet<string>(currentDevices.Select(d=>d.Id),StringComparer.OrdinalIgnoreCase);
        return backup.Profiles.Where(p=>!liveIds.Contains(p.Key))
            .ToDictionary(p=>p.Key,p=>p.Value,StringComparer.OrdinalIgnoreCase);
    }

    public static Dictionary<string, string> SuggestMappings(IReadOnlyDictionary<string,DeviceProfile> archived, IReadOnlyList<AudioDevice> unmatched)
    {
        var choices=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        var taken=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var old in archived)
        {
            var matches=unmatched.Where(d=>!taken.Contains(d.Id) &&
                string.Equals(d.Name,old.Value.Name,StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) continue;
            choices[old.Key]=matches[0].Id;
            taken.Add(matches[0].Id);
        }
        return choices;
    }

    /// <summary>
    /// Select the Windows default render endpoint first, preferably for an archived
    /// profile with the same friendly name. Other profiles receive only unique
    /// unambiguous name matches, never an arbitrary output device.
    /// </summary>
    public static Dictionary<string,string> SuggestMappingsWithDefault(
        IReadOnlyDictionary<string,DeviceProfile> archived,
        IReadOnlyList<AudioDevice> unmatched,
        string? defaultRenderDeviceId)
    {
        var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        if(archived.Count==0 || unmatched.Count==0)return result;
        var primary=unmatched.FirstOrDefault(d=>
            string.Equals(d.Id,defaultRenderDeviceId,StringComparison.OrdinalIgnoreCase)) ?? unmatched[0];
        var preferred=archived.FirstOrDefault(p=>string.Equals(p.Value.Name,primary.Name,StringComparison.OrdinalIgnoreCase));
        string profileId=string.IsNullOrEmpty(preferred.Key) ? archived.First().Key : preferred.Key;
        result.Add(profileId,primary.Id);
        var taken=new HashSet<string>(StringComparer.OrdinalIgnoreCase){primary.Id};
        foreach(var old in archived)
        {
            if(result.ContainsKey(old.Key))continue;
            var matches=unmatched.Where(d=>!taken.Contains(d.Id) &&
                string.Equals(d.Name,old.Value.Name,StringComparison.OrdinalIgnoreCase)).ToArray();
            if(matches.Length!=1)continue;
            result.Add(old.Key,matches[0].Id);
            taken.Add(matches[0].Id);
        }
        return result;
    }

    public static int ApplyMappings(AppSettings backup, IReadOnlyDictionary<string,string> mapping, IReadOnlyList<AudioDevice> currentDevices)
    {
        if (backup.Profiles is null) throw new InvalidDataException("백업에 장치별 설정이 없습니다.");
        var allowed = new HashSet<string>(currentDevices.Select(d=>d.Id),StringComparer.OrdinalIgnoreCase);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Validate the entire selection before touching the backup: a failed mapping
        // must not cause partial data restoration or overwrite an existing profile.
        foreach(var pair in mapping)
        {
            if (!backup.Profiles.ContainsKey(pair.Key) || !allowed.Contains(pair.Value)
                || backup.Profiles.ContainsKey(pair.Value) || !used.Add(pair.Value))
                throw new InvalidDataException("복원할 장치 연결 정보가 올바르지 않습니다. 대상을 다시 선택해주세요.");
        }
        foreach (var pair in mapping)
        {
            var source=backup.Profiles[pair.Key];
            // JSON round-trip preserves custom EQ, AutoEQ, VST3 compressor, spatial settings
            // and any future serialized fields; no linked references between two profiles.
            var json=System.Text.Json.JsonSerializer.Serialize(source,SettingsStore.JsonOptions);
            var copy=System.Text.Json.JsonSerializer.Deserialize<DeviceProfile>(json,SettingsStore.JsonOptions)
                     ?? throw new InvalidDataException("장치 프로필을 읽지 못했습니다.");
            var target=currentDevices.First(d=>string.Equals(d.Id,pair.Value,StringComparison.OrdinalIgnoreCase));
            copy.Name=target.Name;
            backup.Profiles.Add(pair.Value,copy);
        }
        return mapping.Count;
    }
}
