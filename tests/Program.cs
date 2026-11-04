using InfinityAudioEqualizerPro;

int assertions = 0;
void Check(bool condition, string message)
{
    assertions++;
    if (!condition) throw new Exception("검증 실패: " + message);
}

// A genuinely fresh installation must start with global effects OFF. Subsequent
// launches must deserialize the last explicit ON/OFF choice unchanged.
var firstLaunch = new AppSettings();
Check(!firstLaunch.GlobalEnabled && firstLaunch.BypassAllEffects,
    "Fresh installation must start with the master effect switch OFF");
string firstLaunchJson = System.Text.Json.JsonSerializer.Serialize(firstLaunch, SettingsStore.JsonOptions);
var firstLaunchReloaded = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(firstLaunchJson, SettingsStore.JsonOptions);
Check(firstLaunchReloaded is not null && !firstLaunchReloaded.GlobalEnabled && firstLaunchReloaded.BypassAllEffects,
    "Fresh OFF status was not preserved after restart");
var userEnabled = new AppSettings { GlobalEnabled = true, BypassAllEffects = false };
var enabledReloaded = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(
    System.Text.Json.JsonSerializer.Serialize(userEnabled, SettingsStore.JsonOptions), SettingsStore.JsonOptions);
Check(enabledReloaded is not null && enabledReloaded.GlobalEnabled && !enabledReloaded.BypassAllEffects,
    "User-selected ON status was not restored on restart");
Console.WriteLine("PASS: first-run master OFF and persisted ON/OFF across launches");

// New profiles prepare user-preferred spatial and compressor values without
// activating the Surround Stage or the compressor by themselves.
var newProfileDefaults = new DeviceProfile();
Check(newProfileDefaults.AmbienceDecay == 1000, "Studio Ambience default decay must be 1000ms");
Check(newProfileDefaults.SurroundMode == "cinema" && newProfileDefaults.SurroundBinaural &&
    newProfileDefaults.Surround3Enabled && !newProfileDefaults.SurroundEnabled,
    "Surround Stage defaults must prepare Cinema HRTF + front/rear, but stay OFF");
Check(newProfileDefaults.CompressorTheme == "OPEN" && newProfileDefaults.CompressorStrength == 12 &&
    !newProfileDefaults.CompressorEnabled,
    "Compressor defaults must prepare Open at strength 12, but stay OFF");
var reloadedDefaults=System.Text.Json.JsonSerializer.Deserialize<DeviceProfile>(
    System.Text.Json.JsonSerializer.Serialize(newProfileDefaults));
Check(reloadedDefaults is not null && reloadedDefaults.SurroundBinaural &&
    reloadedDefaults.Surround3Enabled && reloadedDefaults.CompressorStrength==12,
    "New profile defaults must persist in JSON");
Console.WriteLine("PASS: new-device spatial, ambience and compressor defaults");

var id = "{0BD7A1BE-7A1A-44DB-8397-CC5392387B5E}";
var profile = new DeviceProfile { Name = "Test", CustomEnabled = true };
var state = new AppSettings { GlobalEnabled = true, BypassAllEffects = true };
state.Profiles[id] = profile;
string neutral = ApoRenderer.Render(state);
Check(!neutral.Contains("Preamp:"), "전체 우회 상태에서 프리앰프가 생성됨");
Check(!neutral.Contains("GraphicEQ:"), "전체 우회 상태에서 EQ가 생성됨");
Check(!neutral.Contains("Copy:"), "전체 우회 상태에서 스테레오 변형이 생성됨");
state.BypassAllEffects = false;
string defaults = ApoRenderer.Render(state);
// Python-compatible default uses a static -5 dB manual preamp, even when the 31-band EQ is flat.
Check(defaults.Contains("Preamp: -5 dB"), "Python 기본 수동 프리앰프 -5dB가 누락됨");
Check(!defaults.Contains("GraphicEQ:"), "플랫 EQ가 생성됨");
// A user-selected 0 dB manual preamp must generate no preamp command.
profile.ManualPreamp = 0;
Check(!ApoRenderer.Render(state).Contains("Preamp:"), "수동 0dB 설정에서 프리앰프가 남음");
profile.ManualPreamp = -5;
profile.Custom[17] = 3; // 1000 Hz
string custom = ApoRenderer.Render(state);
Check(custom.Contains("GraphicEQ:") && custom.Contains("1000 3"), "1kHz +3dB 설정 누락");
profile.CustomEnabled = false;
Check(!ApoRenderer.Render(state).Contains("GraphicEQ:"), "31밴드 비활성화 무시됨");
profile.AutoEnabled = true;
profile.AutoFilters.Add(new AutoFilter { Type = "PK", Frequency = 1000, Gain = -2, Q = 1 });
profile.Graphic.Add([1000, 6]); // alternative AutoEQ format must not double-apply
string auto = ApoRenderer.Render(state);
Check(auto.Contains("Filter: ON PK") && !auto.Contains("GraphicEQ:"), "AutoEQ 대안 형식 중복 적용");
profile.AutoFilters.Clear();
profile.CustomEnabled = true;
string combined = ApoRenderer.Render(state);
Check(combined.Split("GraphicEQ:").Length == 3, "Python 기준 AutoEQ+31밴드 두 단계가 누락됨");
state.GlobalEnabled = false;
Check(!ApoRenderer.Render(state).Contains("GraphicEQ:"), "엔진 OFF에서 필터가 남음");
string root = "Preamp: -6 dB\nInclude: example.txt\nGraphicEQ: 25 0; 40 0\nDevice: {TEST}\nPreamp: -2 dB\n";
string installed = ApoBridge.PrepareConfigForSession(root);
Check(installed.Contains("# InfinityAudioEqualizerPro-root-paused: Preamp: -6 dB"), "전역 프리앰프 중지 누락");
Check(installed.Contains("# InfinityAudioEqualizerPro-root-paused: Include: example.txt"), "전역 Include 중지 누락");
Check(installed.Contains("Device: {TEST}\n# InfinityAudioEqualizerPro-root-paused: Preamp: -2 dB"), "다른 장치 필터 중지가 누락됨");
Check(installed.IndexOf(ApoBridge.Marker,StringComparison.Ordinal) < installed.IndexOf("Device: {TEST}",StringComparison.Ordinal), "C# Include가 Device 스코프 뒤에 있음");
Check(installed.Contains(ApoBridge.Marker), "C# bridge 마커 누락");
Check(!installed.Contains("\nPreamp: -2 dB"),"기존 장치별 프리앰프 활성 상태로 남음");
Check(!installed.Contains("\nGraphicEQ: 25 0"),"기존 그래픽 EQ 활성 상태로 남음");
Check(ApoBridge.RestoreConfigFromSession(installed).Trim() == root.Trim(), "원본 APO 설정 복원 실패");
string neutralUnlinked=ApoBridge.NeutralConfigAfterDisconnect(installed);
Check(!neutralUnlinked.Contains("Preamp:") && !neutralUnlinked.Contains("GraphicEQ:") && !neutralUnlinked.Contains("Include:") && !neutralUnlinked.Contains("Device:"),
    "APO 연결 해제 후 기존 필터가 남아 있음");
try
{
    ApoBridge.PrepareConfigForSession("UnknownEffect: special\n");
    throw new Exception("검증 실패: 알 수 없는 전역 설정을 허용함");
}
catch (InvalidOperationException) { assertions++; }
// Compressor crossovers should always remain in order in the editor. Every tab
// receives a dynamic frequency interval bounded by its adjacent bands.
var normalCrossovers=CompressorFrequencyRanges.Normalize(new double[] { 160,850,4000,8500,20000 });
Check(normalCrossovers.SequenceEqual(new decimal[] {160,850,4000,8500,20000}),
    "Valid compressor crossovers changed when opening the editor");
var importedCrossovers=CompressorFrequencyRanges.Normalize(new double[] {500,60,8000,1000,15000});
Check(importedCrossovers[4]==20000 && Enumerable.Range(1,4).All(i=>importedCrossovers[i]>importedCrossovers[i-1]),
    "Invalid saved compressor crossover frequencies were not normalized");
var bassRange=CompressorFrequencyRanges.Allowed(normalCrossovers,1);
Check(bassRange.Min==161 && bassRange.Max==2500,
    "Bass cutoff must respect the Sub cutoff and its band-specific upper limit");
var subRange=CompressorFrequencyRanges.Allowed(normalCrossovers,0);
Check(subRange.Min==30 && subRange.Max==500,
    "Sub cutoff must have a safe band-specific range");
var highRange=CompressorFrequencyRanges.Allowed(normalCrossovers,3);
Check(highRange.Min==4001 && highRange.Max==16000,
    "High cutoff must respect the Mid cutoff");
var airRange=CompressorFrequencyRanges.Allowed(normalCrossovers,4);
Check(airRange.Min==20000 && airRange.Max==20000,
    "Air upper frequency must stay fixed at 20kHz");
Console.WriteLine("PASS: five-band compressor dynamic crossover limits and imported preset repair");
var compressor=CompressorThemes.Parameters("PREMIUM_BALANCE",12);
Check(compressor.Contains("T1 ") && compressor.Contains("X4 ") && compressor.Split(' ').Length == 68,
    "VST3 5밴드 34개 파라미터 생성 오류");
profile.CompressorEnabled = true;
state.GlobalEnabled = true;
profile.AutoEnabled=false;profile.CustomEnabled=false;
string withPlugin=ApoRenderer.Render(state, @"C:\APO\config");
Check(withPlugin.Contains("VSTPlugin: Library") && withPlugin.Contains("InfinityCompressor.vst3"), "컴프레서 설정 누락");
profile.CompressorEnabled = false;
profile.ManualPreamp=-6;profile.PreampMode="manual";
Check(ApoRenderer.RenderProfile(profile).Any(x=>x.StartsWith("Preamp: -6")),"Python 방식의 수동 프리앰프 누락");
profile.Enabled=false;
Check(!ApoRenderer.RenderProfile(profile).Any(x=>x.StartsWith("Preamp:")),"장치 EQ OFF에서도 프리앰프 활성");
profile.Enabled=true;
// Guard against a stale user-saved APO folder masking the active XT registry ConfigPath.
string rootChoiceTest = Path.Combine(Path.GetTempPath(), "InfinityAudioEqualizerPro-APORootTest-" + Guid.NewGuid().ToString("N"));
try
{
    string registered = Path.Combine(rootChoiceTest, "registered");
    string saved = Path.Combine(rootChoiceTest, "saved-old");
    Directory.CreateDirectory(registered);
    Directory.CreateDirectory(saved);
    File.WriteAllText(Path.Combine(registered, "config.txt"), "# active XT config\n");
    File.WriteAllText(Path.Combine(saved, "config.txt"), "# unused legacy config\n");
    Check(ApoBridge.ChooseConfigDirectory(saved, registered, []) == Path.GetFullPath(registered),
        "Old saved APO path shadowed registered ConfigPath (silent no-effect issue)");
    Check(ApoBridge.ChooseConfigDirectory(saved, null, [registered]) == Path.GetFullPath(registered),
        "Canonical XT config directory lost priority to old saved path");
    Check(ApoBridge.ChooseConfigDirectory(saved, null, []) == Path.GetFullPath(saved),
        "Legacy config fallback not available");
}
finally { try { Directory.Delete(rootChoiceTest, recursive:true); } catch (IOException) {} }
// The user must receive the actual backup filename after first connection.
string receiptRoot = Path.Combine(Path.GetTempPath(), "InfinityAudioEqualizerPro-BackupTest-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(Path.Combine(receiptRoot, ApoBridge.Folder));
    string savedBackup = Path.Combine(receiptRoot, ApoBridge.BackupNamePrefix + "20261103-123456-123.txt");
    File.WriteAllText(savedBackup, "# saved original configuration");
    string receipt = Path.Combine(receiptRoot, ApoBridge.Folder, "last_connection_backup.txt");
    File.WriteAllText(receipt, savedBackup);
    Check(ApoBridge.LastConnectionBackup(receiptRoot) == savedBackup, "Connected XT backup filename not available to UI");
    File.WriteAllText(receipt, Path.Combine(Path.GetTempPath(), ApoBridge.BackupNamePrefix + "spoof.txt"));
    Check(ApoBridge.LastConnectionBackup(receiptRoot) is null, "Backup receipt must not reference outside directory");
}
finally
{
    try { Directory.Delete(receiptRoot, recursive:true); }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
}
// Previous developer-named paths must not appear in newly generated user locations.
Check(SettingsStore.ProductDirectoryName == "InfinityAudioEqualizerPro", "User data directory has a development suffix");
Check(Path.GetFileName(SettingsStore.CurrentDirectory) == "InfinityAudioEqualizerPro", "User settings directory mismatch");
Check(ApoBridge.Folder == "InfinityAudioEqualizerPro", "APO managed directory mismatch");
Check(ApoBridge.Marker == "Include: InfinityAudioEqualizerPro\\managed.txt", "APO include uses legacy name");
string migrationTestRoot = Path.Combine(Path.GetTempPath(), "InfinityAudioEqualizerPro-MigrationTest-" + Guid.NewGuid().ToString("N"));
try
{
    string prior = Path.Combine(migrationTestRoot, "previous");
    string current = Path.Combine(migrationTestRoot, "current");
    Directory.CreateDirectory(prior);
    File.WriteAllText(Path.Combine(prior, "settings.json"),
        System.Text.Json.JsonSerializer.Serialize(new AppSettings { GlobalEnabled = false }, SettingsStore.JsonOptions));
    Check(SettingsStore.MigratePreviousSettings(prior, current), "Previous settings were not copied");
    Check(File.Exists(Path.Combine(current, "settings.json")), "New user settings file is missing");
    Check(File.Exists(Path.Combine(prior, "settings.json")), "Previous settings were unexpectedly deleted");
    Check(!SettingsStore.MigratePreviousSettings(prior, current), "Settings reimported over current file");
    File.Delete(Path.Combine(current, "settings.json"));
    Check(!SettingsStore.MigratePreviousSettings(prior, current), "Reset settings resurrected previous preferences");
    Check(!File.Exists(Path.Combine(current, "settings.json")), "Old settings reappeared after reset");
    // A product profile already present before migration must also prevent future reimport.
    string prior2 = Path.Combine(migrationTestRoot, "previous2");
    string current2 = Path.Combine(migrationTestRoot, "current2");
    Directory.CreateDirectory(prior2);
    Directory.CreateDirectory(current2);
    File.WriteAllText(Path.Combine(prior2, "settings.json"), "{}");
    File.WriteAllText(Path.Combine(current2, "settings.json"), "{}");
    Check(!SettingsStore.MigratePreviousSettings(prior2, current2), "Existing settings were overwritten");
    File.Delete(Path.Combine(current2, "settings.json"));
    Check(!SettingsStore.MigratePreviousSettings(prior2, current2), "Reset resurrected a pre-existing profile");
}
finally
{
    try { Directory.Delete(migrationTestRoot, recursive:true); }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
}
// Elevated re-link from the previous managed APO folder should remove old marker
// and preserve the global filter comments without double-applying them.
string oldRoot = "# Infinity Audio Equalizer Pro for Windows - C#\n" + ApoBridge.PreviousMarker +
    "\n# InfinityCS-root-paused: Preamp: -6 dB\nDevice: {OLD}\n";
string restoredOld = ApoBridge.RestoreConfigFromSession(oldRoot);
Check(restoredOld.Contains("Preamp: -6 dB") && !restoredOld.Contains(ApoBridge.PreviousMarker),
    "Failed to recover an old managed APO connection");
string upgradedRoot = ApoBridge.PrepareConfigForSession(restoredOld);
Check(upgradedRoot.Contains(ApoBridge.Marker) && !upgradedRoot.Contains(ApoBridge.PreviousMarker),
    "APO bridge upgrade still references previous managed folder");

Console.WriteLine($"PASS: EQ/APO/VST3 설정 {assertions}개 검증");

// Python AutoEq INDEX format: model names may include parentheses and measurement variants.
string catalog = "- [AirPods 4 (ANC on)](./oratory1990/in-ear/Apple AirPods 4 (ANC on)) by oratory1990\n" +
                 "- [Galaxy Buds3](./test/in-ear/Samsung Galaxy Buds3) by official\n" +
                 "- [Malicious](../escape) by invalid\n";
var entries=AutoEqCatalogService.ParseIndex(catalog);
Check(entries.Count==2,"AutoEQ index parser: official rows / path validation");
Check(entries[0].Name=="AirPods 4 (ANC on)" && entries[0].Folder.Contains("(ANC on)"),"INDEX 이름의 괄호 처리");
var imported=new DeviceProfile();
AutoEqImporter.Import("Preamp: -5.3 dB\nFilter 1: ON PK Fc 1000 Hz Gain 3 dB Q 1.0",imported,"test.txt");
Check(imported.AutoEnabled && imported.AutoFilters.Count==1 && imported.AutoPreamp == -5.3,"AutoEQ PEQ import");
Check(imported.PreampMode=="manual" && imported.ManualPreamp==-5, "AutoEQ 가져오기 시 프리앰프가 강제로 켜짐");
var preserved = new DeviceProfile { PreampMode="manual", ManualPreamp=-2.25,
    CompressorEnabled=true, SurroundEnabled=true, SurroundStrength=70, CustomEnabled=true };
preserved.Custom[17]=4.5;
AutoEqImporter.Import("Preamp: -8 dB\nFilter 1: ON PK Fc 1000 Hz Gain 5 dB Q 1", preserved, "manual-untouched");
Check(preserved.PreampMode=="manual" && preserved.ManualPreamp==-2.25,
    "AutoEQ import overwrote user preamp");
Check(preserved.AutoPreamp==-8 && preserved.CompressorEnabled && preserved.SurroundEnabled && preserved.Custom[17]==4.5,
    "AutoEQ import modified other effects");
AutoEqImporter.Import("Preamp: -4 dB\nFilter 1: ON PK Fc 300 Hz Gain 2 dB Q 1", preserved, "second-import");
Check(preserved.ManualPreamp==-2.25 && preserved.PreampMode=="manual", "Repeated AutoEQ import modified preamp");
var stageProfile=new DeviceProfile{SurroundEnabled=true,SurroundStrength=75,SurroundSpread=70,SurroundCenter=80,
    SurroundMode="cinema"};
var (own,cross)=ImpulseResponseService.SurroundMatrix(stageProfile);
Check(double.IsFinite(own)&&double.IsFinite(cross)&&Math.Abs(own-cross-1)<0.000001,
    "Surround matrix changed center/mono signal level");
var stageCommands=ApoRenderer.RenderProfile(stageProfile,@"C:\APO\config\InfinityCompressor.vst3");
Check(stageCommands.Any(x=>x.StartsWith("Copy:")) && stageCommands.Any(x=>x.StartsWith("Convolution:")),
    "Surround enabled but Copy/Convolution not generated");
stageProfile.SurroundEnabled=false;
Check(!ApoRenderer.RenderProfile(stageProfile,@"C:\APO\config\InfinityCompressor.vst3").Any(x=>x.StartsWith("Copy:")||x.StartsWith("Convolution:")),
    "Surround disabled but DSP commands remain");
imported.PreampMode="manual"; imported.ManualPreamp=0;
Check(Math.Abs(PreampCalculator.Calculate(imported).Final)<1e-9,"manual preamp stays absolute");
imported.PreampMode="source";
Check(Math.Abs(PreampCalculator.Calculate(imported).Final+5.3)<1e-6,"source preamp is applied only when selected");
imported.PreampMode="automatic";
Check(PreampCalculator.Calculate(imported).Final<=-5.3,"automatic baseline guards headroom");
Console.WriteLine("PASS: AutoEQ INDEX/import/preamp tests");

// Spatial DSP regression: only enabled effects produce impulse-response commands.
var spatial = new DeviceProfile { Enabled=true,SurroundEnabled=true,SurroundStrength=45,SurroundSpread=45,
    AmbienceEnabled=true,AmbienceStrength=4,AmbienceDecay=240,AmbienceRate=48000,
    // Explicit legacy spatial pipeline: the default for new profiles is now binaural HRTF + Stage3.
    SurroundBinaural=false,Surround3Enabled=false };
var spatialState = new AppSettings{GlobalEnabled=true,BypassAllEffects=false};
spatialState.Profiles[id] = spatial;
var spatialText=ApoRenderer.Render(spatialState,Path.GetTempPath());
Check(spatialText.Contains("infinity_surround_stage_v2_") && spatialText.Contains("studio_ambience_v4_"),"Surround/Studio convolution entries missing");
var spatialFolder=Path.Combine(Path.GetTempPath(),"InfinityAudioEqualizerPro-SmokeTest-"+Guid.NewGuid().ToString("N"));
try
{
    ImpulseResponseService.Ensure(spatialFolder,spatialState);
    foreach(var file in new[]{ImpulseResponseService.SurroundFile(spatial),ImpulseResponseService.AmbienceFile(spatial)})
    {
        var path=Path.Combine(spatialFolder,file);
        Check(File.Exists(path),"IR file missing: "+file);
        using var f=File.OpenRead(path);
        var bytes=new byte[12];
        Check(f.Read(bytes,0,12)==12 && System.Text.Encoding.ASCII.GetString(bytes,0,4)=="RIFF" &&
              System.Text.Encoding.ASCII.GetString(bytes,8,4)=="WAVE", "invalid PCM WAV file: "+file);
    }
    spatialState.BypassAllEffects=true;
    Check(!ApoRenderer.Render(spatialState,Path.GetTempPath()).Contains("Convolution:"),"bypass still applies spatial DSP");
}
finally{if(Directory.Exists(spatialFolder))Directory.Delete(spatialFolder,true);}
Console.WriteLine("PASS: Spatial/Studio impulse response tests");
// Verify the newly requested defaults independently of the legacy/basic FIR tests.
var newDefaults = new DeviceProfile();
Check(!newDefaults.SurroundEnabled && newDefaults.SurroundBinaural && newDefaults.Surround3Enabled &&
      newDefaults.SurroundMode == "cinema" && newDefaults.AmbienceDecay == 1000 &&
      newDefaults.CompressorTheme == "OPEN" && newDefaults.CompressorStrength == 12,
      "New-device defaults do not match the requested spatial/ambience/compressor settings");
newDefaults.SurroundEnabled = true;
var defaultHrtfCommands = ApoRenderer.RenderProfile(newDefaults, Path.Combine(Path.GetTempPath(), "InfinityCompressor.vst3"));
Check(defaultHrtfCommands.Count(x => x.StartsWith("Convolution:", StringComparison.Ordinal)) == 4 &&
      defaultHrtfCommands.Any(x => x.Contains("infinity_parallel_hrtf_v5_")) &&
      defaultHrtfCommands.Any(x => x.Contains("infinity_stage3_v6_")),
      "New HRTF/Stage3 defaults must generate both spatial processing stages");
Console.WriteLine("PASS: New HRTF/Stage3/Studio/compressor defaults");
var share=EffectTransfer.ExportShare(new DeviceProfile { Name="Shared",Source=@"C:\\Users\\someone\\private.json",AmbienceEnabled=true,AmbienceDecay=700 });
Check(!share.Contains("private.json") && !share.Contains("apo_config") && EffectTransfer.IsShare(share),"sharing export leaks user path");
var importedShare=EffectTransfer.ImportShare(share);
Check(importedShare.AmbienceEnabled && importedShare.AmbienceDecay==700 && importedShare.Extra is null,"sharing import loses effects or extensions");
// Shared profile must transfer ALL spatial options and never export AutoEQ data.
var hrtfShareProfile=new DeviceProfile {
    Name="Private Headphones",Source="Secret headphones measurement",AutoEnabled=true,
    AutoPreamp=-6.2,AutoFilters=[new AutoFilter{Frequency=1000,Gain=3,Q=1}],
    Graphic=[new double[]{100,3}],SurroundEnabled=true,SurroundBinaural=true,
    SurroundStable=false,SurroundBalanced=false,Surround3Enabled=true,
    Surround3Depth=69,Surround3Rear=74,Surround3Front=82,Surround3Separation=67,
    AmbienceEnabled=true,AmbienceRateAuto=true
};
string hrtfJson=EffectTransfer.ExportShare(hrtfShareProfile);
Check(!hrtfJson.Contains("auto_filters") && !hrtfJson.Contains("auto_enabled") &&
      !hrtfJson.Contains("auto_preamp") && !hrtfJson.Contains("\"graphic\"") &&
      !hrtfJson.Contains("Secret headphones measurement"), "sharing leaked AutoEQ");
var spatialRoundtrip=EffectTransfer.ImportShare(hrtfJson);
Check(spatialRoundtrip.SurroundBinaural && spatialRoundtrip.Surround3Enabled &&
      spatialRoundtrip.Surround3Depth==69 && spatialRoundtrip.AmbienceRateAuto,
      "sharing lost HRTF or spatial settings");
var recipient=new DeviceProfile {Name="Recipient headset",AutoEnabled=true,Source="My own AutoEQ",
    AutoPreamp=-5,PreampMode="source",AutoFilters=[new AutoFilter{Frequency=200,Gain=-2,Q=1}]};
var merged=EffectTransfer.MergeIntoDevice(recipient,spatialRoundtrip);
Check(merged.Name=="Recipient headset" && merged.AutoEnabled && merged.Source=="My own AutoEQ" &&
      merged.AutoFilters.Count==1 && merged.AutoFilters[0].Frequency==200 &&
      merged.SurroundBinaural && merged.PreampMode=="source", "shared effects import replaced device AutoEQ");
// v1 compatibility: old shared files may contain AutoEQ. Import must discard it.
string oldShare=hrtfJson.Replace(EffectTransfer.CurrentFormat,EffectTransfer.LegacyFormat);
{
    var legacyRoot=System.Text.Json.Nodes.JsonNode.Parse(oldShare)!.AsObject();
    legacyRoot["profile"]!["auto_enabled"]=true;
    legacyRoot["profile"]!["auto_preamp"]=-9;
    legacyRoot["profile"]!["source"]="old headset";
    legacyRoot["profile"]!["auto_filters"]=System.Text.Json.Nodes.JsonNode.Parse("[ { \"type\": \"PK\", \"freq\": 300, \"gain\": 2, \"q\": 1 } ]");
    oldShare=legacyRoot.ToJsonString();
}
Check(EffectTransfer.IsShare(oldShare), "legacy shared preset no longer recognized");
var legacyImported=EffectTransfer.ImportShare(oldShare);
Check(!legacyImported.AutoEnabled && legacyImported.AutoFilters.Count==0 && legacyImported.Source=="", "legacy sharing retained AutoEQ data");
Console.WriteLine("PASS: Shared effects export/import tests");
var customBands=CompressorThemes.MakeBands("PREMIUM_BALANCE",6).Select(CompressorBand.From).ToList();
var customProfile=new DeviceProfile { Enabled=true,CompressorEnabled=true,CompressorTheme="CUSTOM",CompressorCustomBands=customBands };
Check(ApoRenderer.RenderProfile(customProfile,@"C:\APO\config\InfinityCompressor.vst3").Any(x=>x.Contains("VSTPlugin:")),"CUSTOM 5-band VST parameters missing");
var copy=EffectTransfer.ImportShare(EffectTransfer.ExportShare(customProfile));
Check(copy.CompressorCustomBands.Count==5,"CUSTOM compressor data lost when sharing");
Console.WriteLine("PASS: editable 5-band compressor settings");

// Real effect state must follow enabled checkboxes and the low-latency bypass.
var effectProfile=new DeviceProfile {
    Enabled=true,CompressorEnabled=true,SurroundEnabled=true,SurroundStrength=45,
    SurroundSpread=45,AmbienceEnabled=true,AmbienceStrength=5,LowLatency=false,
    SurroundBinaural=false,Surround3Enabled=false
};
string resourceFolder=Path.Combine(Path.GetTempPath(),"InfinityAudioEqualizerPro-EffectTest");
var activeCommands=ApoRenderer.RenderProfile(effectProfile,Path.Combine(resourceFolder,"InfinityCompressor.vst3"));
Check(activeCommands.Any(x=>x.StartsWith("VSTPlugin:",StringComparison.Ordinal)),"컴프레서 ON 시 VST 명령 누락");
Check(activeCommands.Count(x=>x.StartsWith("Convolution:",StringComparison.Ordinal))==2,"공간 효과 두 종류의 컨볼루션 누락");
effectProfile.LowLatency=true;
var bypassCommands=ApoRenderer.RenderProfile(effectProfile,Path.Combine(resourceFolder,"InfinityCompressor.vst3"));
Check(!bypassCommands.Any(x=>x.StartsWith("VSTPlugin:",StringComparison.Ordinal) || x.StartsWith("Convolution:",StringComparison.Ordinal)),"저지연 모드에서 무거운 효과가 남음");
effectProfile.LowLatency=false;
effectProfile.CompressorEnabled=false;
effectProfile.SurroundEnabled=false;
effectProfile.AmbienceEnabled=false;
var disabledCommands=ApoRenderer.RenderProfile(effectProfile,Path.Combine(resourceFolder,"InfinityCompressor.vst3"));
Check(!disabledCommands.Any(x=>x.StartsWith("VSTPlugin:",StringComparison.Ordinal) || x.StartsWith("Convolution:",StringComparison.Ordinal)),"음향 효과 OFF 이후 필터가 남음");
Console.WriteLine("PASS: 컴프레서/공간 효과 ON-OFF, 저지연 모드에 따른 APO 명령 검증");

// EQ summation must respect enabled states and preserve the user's gain.
var gainCheck=new DeviceProfile{AutoEnabled=true,CustomEnabled=true,ManualPreamp=0};
gainCheck.Graphic.Add([20,3]);gainCheck.Graphic.Add([100,0]);gainCheck.Custom[0]=5;
var twenty=PreampCalculator.ResponseAt(gainCheck,20);
Check(Math.Abs(twenty.AutoEqDb-3)<.001 && Math.Abs(twenty.UserEqDb-5)<.001 && Math.Abs(twenty.CombinedDb-8)<.001,
    "AutoEQ and custom bass boosts must add, not overwrite");
Check(PreampCalculator.AnalyzePeak(gainCheck).PeakDb>=7.9,"Combined EQ peak should reflect added gain");
gainCheck.CustomEnabled=false;
Check(Math.Abs(PreampCalculator.ResponseAt(gainCheck,20).CombinedDb-3)<.001,"Custom EQ OFF leaves AutoEQ alone");
gainCheck.AutoEnabled=false;
Check(Math.Abs(PreampCalculator.ResponseAt(gainCheck,20).CombinedDb)<.001,"AutoEQ OFF must remove graphic response");
Check(gainCheck.PreampMode=="manual" && Math.Abs(PreampCalculator.Calculate(gainCheck).Final)<.001,"Diagnostics changed preamp state");
Check(new DeviceProfile().ManualPreamp == -5,"Python default profile preamp -5 dB parity");
// Python parity: no separate preamp enable flag; mode and device EQ enable control output.
var parity = new DeviceProfile { Enabled=true, AutoEnabled=true, AutoPreamp=-5.5, ManualPreamp=-1.0, PreampMode="source" };
Check(Math.Abs(PreampCalculator.Calculate(parity).Final + 6.5) < .0001,"Python source+user preamp parity");
parity.PreampMode="manual";
Check(Math.Abs(PreampCalculator.Calculate(parity).Final + 1.0) < .0001,"Python manual preamp parity");
parity.Enabled=false;
Check(Math.Abs(PreampCalculator.Calculate(parity).Final) < .0001,"Disabled device profile must be neutral");
// Regression: legacy preamp_enabled=false must not suppress a selected preamp mode.
// AutoEQ source attenuation is only relevant while AutoEQ is enabled.
var legacyJson = "{\"preamp_enabled\":false,\"enabled\":true,\"auto_enabled\":true,\"preamp_mode\":\"source\",\"auto_preamp\":-4,\"manual_preamp\":0}";
var legacyProfile=System.Text.Json.JsonSerializer.Deserialize<DeviceProfile>(legacyJson, SettingsStore.JsonOptions)!;
Check(legacyProfile.AutoEnabled, "Legacy compatibility fixture must explicitly enable AutoEQ");
Check(Math.Abs(PreampCalculator.Calculate(legacyProfile).Final+4)<.0001,"Legacy C# preamp checkbox value must not suppress Python-compatible preamp");
legacyProfile.AutoEnabled=false;
Check(Math.Abs(PreampCalculator.Calculate(legacyProfile).Final)<.0001,"AutoEQ OFF must not apply an old AutoEQ source attenuation");
legacyProfile.PreampMode="manual";
legacyProfile.ManualPreamp=-3;
Check(Math.Abs(PreampCalculator.Calculate(legacyProfile).Final+3)<.0001,"Legacy C# checkbox value must not suppress manually selected preamp");
Console.WriteLine("PASS: EQ response diagnostics and low-frequency summation tests");

// Python Surround Stage mode parity: distinct APO processing pipelines.
// Do not infer actual audio-driver/VST execution from a generated config file.
var spatialMode=new DeviceProfile {
    Enabled=true, ManualPreamp=0, SurroundEnabled=true, SurroundStrength=45,
    SurroundSpread=45, SurroundMode="cinema", AmbienceRate=48000,
    SurroundBinaural=false,Surround3Enabled=false
};
string testPlugin=Path.Combine(Path.GetTempPath(),"InfinityCompressor.vst3");
var normalStage=ApoRenderer.RenderProfile(spatialMode,testPlugin);
Check(normalStage.Any(x=>x.StartsWith("Convolution: infinity_surround_stage_v2_")),
    "basic Surround Stage must use its own FIR");
spatialMode.SurroundBinaural=true; spatialMode.SurroundStable=true;
var stableStage=ApoRenderer.RenderProfile(spatialMode,testPlugin);
Check(stableStage.Any(x=>x.StartsWith("Copy:")) && !stableStage.Any(x=>x.StartsWith("Convolution:")),
    "stable mode must remain dry and avoid reverberant convolution");
spatialMode.SurroundStable=false; spatialMode.SurroundBalanced=true;
var balancedStage=ApoRenderer.RenderProfile(spatialMode,testPlugin);
Check(balancedStage.Any(x=>x.Contains("infinity_balanced_hrtf_v1_")),
    "balanced mode must use its dedicated FIR");
spatialMode.SurroundBalanced=false;
var hrtfStage=ApoRenderer.RenderProfile(spatialMode,testPlugin);
Check(hrtfStage.Count(x=>x.StartsWith("Convolution:"))==2 &&
      hrtfStage.Any(x=>x.Contains("infinity_parallel_hrtf_v5_")),
    "binaural HRTF mode must generate 2 convolution stages");
spatialMode.Surround3Enabled=true;
var combinedStage=ApoRenderer.RenderProfile(spatialMode,testPlugin);
Check(combinedStage.Count(x=>x.StartsWith("Convolution:"))==4 &&
    combinedStage.Any(x=>x.Contains("infinity_stage3_v6_")),
    "Stage3 measured front/rear HRTF must be an additive stage");
spatialMode.LowLatency=true;
Check(!ApoRenderer.RenderProfile(spatialMode,testPlugin).Any(x=>x.StartsWith("Convolution:")),
    "Low latency must bypass all generated HRTF convolutions");
spatialMode.LowLatency=false;
spatialMode.SurroundEnabled=false;
Check(!ApoRenderer.RenderProfile(spatialMode,testPlugin).Any(x=>x.StartsWith("Convolution:")),
    "Surround OFF must remove all HRTF commands");
spatialMode.SurroundEnabled=true;
var hrtfTemp=Path.Combine(Path.GetTempPath(),"InfinityAudioEqualizerPro-HRTFTest-"+Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(hrtfTemp);
    BinauralStageService.Ensure(hrtfTemp,spatialMode);
    HrtfStageService.Ensure(hrtfTemp,spatialMode);
    var referenced=combinedStage.Where(x=>x.StartsWith("Convolution:"))
        .Select(x=>x["Convolution:".Length..].Trim()).ToList();
    foreach(var file in referenced)
    {
        string path=Path.Combine(hrtfTemp,file);
        Check(File.Exists(path) && new FileInfo(path).Length>44,"HRTF missing: "+file);
        using var stream=File.OpenRead(path);
        var tag=new byte[4];
        Check(stream.Read(tag)==4 && System.Text.Encoding.ASCII.GetString(tag)=="RIFF", "HRTF bad WAV: "+file);
    }
    // FIRs are immutable cache artifacts: generating the same mode twice must not replace them.
    var lastModified=referenced.ToDictionary(x=>x,x=>File.GetLastWriteTimeUtc(Path.Combine(hrtfTemp,x)));
    BinauralStageService.Ensure(hrtfTemp,spatialMode);
    HrtfStageService.Ensure(hrtfTemp,spatialMode);
    Check(referenced.All(x=>lastModified[x]==File.GetLastWriteTimeUtc(Path.Combine(hrtfTemp,x))),
        "rapid mode re-apply must not replace an in-use FIR file");
}
finally { if(Directory.Exists(hrtfTemp))Directory.Delete(hrtfTemp,true); }
Console.WriteLine("PASS: Python spatial modes, Stage3 HRTF WAVs, low-latency bypass and cached FIRs");

// The AutoEQ preview mirrors Python: it shows saved filters and saved 31-band
// adjustments independently from their current ON/OFF switches.
var previewProfile=new DeviceProfile { Enabled=false, AutoEnabled=false, CustomEnabled=false,ManualPreamp=0 };
previewProfile.Graphic.Add([20,3]);
previewProfile.Graphic.Add([100,0]);
previewProfile.AutoFilters.Add(new AutoFilter { Type="PK", Frequency=1000,Gain=2,Q=1 });
previewProfile.Custom[0]=5;
var previewValue=PreampCalculator.PreviewResponseAt(previewProfile,20);
Check(previewValue.AutoEqDb > 2.9 && previewValue.UserEqDb == 5,
    "AutoEQ comparison chart must show stored, inactive EQ curves");
Check(Math.Abs(previewValue.CombinedDb-previewValue.AutoEqDb-previewValue.UserEqDb)<.00001,
    "combined AutoEQ chart curve must equal the sum of its source curves");
Console.WriteLine("PASS: AutoEQ / 31-band / combined preview (switch independent)");

// UI parity / backend invariants for the updated controls.
var defaultAmbience=new DeviceProfile();
Check(defaultAmbience.AmbienceRateAuto, "New device profiles must use automatic output-rate detection");
var rateJson=System.Text.Json.JsonSerializer.Serialize(defaultAmbience, SettingsStore.JsonOptions);
Check(rateJson.Contains("\"ambience_rate_auto\": true",StringComparison.Ordinal),
    "Automatic sample rate must survive a settings export");
defaultAmbience.AmbienceRateAuto=false;
defaultAmbience.AmbienceRate=96000;
var manualRate=System.Text.Json.JsonSerializer.Deserialize<DeviceProfile>(
    System.Text.Json.JsonSerializer.Serialize(defaultAmbience,SettingsStore.JsonOptions),SettingsStore.JsonOptions)!;
Check(!manualRate.AmbienceRateAuto && manualRate.AmbienceRate==96000,
    "Manual sample-rate override must survive save/reload");

var comparison=new DeviceProfile
{
    Enabled=true,AutoEnabled=true,CustomEnabled=true,ManualPreamp=-8,
    SurroundEnabled=true,Surround3Enabled=true,CompressorEnabled=true,
    AmbienceEnabled=true,DepthComparisonMode="front",SurroundStrength=45,AmbienceRate=44100
};
comparison.Custom[17]=8;
var compareLines=ApoRenderer.RenderProfile(comparison,Path.Combine(Path.GetTempPath(),"InfinityCompressor.vst3"));
Check(compareLines.Count(x=>x.StartsWith("Convolution:"))==1 &&
      compareLines.Any(x=>x.Contains("_compare_")) &&
      !compareLines.Any(x=>x.StartsWith("GraphicEQ:") || x.StartsWith("Filter:") ||
                           x.StartsWith("VSTPlugin:") || x.StartsWith("Preamp: -8")),
    "Front/rear A/B must isolate one measured virtual source without EQ or dynamics");
Check(HrtfStageService.ComparisonFile(comparison).Contains("_front.wav"),"Front comparison filename");
var comparisonFolder=Path.Combine(Path.GetTempPath(),"InfinityAudioEqualizerPro-StageComparisonTest-"+Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(comparisonFolder);
    HrtfStageService.Ensure(comparisonFolder,comparison);
    var frontPath=Path.Combine(comparisonFolder,HrtfStageService.ComparisonFile(comparison));
    Check(File.Exists(frontPath)&&new FileInfo(frontPath).Length>44,"Front measured HRIR WAV missing");
    comparison.DepthComparisonMode="rear";
    HrtfStageService.Ensure(comparisonFolder,comparison);
    var rearPath=Path.Combine(comparisonFolder,HrtfStageService.ComparisonFile(comparison));
    Check(File.Exists(rearPath)&&new FileInfo(rearPath).Length>44,"Rear measured HRIR WAV missing");
    Check(!File.ReadAllBytes(frontPath).SequenceEqual(File.ReadAllBytes(rearPath)),
        "Front and rear must use different measured HRIRs");
}
finally { if(Directory.Exists(comparisonFolder))Directory.Delete(comparisonFolder,true); }
var comparisonJson=System.Text.Json.JsonSerializer.Serialize(comparison,SettingsStore.JsonOptions);
Check(!comparisonJson.Contains("DepthComparisonMode",StringComparison.Ordinal) &&
      !comparisonJson.Contains("comparison",StringComparison.OrdinalIgnoreCase),
    "Temporary A/B comparison must never become persistent preset data");
comparison.DepthComparisonMode="";
Check(ApoRenderer.RenderProfile(comparison,Path.Combine(Path.GetTempPath(),"InfinityCompressor.vst3"))
        .Any(x=>x.StartsWith("VSTPlugin:")),
    "A/B comparison OFF must restore the full original audio chain");
Check(CompressorDetailsDialog.SliderFromValue(0,120)<CompressorDetailsDialog.SliderFromValue(0,8500),
    "Compressor logarithmic frequency slider must preserve frequency order");
Console.WriteLine("PASS: automatic sample rate settings, directional comparison, compressor slider mappings");

// Device endpoint switching must use the new Windows default and preserve
// independent saved profiles / AutoEQ files for the old and new endpoints.
var endpoints=new List<AudioDevice>
{
    new("speaker-1","Sound Card","{A}"),
    new("buds-2","Bluetooth Headphones","{B}")
};
Check(DeviceSelectionPolicy.ChooseIndex(endpoints,"speaker-1","buds-2","speaker-1")==1,
    "Bluetooth default endpoint change must automatically switch to the new endpoint");
Check(DeviceSelectionPolicy.ChooseIndex(endpoints,"speaker-1","speaker-1","speaker-1")==0,
    "Unchanged Windows default should not override the current selection");
Check(DeviceSelectionPolicy.ChooseIndex(endpoints,"missing","buds-2","speaker-1")==1,
    "Disconnected output must fall back to the active default device");
Check(DeviceSelectionPolicy.Signature(endpoints)!=DeviceSelectionPolicy.Signature(endpoints.Take(1)),
    "Endpoint list change must be detected");
var isolatedProfiles=new AppSettings();
var speaker=new DeviceProfile {Name="speaker",ManualPreamp=-2};
var headset=new DeviceProfile {Name="headset",ManualPreamp=-5};
isolatedProfiles.Profiles["speaker-1"]=speaker;
isolatedProfiles.Profiles["buds-2"]=headset;
AutoEqImporter.Import("Preamp: -6.2 dB\nFilter 1: ON PK Fc 1000 Hz Gain 2 dB Q 1",speaker,"Speaker AutoEQ");
AutoEqImporter.Import("Preamp: -3.8 dB\nFilter 1: ON PK Fc 300 Hz Gain -2 dB Q 1",headset,"Buds AutoEQ");
var snapshot=System.Text.Json.JsonSerializer.Deserialize<AppSettings>(
    System.Text.Json.JsonSerializer.Serialize(isolatedProfiles,SettingsStore.JsonOptions),SettingsStore.JsonOptions)!;
Check(snapshot.Profiles["speaker-1"].Source=="Speaker AutoEQ" &&
      snapshot.Profiles["buds-2"].Source=="Buds AutoEQ" &&
      snapshot.Profiles["speaker-1"].AutoFilters[0].Frequency==1000 &&
      snapshot.Profiles["buds-2"].AutoFilters[0].Frequency==300,
      "Each device must retain its own downloaded AutoEQ after saving and reloading");
AutoEqImporter.Import("Preamp: -4 dB\nFilter 1: ON PK Fc 1800 Hz Gain 1 dB Q 1",speaker,"New Speaker EQ");
Check(headset.Source=="Buds AutoEQ" && headset.AutoFilters[0].Frequency==300,
    "Importing an AutoEQ model on one device must not overwrite another device");
Check(CompressorThemes.DisplayName("OPEN")=="자연스러운 개방감" &&
      CompressorThemes.IdFromDisplayName("입체적 디테일")=="SPATIAL_DETAIL" &&
      CompressorThemes.IdFromDisplayName("사용자 설정")=="CUSTOM",
      "Korean compressor theme names must serialize to original stable preset IDs");
Console.WriteLine("PASS: Bluetooth auto-switch, isolated per-device AutoEQ, Korean compressor themes");

// Transient ambience diagnostic must be audible while it is enabled, must not
// persist in profiles, and must restore the original compressor/EQ chain.
var ambienceDiagnostic=new DeviceProfile
{
    Enabled=true,CompressorEnabled=true,CustomEnabled=true,
    AmbienceEnabled=false,AmbienceRate=44100,ManualPreamp=-5,AmbienceDiagnostic=true
};
ambienceDiagnostic.Custom[17]=4;
var diagLines=ApoRenderer.RenderProfile(ambienceDiagnostic,Path.Combine(Path.GetTempPath(),"InfinityCompressor.vst3"));
Check(diagLines.Count==1 && diagLines[0]=="Convolution: "+ImpulseResponseService.DiagnosticAmbienceFile(ambienceDiagnostic),
    "Temporary reverb test must bypass normal EQ and dynamics");
var diagSettings=new AppSettings {GlobalEnabled=true,BypassAllEffects=false};
diagSettings.Profiles[id]=ambienceDiagnostic;
var diagnosticRoot=Path.Combine(Path.GetTempPath(),"InfinityAudioEqualizerPro-AmbienceTest-"+Guid.NewGuid().ToString("N"));
try
{
    ImpulseResponseService.Ensure(diagnosticRoot,diagSettings);
    string file=Path.Combine(diagnosticRoot,ImpulseResponseService.DiagnosticAmbienceFile(ambienceDiagnostic));
    Check(File.Exists(file) && new FileInfo(file).Length>44,"Temporary reverb WAV must be generated");
}
finally {if(Directory.Exists(diagnosticRoot))Directory.Delete(diagnosticRoot,true);}
string beforeDiag=System.Text.Json.JsonSerializer.Serialize(ambienceDiagnostic,SettingsStore.JsonOptions);
Check(!beforeDiag.Contains("AmbienceDiagnostic",StringComparison.OrdinalIgnoreCase),
    "Temporary reverb diagnostic must not be stored in a user preset");
ambienceDiagnostic.AmbienceDiagnostic=false;
var restoredLines=ApoRenderer.RenderProfile(ambienceDiagnostic,Path.Combine(Path.GetTempPath(),"InfinityCompressor.vst3"));
Check(restoredLines.Any(x=>x.StartsWith("VSTPlugin:")) &&
      restoredLines.Any(x=>x.StartsWith("GraphicEQ:")) &&
      !restoredLines.Any(x=>x.Contains("diagnostic_v3_")),
      "Ending the reverb test must restore all normal effects");
Console.WriteLine("PASS: temporary reverb test, restored audio chain, non-persistent diagnostic state");
