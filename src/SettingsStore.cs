// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Text;
using System.Text.Json;
namespace InfinityAudioEqualizerPro;

public static class SettingsStore
{
    // Product paths are independent of the implementation language or release channel.
    public const string ProductDirectoryName = "InfinityAudioEqualizerPro";
    public static readonly string CurrentDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductDirectoryName);
    public static readonly string CurrentFile = Path.Combine(CurrentDirectory, "settings.json");

    // Previous Windows development build: read only for a one-time user-settings migration.
    public static readonly string PreviousDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InfinityAudioEqualizerProCSharpPreview");
    public static readonly string LegacyDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InfinityAutoEQStudio");
    public static readonly string LegacyFile = Path.Combine(LegacyDirectory, "settings.json");
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static AppSettings Load()
    {
        try
        {
            MigratePreviousSettings(PreviousDirectory, CurrentDirectory);
            if (File.Exists(CurrentFile))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(CurrentFile), JsonOptions) ?? new();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("설정 파일을 읽지 못했습니다. 기존 파일은 보존됩니다.", e);
        }
        return new AppSettings();
    }

    /// <summary>
    /// Imports the user's settings once, without changing or deleting the previous file.
    /// A marker prevents deleted/reset production settings being resurrected later.
    /// The directory arguments make this behavior testable without real user data.
    /// </summary>
    public static bool MigratePreviousSettings(string previousDirectory, string destinationDirectory)
    {
        string oldFile = Path.Combine(previousDirectory, "settings.json");
        string newFile = Path.Combine(destinationDirectory, "settings.json");
        string marker = Path.Combine(previousDirectory, ".migrated-to-InfinityAudioEqualizerPro");
        if (File.Exists(marker) || !File.Exists(oldFile)) return false;
        if (File.Exists(newFile))
        {
            // An existing production profile is authoritative even if the old
            // development folder was never marked. Never resurrect it on reset.
            TryMarkMigration(marker, newFile);
            return false;
        }

        // Never replace a valid current configuration with the old development copy.
        string original = File.ReadAllText(oldFile, Encoding.UTF8);
        _ = JsonSerializer.Deserialize<AppSettings>(original, JsonOptions)
            ?? throw new JsonException("이전 설정 파일이 비어 있습니다.");
        Directory.CreateDirectory(destinationDirectory);
        string temp = Path.Combine(destinationDirectory,
            ".infinity-audio-equalizer-pro-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temp, original, new UTF8Encoding(false));
            File.Move(temp, newFile); // Never overwrite the new profile on a later launch.
            string previousBackup = oldFile + ".bak";
            if (File.Exists(previousBackup))
            {
                try { File.Copy(previousBackup, newFile + ".bak", overwrite: false); }
                catch (IOException) { } // Optional backup must not block migration.
                catch (UnauthorizedAccessException) { }
            }
            // The old profile stays readable for recovery; subsequent reset is a true reset.
            TryMarkMigration(marker, newFile);
            return true;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void TryMarkMigration(string marker, string destination)
    {
        try { File.WriteAllText(marker, "Settings imported to " + destination, new UTF8Encoding(false)); }
        catch (IOException) { } // The current profile stays authoritative.
        catch (UnauthorizedAccessException) { }
    }

    public static bool CanImportLegacy => File.Exists(LegacyFile);
    public static AppSettings ImportLegacyReadOnly()
    {
        var result = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(LegacyFile), JsonOptions)
                     ?? throw new InvalidDataException("기존 설정을 읽지 못했습니다.");
        if (result.Profiles is null) throw new InvalidDataException("프로필 정보가 없습니다.");
        return result;
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(CurrentDirectory);
        string target = CurrentFile;
        string temp = Path.Combine(CurrentDirectory,
            ".infinity-audio-equalizer-pro-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions), new UTF8Encoding(false));
            if (File.Exists(target)) File.Replace(temp, target, target + ".bak", true);
            else File.Move(temp, target);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
