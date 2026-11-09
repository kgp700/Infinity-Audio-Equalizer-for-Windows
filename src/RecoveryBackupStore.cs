// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;

namespace InfinityAudioEqualizerPro;

/// <summary>On-demand permanent full-settings recovery snapshots, never age-pruned.</summary>
public static class RecoveryBackupStore
{
    public static readonly string DirectoryPath = Path.Combine(SettingsStore.CurrentDirectory, "RecoveryBackups");

    public sealed record RecoveryEntry(string DeviceId, string DeviceName, string Source, DateTime SavedAt,
        DeviceProfile Profile, bool FromSnapshot);

    // Friendly Windows endpoint names may differ only by spaces after driver updates.
    // This is a *suggestion* match, never evidence for unattended restoration.
    public static bool DeviceNamesMatch(string? left, string? right)
    {
        if(string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        static string Normalize(string value) => Regex.Replace(value.Normalize(NormalizationForm.FormKC),
            @"\s+", "").Trim().ToUpperInvariant();
        return string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
    }

    // A durable snapshot must be written successfully before any destructive settings change.
    public static string SaveSnapshot(AppSettings state, string reason, string? backupDirectory = null)
    {
        if (state.Profiles is null) throw new InvalidDataException("백업할 장치 설정이 없습니다.");
        string serialized=JsonSerializer.Serialize(state, SettingsStore.JsonOptions);
        _=JsonSerializer.Deserialize<AppSettings>(serialized, SettingsStore.JsonOptions)
            ?? throw new InvalidDataException("복구 백업 검증에 실패했습니다.");
        string directory=backupDirectory ?? DirectoryPath;
        Directory.CreateDirectory(directory);
        string name=$"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{reason}_{Guid.NewGuid():N}.json";
        string destination=Path.Combine(directory,name);
        string temp=destination+".tmp";
        try
        {
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                var bytes=new UTF8Encoding(false).GetBytes(serialized);
                stream.Write(bytes);
                stream.Flush(true);
            }
            File.Move(temp,destination); // Never overwrite or delete any earlier snapshot.
            // Verify the finalized file is readable before allowing the caller to mutate settings.
            var check=JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(destination),SettingsStore.JsonOptions);
            if(check?.Profiles is null || check.Profiles.Count!=state.Profiles.Count)
                throw new InvalidDataException("복구 백업을 확인할 수 없습니다.");
            return destination;
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }

    // Windows may enumerate several newly recreated Bluetooth render endpoints in a
    // single pairing sequence. Each endpoint formerly triggered its own full-settings
    // snapshot, so the recovery UI contained near-identical backups whose only
    // difference was the new blank profiles accumulated during enumeration.
    // Compare *meaningful saved audio profiles*, not the complete settings JSON:
    // new untouched device defaults and one-time prompt acknowledgements must not
    // turn a single pairing event into several backups. A genuine user DSP change
    // produces a different signature and is still protected by a new snapshot.
    public static string? SaveBeforeNewEndpointIfChanged(AppSettings state, string? backupDirectory = null)
    {
        string directory = backupDirectory ?? DirectoryPath;
        string signature = AudioProfileSignature(state);
        if (Directory.Exists(directory))
        {
            // Files include time + GUID; consider the latest valid snapshot of
            // this kind only. Never remove older snapshots or suppress a later
            // backup when meaningful audio settings have actually changed.
            foreach (string file in Directory.EnumerateFiles(directory, "*_before_new_endpoint_*.json")
                         .OrderByDescending(Path.GetFileName))
            {
                try
                {
                    var previous = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(file), SettingsStore.JsonOptions);
                    if (previous?.Profiles is null) continue;
                    if (string.Equals(AudioProfileSignature(previous), signature, StringComparison.Ordinal))
                        return null;
                    break;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
                {
                    DiagnosticLog.Error("이전 장치 복구 백업 확인", ex);
                    // A corrupted snapshot must not prevent new protective backups.
                }
            }
        }
        return SaveSnapshot(state, "before_new_endpoint", directory);
    }

    private static string AudioProfileSignature(AppSettings state)
    {
        var meaningful = new SortedDictionary<string, DeviceProfile>(StringComparer.OrdinalIgnoreCase);
        if (state.Profiles is not null)
            foreach (var pair in state.Profiles)
                if (pair.Value is not null && !IsUntouchedDefaultProfile(pair.Value))
                    meaningful[pair.Key] = pair.Value;
        return JsonSerializer.Serialize(meaningful, SettingsStore.JsonOptions);
    }

    public static List<RecoveryEntry> ListEntries(AppSettings current, string? backupDirectory = null)
    {
        var entries=new List<RecoveryEntry>();
        DateTime currentFileModified = File.Exists(SettingsStore.CurrentFile)
            ? File.GetLastWriteTime(SettingsStore.CurrentFile) : DateTime.MinValue;
        if(current.Profiles is not null)
            foreach(var p in current.Profiles)
                entries.Add(new RecoveryEntry(p.Key,p.Value.Name,"현재 설정 (백업 아님)",currentFileModified,p.Value,false));
        string directory=backupDirectory ?? DirectoryPath;
        if(!Directory.Exists(directory)) return entries;
        foreach(var file in Directory.EnumerateFiles(directory,"*.json").OrderByDescending(x=>x))
        {
            try
            {
                var settings=JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(file),SettingsStore.JsonOptions);
                if(settings?.Profiles is null)continue;
                string filename=Path.GetFileNameWithoutExtension(file);
                DateTime when=File.GetCreationTime(file);
                if(filename.Length>=19 && DateTime.TryParseExact(filename[..19],"yyyyMMdd_HHmmss_fff",
                        CultureInfo.InvariantCulture,DateTimeStyles.None,out var parsed)) when=parsed;
                string reason=filename.Contains("before_eq_reset",StringComparison.OrdinalIgnoreCase)?"31밴드 EQ 초기화 전":
                    filename.Contains("before_all_reset",StringComparison.OrdinalIgnoreCase)?"전체 설정 초기화 전":
                    filename.Contains("before_device_reset",StringComparison.OrdinalIgnoreCase)?"장치 음향효과 초기화 전":
                    filename.Contains("before_import",StringComparison.OrdinalIgnoreCase)?"설정 불러오기 전":
                    filename.Contains("before_restore",StringComparison.OrdinalIgnoreCase)?"설정 복구 전":
                    filename.Contains("before_new_endpoint",StringComparison.OrdinalIgnoreCase)?"새 출력 장치 인식 전":"안전 백업";
                foreach(var p in settings.Profiles)
                    entries.Add(new RecoveryEntry(p.Key,p.Value.Name,reason,when,p.Value,true));
            }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException)
            { DiagnosticLog.Error("복구 백업 목록 읽기",e); }
        }
        return entries;
    }

    // Unchanged, newly created endpoint profiles have no prior DSP adjustments.
    // A profile with a flat 31-band EQ may still contain useful AutoEQ or spatial
    // settings, so compare the entire stored profile rather than just EQ sliders.
    public static bool IsUntouchedDefaultProfile(DeviceProfile profile)
    {
        var baseline = new DeviceProfile { Name = profile.Name };
        return JsonSerializer.Serialize(profile, SettingsStore.JsonOptions) ==
               JsonSerializer.Serialize(baseline, SettingsStore.JsonOptions);
    }

    /// <summary>
    /// Return only meaningful recovery sources for the selected endpoint. Its
    /// live profile is never a backup, even if its name matches a past device.
    /// Historical snapshots remain visible even when their EQ values are flat.
    /// </summary>
    public static List<RecoveryEntry> FindRecoveryCandidates(IEnumerable<RecoveryEntry> entries,
        string? targetId, string? targetName, bool includeOtherDevices)
    {
        return entries
            .Where(entry =>
                // A current profile cannot be used to recover itself.
                !(entry.DeviceId.Equals(targetId, StringComparison.OrdinalIgnoreCase) && !entry.FromSnapshot))
            // A full-settings snapshot can contain default profiles created earlier
            // in the same pairing sequence. They are not useful restoration sources.
            // Flat EQ alone is not a reason to exclude a profile: other DSP
            // effects are compared as well.
            .Where(entry => !IsUntouchedDefaultProfile(entry.Profile))
            .Where(entry => includeOtherDevices ||
                entry.DeviceId.Equals(targetId, StringComparison.OrdinalIgnoreCase) ||
                DeviceNamesMatch(entry.DeviceName, targetName))
            .OrderByDescending(entry => entry.FromSnapshot &&
                entry.DeviceId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(entry => DeviceNamesMatch(entry.DeviceName, targetName))
            .ThenByDescending(entry => entry.FromSnapshot)
            .ThenByDescending(entry => entry.SavedAt)
            .ToList();
    }

    public static DeviceProfile CloneProfile(DeviceProfile source)
    {
        string json=JsonSerializer.Serialize(source,SettingsStore.JsonOptions);
        return JsonSerializer.Deserialize<DeviceProfile>(json,SettingsStore.JsonOptions)
            ?? throw new InvalidDataException("복구할 장치 설정을 읽지 못했습니다.");
    }
}
