// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Drawing;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
namespace InfinityAudioEqualizerPro;
public sealed class MainForm : Form
{
    private AppSettings _state=SettingsStore.Load();
    private readonly ComboBox _devices=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=350};
    private readonly CheckBox _engine=new(){Text="오디오 엔진 활성화",AutoSize=true};
    private readonly CheckBox _bypassAll=new(){Text="모든 음향 효과 끄기",AutoSize=true,Checked=true};
    private readonly CheckBox _profileEnabled=new(){Text="장치에 음향효과 적용",AutoSize=true};
    private readonly CheckBox _customEnabled=new(){Text="31밴드 EQ 적용",AutoSize=true};
    private readonly CheckBox _autoEnabled=new(){Text="AutoEQ 적용",AutoSize=true};
    private readonly CheckBox _compressorEnabled=new(){Text="5밴드 VST3 컴프레서",AutoSize=true};
    private readonly ComboBox _compressorTheme=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=170};
    private readonly NumericUpDown _compressorStrength=new(){Minimum=1,Maximum=12,Value=4,Width=62};
    private readonly CheckBox _widthEnabled=new(){Text="스테레오 확장",AutoSize=true};
    private readonly TextBox _profileName=new(){Width=260};
    private readonly Label _autoSource=new(){AutoSize=true,MaximumSize=new Size(480,0),ForeColor=Color.FromArgb(170,200,230),Text="AutoEQ 파일 없음"};
    private readonly CheckBox _lowLatency=new(){Text="재생 시작 지연 최소화 · 공간 컨볼루션/5밴드 VST 우회",AutoSize=true};
    private readonly Label _preampSummary=new(){AutoSize=true,ForeColor=Color.FromArgb(170,200,230),Text=""};
    private readonly NumericUpDown _preamp=new(){Minimum=-40,Maximum=20,DecimalPlaces=1,Increment=.5m,Width=85};
    private readonly NumericUpDown _width=new(){Minimum=0,Maximum=100,Width=65};
    private readonly ComboBox _preampMode=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=140};
    private readonly Label _status=new(){AutoSize=true,ForeColor=Color.FromArgb(175,190,205),Text="준비"};
    private readonly List<NumericUpDown> _bands=[];
    private readonly List<SlimSlider> _sliders=[];
    private readonly CheckBox _surroundOn=new(){Text="Surround Stage 사용",AutoSize=true};
    private readonly CheckBox _surround3On=new(){Text="전후 공간감 사용",AutoSize=true};
    private readonly NumericUpDown _balanceStability=new(){Minimum=0,Maximum=100,Value=65};
    private readonly NumericUpDown _balanceSpace=new(){Minimum=0,Maximum=100,Value=80};
    private readonly NumericUpDown _balanceDepth=new(){Minimum=0,Maximum=100,Value=55};
    private readonly NumericUpDown _surround3Depth=new(){Minimum=0,Maximum=100,Value=80,Width=63};
    private readonly NumericUpDown _surround3Rear=new(){Minimum=0,Maximum=100,Value=75,Width=63};
    private readonly NumericUpDown _surround3Front=new(){Minimum=0,Maximum=100,Value=75,Width=63};
    private readonly NumericUpDown _surround3Separation=new(){Minimum=0,Maximum=100,Value=80,Width=63};
    private readonly ComboBox _surroundMode=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=140};
    private readonly ComboBox _surroundProcessing=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=155};
    private readonly NumericUpDown _surroundStrength=new(){Minimum=0,Maximum=100,Width=62};
    private readonly NumericUpDown _surroundCenter=new(){Minimum=0,Maximum=100,Width=62};
    private readonly NumericUpDown _surroundSpread=new(){Minimum=0,Maximum=100,Width=62};
    private readonly CheckBox _ambienceOn=new(){Text="Studio Ambience 사용",AutoSize=true};
    private readonly NumericUpDown _ambienceStrength=new(){Minimum=0,Maximum=10,Width=60};
    private readonly NumericUpDown _ambienceDecay=new(){Minimum=100,Maximum=3000,Increment=50,Width=95};
    private readonly ComboBox _ambienceRate=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=130};
    private readonly CheckBox _startup = new() { Text="Windows 로그인 시 자동 실행", AutoSize=true };
    private readonly CheckBox _staticMode = new() { Text="정적 EQ 모드 · 자동 음량 조정 없이 고정 게인만 사용", AutoSize=true };
    private static readonly int[] AmbienceRates = [44100,48000,88200,96000,176400,192000];
    private AccentButton? _frontCompareButton;
    private AccentButton? _rearCompareButton;
    private AccentButton? _ambienceTestButton;
    private bool _compressorDialogOpen;
    private string _lastChangedEffect="음향 설정";
    private readonly Label _titleLabel = new() {Text="Infinity Audio Equalizer Pro for Windows",AutoSize=true,Font=new Font("Segoe UI",14.5f,FontStyle.Bold),ForeColor=Color.FromArgb(229,241,252),Padding=new Padding(4,3,4,3)};
    private readonly AccentButton _masterButton = new() {Text="전체 효과 OFF", Width=142,Height=34,FlatStyle=FlatStyle.Flat};
    private readonly NotifyIcon _tray = new() { Text="Infinity Audio Equalizer Pro for Windows" };
    private bool _reallyExit;
    private readonly bool _startedFromWindowsLogon;
    private string? _manuallySelectedXtSelector;
    private string? FindXtSelector()
    {
        var installed = XtSetupService.FindDeviceSelector(_state);
        if (installed is not null) return installed;
        // Only the folder explicitly chosen during this process is a fallback.
        if (!string.IsNullOrWhiteSpace(_manuallySelectedXtSelector)
            && File.Exists(_manuallySelectedXtSelector)
            && File.Exists(Path.Combine(Path.GetDirectoryName(_manuallySelectedXtSelector)!, "Editor.exe")))
            return _manuallySelectedXtSelector;
        return null;
    }
    private bool _xtInstallBusy;
    private bool _selectorBusy;
    private bool _apoSetupCompletionBusy;
    private bool _activationPromptBusy;
    private bool _registrationCheckRecommended;
    // The UI text is a second, independently maintained observation. Capture it
    // before ChangeCurrent/ApplyLive can overwrite it during an ON transition.
    private enum ActivationIssue { None, XtMissing, ConfigMissing, BridgeMissing, DeviceNotRegistered, Unknown }
    private sealed record ActivationUiSnapshot(string Status, string BridgeStatus);
    private ActivationUiSnapshot CaptureActivationUi() => new(_status.Text, _bridgeStatus.Text);

    private static bool UiConfirmsIssue(ActivationIssue issue, ActivationUiSnapshot ui)
    {
        // Compare semantic signal words, not exact translated message text.
        // A general OFF or successful file-save message is not corroboration.
        string status = ui.Status;
        string bridge = ui.BridgeStatus;
        return issue switch
        {
            ActivationIssue.XtMissing =>
                status.Contains("설치 필요", StringComparison.OrdinalIgnoreCase)
                || bridge.Contains("APO-XT 설치", StringComparison.OrdinalIgnoreCase),
            ActivationIssue.ConfigMissing =>
                status.Contains("설정 폴더", StringComparison.OrdinalIgnoreCase)
                || bridge.Contains("설정 폴더", StringComparison.OrdinalIgnoreCase)
                || bridge.Contains("설정 경로 선택", StringComparison.OrdinalIgnoreCase),
            ActivationIssue.BridgeMissing =>
                status.Contains("연결 필요", StringComparison.OrdinalIgnoreCase)
                || status.Contains("미연결", StringComparison.OrdinalIgnoreCase)
                || bridge.Contains("연결 필요", StringComparison.OrdinalIgnoreCase)
                || bridge.Contains("미연결", StringComparison.OrdinalIgnoreCase),
            ActivationIssue.DeviceNotRegistered =>
                status.Contains("출력 장치 등록 필요", StringComparison.OrdinalIgnoreCase)
                || bridge.Contains("출력 장치 등록 필요", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private ActivationIssue IndependentlyDiagnoseActivation(out string? locatedRoot)
    {
        locatedRoot = null;
        if (FindXtSelector() is null) return ActivationIssue.XtMissing;
        locatedRoot = ApoBridge.DiscoverConfig(_state);
        if (locatedRoot is null) return ActivationIssue.ConfigMissing;
        // Check endpoint registration separately from config.txt linkage. A successful
        // Infinity Audio Equalizer Pro include is not evidence that Windows has registered the APO.
        var registration = ProbeSelectedEndpoint();
        if (registration == EndpointApoRegistration.Missing) return ActivationIssue.DeviceNotRegistered;
        if (!ApoBridge.IsConnected(locatedRoot)) return ActivationIssue.BridgeMissing;
        return ActivationIssue.None;
    }

    private EndpointApoRegistration ProbeSelectedEndpoint()
    {
        var endpoint = _devices.SelectedItem as AudioDevice;
        if (endpoint is null) return EndpointApoRegistration.Unknown;
        return ApoEndpointRegistration.Read(endpoint.Guid, FindXtSelector());
    }

    private void UpdateApoRegistrationStatus()
    {
        if (FindXtSelector() is null) return;
        var registration = ProbeSelectedEndpoint();
        if (registration == EndpointApoRegistration.Missing)
        {
            _bridgeStatus.Text = "출력 장치 등록 필요";
            _status.Text = "출력 장치 등록이 필요합니다.";
        }
        else if (registration == EndpointApoRegistration.Registered && _connected)
            _bridgeStatus.Text = "APO 설정 연결됨";
    }

    private void ShowActivationCheckRequired(ActivationIssue issue, ActivationUiSnapshot ui)
    {
        // Conflicting internal diagnostics must not appear as speculative customer messages.
        // Keep the visible state based on the requested switch operation only.
        if (issue == ActivationIssue.DeviceNotRegistered)
        {
            _bridgeStatus.Text = "출력 장치 등록 필요";
            _status.Text = "출력 장치 등록이 필요합니다.";
        }
        DiagnosticLog.Error("전체 효과 ON 진단 불일치", new InvalidOperationException(
            $"Issue={issue}; Status={ui.Status}; BridgeStatus={ui.BridgeStatus}"));
    }

    private readonly System.Windows.Forms.Timer _deviceTimer = new() { Interval=1600 };
    private string _deviceSignature="";
    private string _observedDefaultDeviceId="";
    private Panel? _contentPanel;
    private Panel? _mainScroller;
    private UserGuideOverlay? _guideOverlay;
    private readonly ToolTip _featureHelpTip = new()
    {
        InitialDelay = 600,
        ReshowDelay = 150,
        AutoPopDelay = 18000,
        ShowAlways = true
    };
    private bool _earlyHelpOnLabels;
    private static readonly string GuideLaunchCountFile = Path.Combine(SettingsStore.CurrentDirectory, "guide_launch_count.txt");

    // Counts actual process starts, not opening the form from the system tray.
    private void BeginGuideHelpSession()
    {
        try
        {
            int previous = 0;
            if (File.Exists(GuideLaunchCountFile))
                int.TryParse(File.ReadAllText(GuideLaunchCountFile).Trim(), out previous);
            int current = Math.Clamp(previous + 1, 1, 6);
            Directory.CreateDirectory(SettingsStore.CurrentDirectory);
            File.WriteAllText(GuideLaunchCountFile, current.ToString());
            _earlyHelpOnLabels = current <= 5;
        }
        catch (Exception ex)
        {
            // Help must never interrupt audio settings or program startup.
            DiagnosticLog.Error("가이드 실행 횟수 저장", ex);
            _earlyHelpOnLabels = true;
        }
    }

    private readonly Label _advancedNotice=new(){AutoSize=true,ForeColor=Color.FromArgb(177,192,208),Text="음향 효과를 변경하면 현재 출력 장치에 적용됩니다."};

    private string _appliedFingerprint = "";
    private bool _loading;
    private string? _apoRoot;
    private readonly System.Windows.Forms.Timer _applyTimer = new() { Interval = 480 };
    private int _applyRetryCount;
    private readonly ToolTip _bridgePathTip = new();
    private readonly Label _bridgeStatus = new() { AutoSize = true, ForeColor = Color.FromArgb(145, 219, 186), Text = "APO: 확인 중" };
    private bool _connected;
    private bool _pendingChanges;
    private string _selectedId="";
    private readonly Color _bg=Color.FromArgb(21,26,36);
    private readonly Color _panel=Color.FromArgb(31,38,52);
    private readonly Color _fg=Color.FromArgb(230,238,246);
    public MainForm(bool startInTray = false)
    {
        _startedFromWindowsLogon = startInTray;
        Text="Infinity Audio Equalizer Pro for Windows";
        // Match the 12px left/right content margins at the 1140px design width.
        // Height and vertical scrolling behavior are unchanged.
        ClientSize=new Size(1140,1302); MinimumSize=new Size(760,600);
        BackColor=_bg;ForeColor=_fg;Font=new Font("Segoe UI",9);
        StartPosition=FormStartPosition.CenterScreen;
        if (_startedFromWindowsLogon)
        {
            // Keep the initial Show/Load lifecycle (device detection and tray setup)
            // without presenting a main window or taskbar button at logon.
            Opacity = 0;
            ShowInTaskbar = false;
            Shown += (_, _) =>
            {
                Hide();
                Opacity = 1;
                ShowInTaskbar = true; // Normal manual tray restoration uses the taskbar.
            };
        }
        try{Icon=new Icon(Path.Combine(AppContext.BaseDirectory,"assets","Infinity_Audio_Equalizer.ico"));}catch{}
        _preampMode.Items.AddRange(["수동 · 최종값 직접 지정", "AutoEQ 원본 감쇄 + 보정", "자동 감쇄 + 보정"]);
        _compressorTheme.Items.AddRange(CompressorThemes.ThemeIds.Concat(["CUSTOM"])
            .Select(CompressorThemes.DisplayName).ToArray());
        _surroundMode.Items.AddRange(["Natural","Cinema","Live Stage"]);
        _surroundProcessing.Items.AddRange(["기본 공간 확장","원음 안정성 우선","균형형 공간감","HRTF 공간감"]);
        _ambienceRate.Items.AddRange(["자동 감지","44,100 Hz","48,000 Hz","88,200 Hz","96,000 Hz","176,400 Hz","192,000 Hz"]);
        _ambienceRate.SelectedIndex=0;
        BeginGuideHelpSession();
        BuildUi();
        _masterButton.Click += (_,_) => { if (!_engine.Checked) _bypassAll.Checked=false; _engine.Checked=!_engine.Checked; };
        UpgradeExistingStartupCommand();
        _startup.Checked=StartupEnabled();
        _startup.CheckedChanged+=(_,_)=> { if (!_loading) SetStartup(_startup.Checked); };
        _staticMode.CheckedChanged+=(_,_)=> { if (_loading)return; _preampMode.SelectedIndex=_staticMode.Checked?0:2; };
        _deviceTimer.Tick+=(_,_)=>RefreshDevicesIfChanged();
        _tray.Icon=Icon ?? SystemIcons.Application;
        var menu=new ContextMenuStrip();
        menu.Items.Add("창 열기",null,(_,_)=>RestoreWindow());
        menu.Items.Add("프로그램 종료",null,(_,_)=>ExitFromTray());
        _tray.ContextMenuStrip=menu;
        _tray.DoubleClick+=(_,_)=>RestoreWindow();
        _tray.Visible=true;

        _bridgeStatus.Cursor = Cursors.Hand;
        _bridgeStatus.Click += (_,_) => ShowOutputActivationGuidance("APO 설정 관리");
        _profileName.Leave+=(_,_)=>ChangeCurrent();
        _profileName.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;ChangeCurrent();}};
        _lowLatency.CheckedChanged+=(_,_)=>ChangeCurrent("저지연 재생");
        _engine.CheckedChanged+=(_,_)=>
        {
            var uiBeforeActivation = CaptureActivationUi();
            RefreshMasterButton();ChangeCurrent("전체 음향 효과");
            if (!_loading && _engine.Checked && !_bypassAll.Checked)
                BeginInvoke((Action)(() => PromptForMissingApoOnActivation(uiBeforeActivation)));
        };
        _bypassAll.CheckedChanged+=(_,_)=>
        {
            var uiBeforeActivation = CaptureActivationUi();
            RefreshMasterButton();ChangeCurrent("전체 음향 효과");
            if (!_loading && !_bypassAll.Checked && _engine.Checked)
                BeginInvoke((Action)(() => PromptForMissingApoOnActivation(uiBeforeActivation)));
        };
        _profileEnabled.CheckedChanged+=(_,_)=>ChangeCurrent("장치 음향 효과");
        _customEnabled.CheckedChanged+=(_,_)=>ChangeCurrent("31밴드 EQ");
        _autoEnabled.CheckedChanged+=(_,_)=>ChangeCurrent("AutoEQ");
        _widthEnabled.CheckedChanged+=(_,_)=>ChangeCurrent("스테레오 확장");
        _compressorEnabled.CheckedChanged+=(_,_)=>ChangeCurrent("5밴드 컴프레서");
        _compressorTheme.SelectedIndexChanged+=(_,_)=>ChangeCurrent("5밴드 컴프레서");
        _compressorStrength.ValueChanged+=(_,_)=>ChangeCurrent("5밴드 컴프레서");
        _preamp.ValueChanged+=(_,_)=>ChangeCurrent("프리앰프");
        _width.ValueChanged+=(_,_)=>ChangeCurrent("스테레오 확장");
        _preampMode.SelectedIndexChanged+=(_,_)=>{if(_loading)return;_loading=true;_staticMode.Checked=_preampMode.SelectedIndex==0;_loading=false;ChangeCurrent("프리앰프");};
        _surroundOn.CheckedChanged+=(_,_)=>ChangeCurrent("Surround Stage");
        _surround3On.CheckedChanged+=(_,_)=>ChangeCurrent("Surround Stage");
        _ambienceOn.CheckedChanged+=(_,_)=>ChangeCurrent("Studio Ambience");
        foreach(var num in new[]{_surroundStrength,_surroundCenter,_surroundSpread,_surround3Depth,_surround3Rear,_surround3Front,_surround3Separation,_balanceStability,_balanceSpace,_balanceDepth})num.ValueChanged+=(_,_)=>ChangeCurrent("Surround Stage");
        foreach(var num in new[]{_ambienceStrength,_ambienceDecay})num.ValueChanged+=(_,_)=>ChangeCurrent("Studio Ambience");
        _surroundMode.SelectedIndexChanged+=(_,_)=>ChangeCurrent("Surround Stage");
        _ambienceRate.SelectedIndexChanged+=(_,_)=>ChangeCurrent("Studio Ambience");
        _surroundProcessing.SelectedIndexChanged+=(_,_)=>ChangeCurrent("Surround Stage");
        _devices.SelectedIndexChanged+=(_,_)=>SelectDevice();
        _applyTimer.Tick += (_,_) => { _applyTimer.Stop(); ApplyLive(); };
        Load+=(_,_)=>
        {
            RefreshDevices(); CheckBridge(); RefreshMasterButton(); _deviceTimer.Start();
            // Do not interrupt Windows logon with a setup/repair dialog.
            // The regular first-run guidance remains available on manual launch.
            if (!_startedFromWindowsLogon)
                BeginInvoke((Action)(() => ShowInitialApoGuidance()));
        };
        // The main content keeps a stable design width; narrower windows scroll horizontally.
        // Avoid resizing AutoSize containers, which can collapse a TopDown WinForms layout.
        FormClosing+=(_,e)=> { if(!_reallyExit && e.CloseReason==CloseReason.UserClosing && _tray.Visible){ e.Cancel=true;Hide();return;} };
        FormClosed+=(_,_)=>{_guideOverlay?.Dispose();_deviceTimer.Dispose();_bridgePathTip.Dispose();_featureHelpTip.Dispose();_tray.Visible=false;_tray.Dispose();};
        FormClosing+=(_,e)=>
        {
            if(e.Cancel)return;
            _applyTimer.Stop();
            _deviceTimer.Stop();
            // Closing is terminal: never run ApplyLive() here, since it can
            // re-enable effects after the tray exit has attempted a bypass.
            if(_reallyExit)
            {
                try { SettingsStore.Save(_state); }
                catch(Exception ex) { DiagnosticLog.Error("종료 중 설정 저장",ex); }
            }
        };
    }
    // Layout follows the Python/PySide6 application screenshot at a 1140-pixel design width.
    // Stable absolute coordinates avoid recursive WinForms AutoSize/Dock width collapse.
    // Smaller displays scroll; controls do not compress into single-character columns.
    private readonly List<Label> _bandReadouts=[];
    private Label? _widthReadout;
    private Label? _rateReadout;
    private bool _startupGuideShown;
    private Label? _deviceHint;

    private Label UiText(string text, int x, int y, int width, int height=24, bool strong=false, Color? tint=null)
    {
        return new Label { Text=text, Location=new Point(x,y), Size=new Size(width,height),
            TextAlign=ContentAlignment.MiddleLeft, ForeColor=tint??_fg,
            Font=new Font("Segoe UI",strong?9.5f:9f,strong?FontStyle.Bold:FontStyle.Regular),
            AutoEllipsis=true, BackColor=Color.Transparent };
    }
    private Button UiAction(string text, Action handler, int x, int y, int width, int height=29)
    {
        var b=new AccentButton { Text=text, Location=new Point(x,y), Size=new Size(width,height),
            BackColor=Color.FromArgb(44,71,112), ForeColor=Color.White,
            FlatStyle=FlatStyle.Flat, UseVisualStyleBackColor=false, Font=new Font("Segoe UI",8.6f) };
        b.FlatAppearance.BorderColor=Color.FromArgb(55,83,127);
        b.Click+=(_,_)=>{try{handler();}catch(Exception ex){ShowError(ex);}};
        return b;
    }
    private GroupBox UiSection(string title, int y, int height)
    {
        return new SubtleGroupBox { Text="  "+title+"  ", Location=new Point(12,y), Size=new Size(1116,height),
            ForeColor=Color.FromArgb(221,235,250),BackColor=_bg,Font=new Font("Segoe UI",9f,FontStyle.Bold),
            Padding=new Padding(9,20,9,8) };
    }
    private void UiStyle(Control c, int x, int y, int w, int h)
    {
        c.Location=new Point(x,y); c.Size=new Size(w,h);
        c.AutoSize=false; c.Anchor=AnchorStyles.Top|AnchorStyles.Left;
        c.Font=new Font("Segoe UI",9f);
        c.ForeColor=_fg;
        if(c is TextBox tb){tb.BorderStyle=BorderStyle.FixedSingle;tb.BackColor=Color.FromArgb(34,44,62);}
        if(c is ComboBox cb){cb.BackColor=Color.FromArgb(36,46,64);cb.ForeColor=_fg;cb.FlatStyle=FlatStyle.Flat;}
        if(c is NumericUpDown nu){nu.BackColor=Color.FromArgb(39,52,71);nu.ForeColor=Color.White;nu.BorderStyle=BorderStyle.FixedSingle;}
        if(c is CheckBox box)
        {
            box.UseVisualStyleBackColor=false;
            box.BackColor=Color.Transparent;
            box.ForeColor=_fg;
            box.TextAlign=ContentAlignment.MiddleLeft;
            box.CheckAlign=ContentAlignment.MiddleLeft;
            // A 970px CheckBox reacts to clicks even on its empty right-hand side.
            // Restrict the hit target to the actual checkbox label, not its layout slot.
            int labelWidth=TextRenderer.MeasureText(box.Text ?? string.Empty,box.Font,
                new Size(int.MaxValue,h),TextFormatFlags.SingleLine|TextFormatFlags.NoPadding).Width;
            box.Width=Math.Max(24,Math.Min(w,labelWidth+28));
        }
        if(c is Button btn){btn.UseVisualStyleBackColor=false;btn.FlatStyle=FlatStyle.Flat;
            btn.BackColor=Color.FromArgb(44,71,112);btn.ForeColor=Color.White;
            btn.FlatAppearance.BorderColor=Color.FromArgb(60,91,135);}
    }
    private static void Put(Control parent, Control child)=>parent.Controls.Add(child);

    private void BuildUi()
    {
        var scroller=new Panel { Dock=DockStyle.Fill,AutoScroll=true,BackColor=_bg };
        _mainScroller=scroller;
        Controls.Add(scroller);
        var canvas=new Panel { Location=Point.Empty, Size=new Size(1140,1262), BackColor=_bg };
        _contentPanel=canvas;
        scroller.Controls.Add(canvas);
        var appHeading=UiText("Infinity Audio Equalizer Pro for Windows",16,6,870,40,true,Color.FromArgb(116,172,255));
        appHeading.Font=new Font("Segoe UI",18f,FontStyle.Bold);
        Put(canvas,appHeading);
        Put(canvas,UiAction("사용자 가이드",ShowUserGuide,910,24,125,28));
        Put(canvas,UiAction("버전 정보",ShowAbout,1042,24,88,28));
        Put(canvas,UiText("AutoEQ 보정 + 31밴드 사용자 EQ  |  장치별 설정은 Equalizer APO-XT가 직접 처리",12,53,980,22));
        Put(canvas,UiText("출력 장치",12,84,60));
        UiStyle(_devices,72,82,805,29);Put(canvas,_devices);
        Put(canvas,UiAction("장치 새로고침",RefreshDevices,885,82,96));
        UiStyle(_masterButton,989,82,142,30);Put(canvas,_masterButton);
        _status.Location=new Point(12,116);_status.Size=new Size(1118,23);_status.AutoSize=false;
        _status.BackColor=Color.FromArgb(33,43,57);_status.ForeColor=Color.FromArgb(167,216,184);
        _status.TextAlign=ContentAlignment.MiddleLeft;Put(canvas,_status);

        var profile=UiSection("선택 장치 프리셋",145,253);Put(canvas,profile);
        Put(profile,UiText("프리셋 이름",19,26,100));UiStyle(_profileName,125,30,974,23);
        // Use the native one-line edit height, vertically centred within the 29px row.
        _profileName.AutoSize=true;
        _profileName.TextAlign=HorizontalAlignment.Left;
        Put(profile,_profileName);
        Put(profile,UiText("장치 음향 효과",19,60,98));_profileEnabled.Text="장치에 음향효과 적용";UiStyle(_profileEnabled,125,60,290,26);Put(profile,_profileEnabled);
        UiStyle(_autoEnabled,450,60,287,26);Put(profile,_autoEnabled);
        UiStyle(_customEnabled,775,60,300,26);_customEnabled.Text="추가 EQ 적용";Put(profile,_customEnabled);
        Put(profile,UiText("자동 보정",19,92,95));
        _autoSource.AutoSize=false;_autoSource.Location=new Point(125,92);_autoSource.Size=new Size(478,28);
        _autoSource.TextAlign=ContentAlignment.MiddleLeft;Put(profile,_autoSource);
        Put(profile,UiAction("AutoEQ 곡선 보기",ShowAutoEqCurve,611,90,120));
        Put(profile,UiAction("AutoEQ 파일 가져오기",ImportAutoEq,735,90,138));
        Put(profile,UiAction("기기명 검색 · AutoEQ 적용",SearchAutoEq,877,90,180));
        Put(profile,UiAction("제거",ClearAutoEq,1060,90,43));
        Put(profile,UiText("최종 프리앰프",19,127,104));
        UiStyle(_preampMode,125,126,175,28);Put(profile,_preampMode);
        UiStyle(_preamp,307,126,78,28);Put(profile,_preamp);
        _preampSummary.AutoSize=false;_preampSummary.Location=new Point(393,127);_preampSummary.Size=new Size(703,25);
        _preampSummary.TextAlign=ContentAlignment.MiddleLeft;Put(profile,_preampSummary);
        var preampWarning=UiText(
            "소리가 작으면 프리앰프를 올려주세요. 너무 높이면 소리가 찢어지거나 클리핑 노이즈가 발생할 수 있습니다.",
            125,155,965,22,false,Color.FromArgb(188,199,213));
        Put(profile,preampWarning);
        Put(profile,UiText("음량 출렁임 방지",19,183,107));
        UiStyle(_staticMode,125,181,975,27);Put(profile,_staticMode);
        Put(profile,UiText("저지연 재생",19,214,104));
        UiStyle(_lowLatency,125,213,970,27);Put(profile,_lowLatency);

        var w=UiSection("스테레오 확장  ·  Stereo Width",413,63);Put(canvas,w);
        _widthEnabled.Text="스테레오 확장 사용";UiStyle(_widthEnabled,17,29,185,20);
        _widthEnabled.TextAlign=ContentAlignment.MiddleLeft;
        _widthEnabled.CheckAlign=ContentAlignment.MiddleLeft;
        Put(w,_widthEnabled);
        Put(w,UiText("확장 강도",200,25,67));
        var track=new SlimSlider{Location=new Point(268,23),Size=new Size(770,30),Minimum=0,Maximum=100,Orientation=System.Windows.Forms.Orientation.Horizontal,Value=0};
        track.ValueChanged+=(_,_)=>{ if(_width.Value!=track.Value)_width.Value=track.Value; };
        _width.ValueChanged+=(_,_)=>{if(track.Value!=(int)_width.Value)track.Value=(int)_width.Value;if(_widthReadout is not null)_widthReadout.Text=_width.Value+"%";};
        Put(w,track);_widthReadout=UiText("0%",1041,25,55);Put(w,_widthReadout);
        _width.Location=new Point(900,3);_width.Visible=false;Put(w,_width);

        var sr=UiSection("Infinity Audio Equalizer Pro Surround Stage  ·  가상 공간 음향",491,212);Put(canvas,sr);
        UiStyle(_surroundOn,17,25,145,29);Put(sr,_surroundOn);
        Put(sr,UiText("처리 방식",200,25,65));UiStyle(_surroundProcessing,265,25,155,28);Put(sr,_surroundProcessing);
        Put(sr,UiText("모드",429,25,34));UiStyle(_surroundMode,467,25,100,28);Put(sr,_surroundMode);
        Put(sr,UiText("입체감",575,25,53));UiStyle(_surroundStrength,632,25,65,28);Put(sr,_surroundStrength);
        Put(sr,UiText("보컬 중심",705,25,66));UiStyle(_surroundCenter,773,25,64,28);Put(sr,_surroundCenter);
        Put(sr,UiText("측면 확산",845,25,66));UiStyle(_surroundSpread,913,25,65,28);Put(sr,_surroundSpread);
        var dir=new SubtleGroupBox{Text="  전후 좌우 방향 분리  ",Location=new Point(15,69),Size=new Size(766,127),
            ForeColor=_fg,BackColor=_bg};Put(sr,dir);
        UiStyle(_surround3On,17,25,300,28);Put(dir,_surround3On);
        _frontCompareButton=(AccentButton)UiAction("전방 비교",()=>ToggleDepthComparison("front"),576,25,80);
        Put(dir,_frontCompareButton);
        _rearCompareButton=(AccentButton)UiAction("후방 비교",()=>ToggleDepthComparison("rear"),660,25,82);
        Put(dir,_rearCompareButton);
        string[] settings={"전후 깊이","후방 공간감","전방 음상 거리","방향 분리 강도"};
        int[] px={17,194,379,563};
        NumericUpDown[] stageControls=[_surround3Depth,_surround3Rear,_surround3Front,_surround3Separation];
        for(int i=0;i<4;i++){
            Put(dir,UiText(settings[i],px[i],63,i==3?100:95));
            UiStyle(stageControls[i],px[i]+(i==3?105:95),63,63,26);
            Put(dir,stageControls[i]);
        }
        var balance=new SubtleGroupBox{Text="  균형형 공간감 상세 설정 · 균형형 전용  ",Location=new Point(792,69),Size=new Size(307,127),
            ForeColor=_fg,BackColor=_bg};Put(sr,balance);
        string[] bl={"원음 안정성","공간 확장","전후 깊이"};
        NumericUpDown[] balanceControls=[_balanceStability,_balanceSpace,_balanceDepth];
        for(int i=0;i<3;i++){
            Put(balance,UiText(bl[i],18,28+30*i,130));
            UiStyle(balanceControls[i],209,27+30*i,75,27);
            Put(balance,balanceControls[i]);
        }

        var amb=UiSection("Infinity Audio Equalizer Pro Studio Ambience  ·  스테레오 공간 잔향",719,65);Put(canvas,amb);
        UiStyle(_ambienceOn,15,25,145,28);Put(amb,_ambienceOn);
        Put(amb,UiText("강도",213,25,40));UiStyle(_ambienceStrength,254,25,70,28);Put(amb,_ambienceStrength);
        Put(amb,UiText("잔향 길이",334,25,65));UiStyle(_ambienceDecay,402,25,93,28);Put(amb,_ambienceDecay);
        Put(amb,UiText("출력 샘플레이트",503,25,103));UiStyle(_ambienceRate,607,25,135,28);Put(amb,_ambienceRate);
        _rateReadout=UiText("",747,25,150);Put(amb,_rateReadout);
        _ambienceRate.SelectedIndexChanged+=(_,_)=>{if(!_loading)UpdateSelectedSampleRate(false);};
        _ambienceTestButton=(AccentButton)UiAction("잔향 적용 테스트",ToggleAmbienceTest,923,25,130);
        Put(amb,_ambienceTestButton);

        var cmp=UiSection("5밴드 컴프레서  ·  음향 테마",800,64);Put(canvas,cmp);
        _compressorEnabled.Text="컴프레서 사용";UiStyle(_compressorEnabled,16,24,145,29);Put(cmp,_compressorEnabled);
        Put(cmp,UiText("음향 테마",168,25,70));UiStyle(_compressorTheme,243,24,246,29);Put(cmp,_compressorTheme);
        Put(cmp,UiText("테마 강도",500,25,73));UiStyle(_compressorStrength,577,24,71,29);Put(cmp,_compressorStrength);
        Put(cmp,UiText("5개 대역 독립 압축 · 스테레오 연동",661,25,295));
        Put(cmp,UiAction("5밴드 상세 조절",ShowCompressorDetails,975,24,128));

        var eq=UiSection("사용자 31밴드 EQ  ·  -12 ~ +12 dB",879,260);Put(canvas,eq);
        var eqScroll=new Panel{Location=new Point(14,26),Size=new Size(1088,182),AutoScroll=true,BackColor=_bg};Put(eq,eqScroll);
        var eqCanvas=new Panel{Location=new Point(0,0),Size=new Size(1085,175),BackColor=_bg};Put(eqScroll,eqCanvas);
        for(int i=0;i<ApoRenderer.Bands.Length;i++)
        {
            int x=4+i*35;
            var valText=UiText("+0.0",x,0,34,18,false,Color.White);
            valText.TextAlign=ContentAlignment.MiddleCenter;
            valText.Font=new Font("Segoe UI",7.3f,FontStyle.Bold);Put(eqCanvas,valText);
            var slider=new SlimSlider{Minimum=-120,Maximum=120,
                Orientation=System.Windows.Forms.Orientation.Vertical,Location=new Point(x+2,18),Size=new Size(31,130),Value=0};
            Put(eqCanvas,slider);
            double hz=ApoRenderer.Bands[i];string text=hz>=1000?(hz/1000).ToString("0.##")+"k":hz.ToString("0.#");
            var hzText=UiText(text,x-2,149,37,21,false,Color.FromArgb(225,236,251));
            hzText.TextAlign=ContentAlignment.MiddleCenter;hzText.Font=new Font("Segoe UI",7.7f);Put(eqCanvas,hzText);
            var val=new NumericUpDown{Minimum=-12,Maximum=12,DecimalPlaces=1,Increment=.1m,Visible=false,Value=0};
            Put(eqCanvas,val);
            slider.ValueChanged+=(_,_)=>{if(_loading)return;decimal n=slider.Value/10m;if(val.Value!=n)val.Value=n;};
            val.ValueChanged+=(_,_)=>{ int n=(int)(val.Value*10);if(slider.Value!=n)slider.Value=n;
                valText.Text=val.Value>=0?"+"+val.Value.ToString("0.0"):val.Value.ToString("0.0");
                if(!_loading)ChangeCurrent("31밴드 EQ"); };
            int bandIndex=i;
            void EditBand(object? _,EventArgs __){
                using var popup=new Form{Text=$"{ApoRenderer.Bands[bandIndex]:0.##} Hz · EQ 조절",Size=new Size(280,145),
                    FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,StartPosition=FormStartPosition.CenterParent,
                    BackColor=_bg,ForeColor=_fg};
                var edit=new NumericUpDown{Minimum=-12,Maximum=12,Value=val.Value,DecimalPlaces=1,Increment=.1m,
                    Location=new Point(22,22),Size=new Size(115,28),BackColor=Color.FromArgb(38,49,67),ForeColor=Color.White};
                var ok=UiAction("적용",()=>{val.Value=edit.Value;popup.DialogResult=DialogResult.OK;popup.Close();},155,21,80);
                Put(popup,edit);Put(popup,ok);popup.ShowDialog(this);
            }
            valText.DoubleClick+=EditBand;hzText.DoubleClick+=EditBand;
            _bands.Add(val);_sliders.Add(slider);_bandReadouts.Add(valText);
        }
        Put(eq,UiAction("31밴드 EQ 초기화",ResetEq,20,214,123));
        Put(eq,UiText("수치 직접 입력: 밴드 상단 dB 또는 주파수 이름을 더블클릭",156,216,630));

        // The Python UI uses two horizontal command rows below the EQ.
        var first=new Panel{Location=new Point(12,1156),Size=new Size(1116,35),BackColor=_bg};Put(canvas,first);
        UiStyle(_startup,4,3,195,29);Put(first,_startup);
        Put(first,UiAction("EqualizerAPO-XT 설치",InstallXt,203,2,222));
        Put(first,UiAction("Equalizer APO 출력 장치 등록",OpenDeviceSelector,430,2,222));
        Put(first,UiAction("Equalizer APO 경로",ChooseApoPath,657,2,222));
        var apoConnectButton=UiAction("Equalizer APO 연결 / 다시 적용",()=>EnsureConnectedAndApply(),884,2,222);
        var apoMenu=new ContextMenuStrip();
        apoMenu.Items.Add("Equalizer APO 연결 해제",null,(_,_)=>{try{DisconnectApo();}catch(Exception ex){ShowError(ex);}});
        apoMenu.Items.Add("Equalizer APO 연결 재구성",null,(_,_)=>{try{RepairApo();}catch(Exception ex){ShowError(ex);}});
        apoConnectButton.ContextMenuStrip=apoMenu;
        Put(first,apoConnectButton);
        var second=new Panel{Location=new Point(12,1198),Size=new Size(1116,38),BackColor=_bg};Put(canvas,second);
        Put(second,UiAction("전체 설정 초기화",ResetEveryDevice,0,2,364,29));
        Put(second,UiAction("현재 설정 내보내기",ExportSettings,376,2,364,29));
        Put(second,UiAction("설정 불러오기",ImportSettings,752,2,364,29));
        _deviceHint=UiText(UiStatusCatalog.DeviceTestHint,12,1240,1100,23);
        Put(canvas,_deviceHint);
        // Keep the public window free of developer-only diagnostics and duplicate APO actions.
        // Recovery functions remain reachable from the APO connection button's context menu.
        _output.Multiline=true;_output.ReadOnly=true;_output.Visible=false;_output.ScrollBars=ScrollBars.Both;
        Put(canvas,_output);
        InstallFeatureHelpIcons();
    }
    private List<UserGuideItem> CreateGuideItems()
    {
        return new List<UserGuideItem>
        {
            new(_devices, "출력 장치", "음향효과를 적용할 Windows 출력 장치를 선택합니다. 장치를 바꾸면 해당 장치의 설정을 불러옵니다."),
            new(_masterButton, "전체 효과 ON/OFF", "Infinity Audio Equalizer Pro의 음향효과를 한 번에 켜거나 끕니다. 세부 설정값은 유지됩니다."),
            new(_profileName, "프리셋 이름", "현재 출력 장치에 저장할 음향 설정의 이름을 지정합니다."),
            new(_profileEnabled, "장치에 음향효과 적용", "선택한 출력 장치에 저장된 EQ와 음향효과를 적용합니다."),
            new(_autoEnabled, "AutoEQ 적용", "헤드폰·이어폰 측정치를 기반으로 주파수 응답을 보정합니다. AutoEQ 파일을 먼저 가져오거나 검색하여 적용하세요."),
            new(_customEnabled, "추가 EQ 적용", "사용자가 설정한 31밴드 EQ를 적용합니다. AutoEQ와 함께 사용하면 보정된 소리에 원하는 음색을 더할 수 있습니다."),
            new(_preamp, "최종 프리앰프", "모든 음역의 전체 음량을 조절합니다. 값을 지나치게 높이면 소리가 왜곡될 수 있습니다."),
            new(_staticMode, "정적 EQ 모드", "자동 음량 조정 없이 설정한 프리앰프와 EQ 게인을 고정하여 사용합니다."),
            new(_lowLatency, "재생 시작 지연 최소화", "재생 시작 시 지연을 줄이도록 처리합니다. 5밴드 컴프레서, Surround Stage, 전후 공간감(HRTF), Studio Ambience를 우회하며 AutoEQ, 31밴드 EQ, 프리앰프, 스테레오 확장은 유지합니다."),
            new(_widthEnabled, "스테레오 확장", "좌우 공간감을 넓혀 소리가 더 넓게 펼쳐지도록 합니다. 강도를 높이면 중앙 음상이 달라질 수 있습니다."),
            new(_surroundOn, "Surround Stage", "보컬 중심과 좌우 공간감, 입체감을 조절해 보다 넓은 음장을 만듭니다."),
            new(_surroundProcessing, "공간 음향 처리 방식", "기본 공간 확장, 원음 안정성 우선, 균형형 공간감, HRTF 공간감 가운데 처리 방식을 고릅니다."),
            new(_surroundMode, "공간 음향 모드", "음악 감상 성향에 맞는 Surround Stage 음향 모드를 선택합니다."),
            new(_surroundStrength, "입체감", "Surround Stage의 공간감 강도를 조절합니다."),
            new(_surroundCenter, "보컬 중심", "공간 음향에서 중앙에 위치한 보컬과 소리의 존재감을 조절합니다."),
            new(_surroundSpread, "측면 확산", "좌우 측면으로 펼쳐지는 공간감의 강도를 조절합니다."),
            new(_surround3On, "전후 공간감", "소리가 앞이나 뒤에서 들리는 듯한 거리감과 방향 분리를 추가합니다."),
            new(_surround3Depth, "전후 깊이", "전후 방향으로 느껴지는 음장 깊이를 조절합니다."),
            new(_surround3Rear, "후방 공간감", "뒤쪽에서 들리는 듯한 공간감의 강도를 조절합니다."),
            new(_surround3Front, "전방 음상 거리", "앞쪽 음상이 멀거나 가깝게 느껴지는 정도를 조절합니다."),
            new(_surround3Separation, "방향 분리 강도", "서로 다른 방향에서 들리는 소리의 구분감을 조절합니다."),
            new(_balanceStability, "원음 안정성", "균형형 공간감 모드에서 원음의 중심과 안정감을 우선하여 유지하는 정도를 조절합니다."),
            new(_balanceSpace, "공간 확장", "균형형 공간감 모드에서 음장이 넓게 펼쳐지는 정도를 조절합니다."),
            new(_balanceDepth, "균형형 전후 깊이", "균형형 공간감 모드에서 앞뒤 음장의 깊이를 조절합니다."),
            new(_ambienceOn, "Studio Ambience", "원음에 공간의 울림과 깊이감을 더합니다. 강도와 잔향 길이를 조절할 수 있습니다."),
            new(_ambienceStrength, "Studio Ambience 강도", "공간 잔향의 양을 조절합니다. 강도가 높으면 잔향이 더 두드러집니다."),
            new(_ambienceDecay, "잔향 길이", "Studio Ambience의 잔향이 남는 시간을 조절합니다. 너무 길면 소리가 흐려질 수 있습니다."),
            new(_ambienceRate, "출력 샘플레이트", "Studio Ambience의 샘플레이트를 자동으로 감지하거나 직접 선택합니다."),
            new(_compressorEnabled, "5밴드 컴프레서", "저음부터 고음까지 다섯 대역의 음량 변화를 개별적으로 조절합니다. 곡의 작은 소리와 큰 소리의 차이를 다듬습니다."),
            new(_compressorTheme, "컴프레서 음향 테마", "미리 준비된 음량 처리 성향을 선택합니다. 테마 강도로 효과 정도를 조절할 수 있습니다."),
            new(_compressorStrength, "컴프레서 테마 강도", "선택한 5밴드 컴프레서 테마의 적용 강도를 조절합니다."),
            new(_startup, "Windows 로그인 시 자동 실행", "Windows에 로그인하면 Infinity Audio Equalizer Pro를 자동으로 시작합니다."),
        };
    }

    private void InstallFeatureHelpIcons()
    {
        // Only the seven representative checkboxes expose compact inline help.
        // The full User Guide overlay retains all detailed controls and descriptions.
        var supported = new HashSet<Control>
        {
            _staticMode, _lowLatency, _widthEnabled, _surroundOn,
            _surround3On, _ambienceOn, _compressorEnabled
        };
        foreach (var item in CreateGuideItems())
        {
            var target = item.Target;
            if (!supported.Contains(target) || target is not CheckBox checkBox)
                continue;
            var parent = target.Parent;
            if (parent is null) continue;

            // The final checkbox text is now set before UiStyle. Recalculate its
            // width once here, using a common metric for every help-bearing row.
            // Leave extra room for the native check glyph and Windows text padding;
            // the full text must stay visible, never be truncated to fit an icon.
            int textWidth = TextRenderer.MeasureText(checkBox.Text ?? string.Empty,
                checkBox.Font, new Size(int.MaxValue, checkBox.Height),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
            const int checkboxGlyphAndPadding = 30;
            checkBox.Width = Math.Max(32, textWidth + checkboxGlyphAndPadding);
            // Reduce only the extra whitespace after the fully measured checkbox.
            // The native checkbox text/glyph safety padding above is retained.
            const int textToIconGap = 1;
            const int iconWidth = 18;
            const int iconHeight = 18;
            // All seven buttons share the same rule. Never apply row-specific
            // negative corrections or clamp against the next setting: those
            // moved icons into/over the label and produced uneven gaps.
            int iconX = checkBox.Right + textToIconGap;
            int iconY = checkBox.Top + (checkBox.Height - iconHeight) / 2;
            if (iconX + iconWidth > parent.ClientSize.Width - 2)
                continue; // Never place a clipped icon.
            var marker = new Label
            {
                AutoSize = false,
                Text = "ⓘ",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Symbol", 10.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(141, 187, 250),
                BackColor = Color.Transparent,
                Cursor = Cursors.Help,
                Size = new Size(iconWidth, iconHeight),
                Location = new Point(iconX, iconY),
                AccessibleName = item.Title + " 설명",
                AccessibleDescription = item.Description,
                TabStop = false
            };
            parent.Controls.Add(marker);
            marker.BringToFront();
            string tip = item.Title + Environment.NewLine + item.Description;
            _featureHelpTip.SetToolTip(marker, tip);
            if (_earlyHelpOnLabels)
                _featureHelpTip.SetToolTip(checkBox, tip);
        }
    }

    private void ShowUserGuide()
    {
        if (_mainScroller is null || _guideOverlay is not null) return;
        var items = CreateGuideItems();
        // The overlay is a direct child of the form: it does not participate in the
        // canvas layout, change the scroll extent, or resize any existing control.
        _featureHelpTip.Active = false;
        var guide = new UserGuideOverlay(_mainScroller, items, () =>
        {
            if (_guideOverlay is not null)
            {
                Controls.Remove(_guideOverlay);
                _guideOverlay.Dispose();
                _guideOverlay = null;
            }
            _featureHelpTip.Active = true;
            _mainScroller?.Focus();
        }) { Dock = DockStyle.Fill };
        _guideOverlay = guide;
        Controls.Add(guide);
        guide.BringToFront();
        guide.Start();
    }

    private CheckBox DisabledCheckAt(string text,int x,int y,int w,int h)
    {
        return new CheckBox { Text=text,Location=new Point(x,y),Size=new Size(w,h),Enabled=false,
            ForeColor=Color.FromArgb(170,185,203),BackColor=Color.Transparent };
    }
    private NumericUpDown UiDisabledNumber(int value,int x,int y,int w)
    {
        return new NumericUpDown { Minimum=0,Maximum=100,Value=value,Location=new Point(x,y),
            Size=new Size(w,26),Enabled=false,ForeColor=Color.FromArgb(175,190,210),BackColor=Color.FromArgb(34,45,62) };
    }
    private void PreviewManaged() => _output.Text=ApoRenderer.Render(_state,_apoRoot);
    private readonly TextBox _output=new();
    private Label MakeLabel(string text)=>new(){Text=text,AutoSize=true,ForeColor=_fg,Padding=new Padding(6,7,6,0)};
    private Button Action(string text,Action onClick){var b=new AccentButton{Text=text,AutoSize=true,Height=32,Margin=new Padding(5,2,5,2)};b.Click+=(_,_)=>{try{onClick();}catch(Exception e){ShowError(e);}};return b;}
    private static void ShowError(Exception e)=>MessageBox.Show(e.Message,"Infinity Audio Equalizer Pro",MessageBoxButtons.OK,MessageBoxIcon.Warning);
    private void RefreshDevices()
    {
        List<AudioDevice> devices;
        try
        {
            devices=AudioDeviceEnumerator.GetActiveRenderDevices();
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidCastException or TypeLoadException)
        {
            _status.Text="출력 장치를 불러오지 못했습니다. 새로고침을 다시 시도해 주세요.";
            MessageBox.Show("Windows 오디오 출력 장치를 조회할 수 없습니다. 장치 연결 상태를 확인한 후 새로고침을 눌러 주세요.\n\n"+ex.Message,
                "Infinity Audio Equalizer Pro",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            return;
        }
        string previous=_selectedId;
        _loading=true;
        try
        {
            _devices.DataSource=null;
            _devices.DisplayMember=nameof(AudioDevice.Name);
            _devices.ValueMember=nameof(AudioDevice.Id);
            _devices.DataSource=devices;
            string? defaultId=AudioDeviceEnumerator.GetDefaultRenderDeviceId();
            int index=DeviceSelectionPolicy.ChooseIndex(devices,previous,defaultId,_observedDefaultDeviceId);
            if(index>=0)_devices.SelectedIndex=index;
            _deviceSignature=DeviceSelectionPolicy.Signature(devices);
            // Only acknowledge a changed default when it can be selected; otherwise
            // retry on the next scan while Bluetooth finishes connecting.
            if(!string.IsNullOrEmpty(defaultId) && devices.Any(d=>d.Id.Equals(defaultId,StringComparison.OrdinalIgnoreCase)))
                _observedDefaultDeviceId=defaultId;
        }
        finally{_loading=false;}
        SelectDevice();
        _status.Text=$"출력 장치 {devices.Count}개 검색됨 · 장치별 설정을 불러왔습니다.";
    }
    private DeviceProfile? CurrentProfile(){if(string.IsNullOrWhiteSpace(_selectedId))return null;return _state.Profiles.GetValueOrDefault(_selectedId);}
    // One calculation for both saved-profile selection and live edits.
    // AutoEQ import must not leave the old source-only text on a manual preamp.
    private void UpdatePreampSummary(DeviceProfile p)
    {
        var details=PreampCalculator.Calculate(p);
        string Display(double value)=>value.ToString("+0.##;-0.##;0")+" dB";
        _preampSummary.Text=(!p.Enabled || !_state.GlobalEnabled || _state.BypassAllEffects)
            ? "음향 효과 OFF · 프리앰프 적용 안 함"
            : p.PreampMode=="manual"
                ? $"최종 {Display(details.Final)} · 자동 음량 감소 없음 · 설정한 값 그대로 적용"
                : $"기준 {Display(details.Baseline)} + 보정 {Display(p.ManualPreamp)} = 최종 {Display(details.Final)}";
    }

    private void SelectDevice()
    {
        if(_loading)return;
        var d=_devices.SelectedItem as AudioDevice;if(d is null)return;
        if (!string.Equals(_selectedId,d.Id,StringComparison.OrdinalIgnoreCase))
        {
            bool wasPreview=_state.Profiles.Values.Any(x=>x.DepthComparisonMode.Length>0 || x.AmbienceDiagnostic);
            foreach (var existing in _state.Profiles.Values)
            {
                existing.DepthComparisonMode="";
                existing.AmbienceDiagnostic=false;
            }
            if (wasPreview && _connected)
            {
                _pendingChanges=true;
                _applyTimer.Stop();
                _applyTimer.Start();
            }
        }
        bool changedEndpoint=!string.Equals(_selectedId,d.Id,StringComparison.OrdinalIgnoreCase);
        _selectedId=d.Id;
        // Keep every output endpoint's independent profile; never reuse the previous
        // device's AutoEQ, compressor or manual bands for a newly connected headset.
        if(!_state.Profiles.TryGetValue(d.Id,out var p))_state.Profiles[d.Id]=p=new DeviceProfile{Name=d.Name};
        _loading=true;
        try
        {
            _profileName.Text=p.Name; _autoSource.Text=string.IsNullOrWhiteSpace(p.Source)?"AutoEQ 파일 없음":p.Source;
            _lowLatency.Checked=p.LowLatency;
            _engine.Checked=_state.GlobalEnabled && !_state.BypassAllEffects;_bypassAll.Checked=_state.BypassAllEffects;_profileEnabled.Checked=p.Enabled;_customEnabled.Checked=p.CustomEnabled;_autoEnabled.Checked=p.AutoEnabled;
            _preamp.Value=Math.Clamp((decimal)p.ManualPreamp,_preamp.Minimum,_preamp.Maximum);
            _preampMode.SelectedIndex=p.PreampMode=="source"?1:p.PreampMode=="automatic"?2:0;_staticMode.Checked=p.PreampMode=="manual";_widthEnabled.Checked=p.StereoWidthEnabled;
            _width.Value=Math.Clamp(p.StereoWidth,(int)_width.Minimum,(int)_width.Maximum);
            _surroundOn.Checked=p.SurroundEnabled;
            _surround3On.Checked=p.Surround3Enabled;
            _surround3Depth.Value=Math.Clamp(p.Surround3Depth,0,100);
            _surround3Rear.Value=Math.Clamp(p.Surround3Rear,0,100);
            _surround3Front.Value=Math.Clamp(p.Surround3Front,0,100);
            _surround3Separation.Value=Math.Clamp(p.Surround3Separation,0,100);
            _surroundProcessing.SelectedIndex=p.SurroundBinaural?(p.SurroundBalanced?2:p.SurroundStable?1:3):0;
            _balanceStability.Value=Math.Clamp(p.SurroundBalanceStability,0,100);
            _balanceSpace.Value=Math.Clamp(p.SurroundBalanceSpace,0,100);
            _balanceDepth.Value=Math.Clamp(p.SurroundBalanceDepth,0,100);
            _surroundMode.SelectedIndex=p.SurroundMode=="cinema"?1:p.SurroundMode=="stage"?2:0;
            _surroundStrength.Value=Math.Clamp(p.SurroundStrength,0,100);
            _surroundCenter.Value=Math.Clamp(p.SurroundCenter,0,100);
            _surroundSpread.Value=Math.Clamp(p.SurroundSpread,0,100);
            _ambienceOn.Checked=p.AmbienceEnabled;
            _ambienceStrength.Value=Math.Clamp(p.AmbienceStrength,0,10);
            _ambienceDecay.Value=Math.Clamp(p.AmbienceDecay,100,3000);
            _ambienceRate.SelectedIndex=p.AmbienceRateAuto ? 0 :
                Math.Max(1,Array.IndexOf(AmbienceRates,p.AmbienceRate)+1);
            _compressorEnabled.Checked=p.CompressorEnabled;
            var theme=p.CompressorTheme;
            _compressorTheme.SelectedItem=CompressorThemes.DisplayName(
                (CompressorThemes.ThemeIds.Contains(theme) || theme=="CUSTOM") ? theme : "PREMIUM_BALANCE");
            _compressorStrength.Value=Math.Clamp(p.CompressorStrength,1,12);
            for(int i=0;i<_bands.Count;i++)
            {
                double v=i<p.Custom.Count && double.IsFinite(p.Custom[i])?p.Custom[i]:0;
                decimal neutral=Math.Clamp((decimal)v,_bands[i].Minimum,_bands[i].Maximum);
                _bands[i].Value=neutral;
                _sliders[i].Value=Math.Clamp((int)Math.Round(neutral * 10),_sliders[i].Minimum,_sliders[i].Maximum);
            }
        }
        finally{_loading=false;}
        UpdatePreampSummary(p);
        UpdateSelectedSampleRate(true);
        UpdateSpatialControlAvailability();
        RefreshDepthComparisonButtons();
        RefreshAmbienceTestButton();
        RefreshMasterButton();
        // Reactivated Bluetooth endpoints may need an APO file update even if their
        // saved filter values and sample rate did not change since last use.
        if(changedEndpoint && _connected)
        {
            _pendingChanges=true;
            _applyRetryCount=0;
            _applyTimer.Stop();
            _applyTimer.Start();
        }
    }

    private void UpdateSelectedSampleRate(bool persistChange)
    {
        var p=CurrentProfile();
        if (p is null || _rateReadout is null) return;
        if (!p.AmbienceRateAuto)
        {
            _rateReadout.Text=$"수동 {p.AmbienceRate:N0} Hz";
            return;
        }
        int? detected=AudioDeviceEnumerator.GetMixSampleRate(_selectedId);
        if (detected.HasValue && AmbienceRates.Contains(detected.Value))
        {
            bool changed=p.AmbienceRate!=detected.Value;
            p.AmbienceRate=detected.Value;
            _rateReadout.Text=$"감지 {detected.Value:N0} Hz";
            if (changed && persistChange)
            {
                SettingsStore.Save(_state);
                if (_connected) { _pendingChanges=true; _applyTimer.Stop(); _applyTimer.Start(); }
            }
        }
        else _rateReadout.Text=detected.HasValue ?
            $"{detected.Value:N0} Hz · 수동 선택" : "감지 실패 · 수동 선택";
    }

    private void RefreshDepthComparisonButtons()
    {
        string mode=CurrentProfile()?.DepthComparisonMode ?? "";
        bool canUse=_connected && _state.GlobalEnabled && !_state.BypassAllEffects &&
            CurrentProfile() is { Enabled:true, LowLatency:false, SurroundEnabled:true, Surround3Enabled:true };
        if (_frontCompareButton is not null)
        {
            _frontCompareButton.Text=mode=="front" ? "전방 비교 해제" : "전방 비교";
            _frontCompareButton.Tone=mode=="front" ? AccentButtonTone.Success : AccentButtonTone.Standard;
            _frontCompareButton.Enabled=canUse;
        }
        if (_rearCompareButton is not null)
        {
            _rearCompareButton.Text=mode=="rear" ? "후방 비교 해제" : "후방 비교";
            _rearCompareButton.Tone=mode=="rear" ? AccentButtonTone.Success : AccentButtonTone.Standard;
            _rearCompareButton.Enabled=canUse;
        }
    }

    private void ToggleDepthComparison(string direction)
    {
        if (direction is not ("front" or "rear")) return;
        var p=CurrentProfile();
        if (p is null || !_connected || !_state.GlobalEnabled || _state.BypassAllEffects ||
            !p.Enabled || p.LowLatency || !p.SurroundEnabled || !p.Surround3Enabled)
        {
            _status.Text="전후 공간감을 켜고 출력 장치를 연결한 뒤 비교해 주세요.";
            return;
        }
        string previous=p.DepthComparisonMode;
        bool oldAmbienceTest=p.AmbienceDiagnostic;
        p.AmbienceDiagnostic=false;
        RefreshAmbienceTestButton();
        p.DepthComparisonMode=previous==direction ? "" : direction;
        RefreshDepthComparisonButtons();
        SynchronizeNow();
        if (_pendingChanges)
        {
            p.DepthComparisonMode=previous;
            p.AmbienceDiagnostic=oldAmbienceTest;
            RefreshDepthComparisonButtons();
            RefreshAmbienceTestButton();
            SynchronizeNow();
            _status.Text="비교 설정을 적용하지 못했습니다. 다시 시도해 주세요.";
            return;
        }
        _status.Text=p.DepthComparisonMode switch
        {
            "front" => "전방 공간감 비교 중 · 다시 누르면 기존 음향 효과로 돌아갑니다.",
            "rear" => "후방 공간감 비교 중 · 다시 누르면 기존 음향 효과로 돌아갑니다.",
            _ => "기존 음향 효과로 돌아왔습니다."
        };
    }

    private void RefreshAmbienceTestButton()
    {
        if(_ambienceTestButton is null)return;
        bool testing=CurrentProfile()?.AmbienceDiagnostic==true;
        _ambienceTestButton.Text=testing ? "테스트 종료 · 복원" : "잔향 적용 테스트";
        _ambienceTestButton.Tone=testing ? AccentButtonTone.Success : AccentButtonTone.Standard;
        _ambienceTestButton.Enabled=CurrentProfile() is {Enabled:true,LowLatency:false} &&
            _connected && _state.GlobalEnabled && !_state.BypassAllEffects;
    }

    private void ToggleAmbienceTest()
    {
        var p=CurrentProfile();
        if(p is null || !_connected || !p.Enabled || p.LowLatency ||
           !_state.GlobalEnabled || _state.BypassAllEffects)
        {
            _status.Text="잔향 테스트를 사용하려면 출력 장치를 연결하고 전체 효과를 켜주세요.";
            return;
        }
        bool previous=p.AmbienceDiagnostic;
        string oldComparison=p.DepthComparisonMode;
        p.AmbienceDiagnostic=!previous;
        p.DepthComparisonMode="";
        RefreshDepthComparisonButtons();
        RefreshAmbienceTestButton();
        _lastChangedEffect="잔향 테스트";
        SynchronizeNow();
        if(_pendingChanges)
        {
            p.AmbienceDiagnostic=previous;
            p.DepthComparisonMode=oldComparison;
            RefreshAmbienceTestButton();
            RefreshDepthComparisonButtons();
            SynchronizeNow();
            _status.Text="잔향 테스트를 적용하지 못했습니다. 다시 시도해 주세요.";
            return;
        }
        _status.Text=p.AmbienceDiagnostic
            ? "잔향 테스트 중 · 다시 누르면 원래 음향 효과로 돌아갑니다."
            : "잔향 테스트를 종료하고 기존 음향 설정을 적용했습니다.";
    }

    private void UpdateSpatialControlAvailability()
    {
        bool enabled=_surroundOn.Checked;
        _surroundMode.Enabled=enabled;
        _surroundProcessing.Enabled=enabled;
        _surroundStrength.Enabled=enabled;
        _surroundCenter.Enabled=enabled;
        _surroundSpread.Enabled=enabled;
        _surround3On.Enabled=enabled;
        bool stage3=enabled && _surround3On.Checked;
        foreach(var control in new[]{_surround3Depth,_surround3Rear,_surround3Front,_surround3Separation})
            control.Enabled=stage3;
        bool balanced=enabled && _surroundProcessing.SelectedIndex==2;
        foreach(var control in new[]{_balanceStability,_balanceSpace,_balanceDepth})
            control.Enabled=balanced;
        RefreshDepthComparisonButtons();
    }
    private void ChangeCurrent(string? effectName=null)
    {
        if(_loading)return;
        if(!string.IsNullOrWhiteSpace(effectName))_lastChangedEffect=effectName;
        UpdateSpatialControlAvailability();
        var p=CurrentProfile();if(p is null)return;
        // Ordinary edits leave a temporary audio test so the visible controls
        // always describe the actual filter chain being written.
        if(p.AmbienceDiagnostic || p.DepthComparisonMode.Length>0)
        {
            p.AmbienceDiagnostic=false;
            p.DepthComparisonMode="";
            RefreshAmbienceTestButton();
            RefreshDepthComparisonButtons();
        }
        _state.GlobalEnabled=_engine.Checked;_state.BypassAllEffects=_bypassAll.Checked;p.Enabled=_profileEnabled.Checked;p.CustomEnabled=_customEnabled.Checked;
        p.Name=string.IsNullOrWhiteSpace(_profileName.Text)?p.Name:_profileName.Text.Trim();
        p.LowLatency=_lowLatency.Checked;
        p.AutoEnabled=_autoEnabled.Checked;p.ManualPreamp=(double)_preamp.Value;p.PreampMode=_preampMode.SelectedIndex==1?"source":_preampMode.SelectedIndex==2?"automatic":"manual";
        p.StereoWidthEnabled=_widthEnabled.Checked;p.StereoWidth=(int)_width.Value;
        p.SurroundEnabled=_surroundOn.Checked;p.SurroundMode=_surroundMode.SelectedIndex==1?"cinema":_surroundMode.SelectedIndex==2?"stage":"natural";
        p.SurroundStrength=(int)_surroundStrength.Value;p.SurroundCenter=(int)_surroundCenter.Value;p.SurroundSpread=(int)_surroundSpread.Value;
        p.Surround3Enabled=_surround3On.Checked;
        p.Surround3Depth=(int)_surround3Depth.Value;p.Surround3Rear=(int)_surround3Rear.Value;
        p.Surround3Front=(int)_surround3Front.Value;p.Surround3Separation=(int)_surround3Separation.Value;
        p.SurroundBinaural=_surroundProcessing.SelectedIndex>0;
        p.SurroundStable=_surroundProcessing.SelectedIndex==1;
        p.SurroundBalanced=_surroundProcessing.SelectedIndex==2;
        p.SurroundBalanceStability=(int)_balanceStability.Value;
        p.SurroundBalanceSpace=(int)_balanceSpace.Value;
        p.SurroundBalanceDepth=(int)_balanceDepth.Value;
        p.AmbienceEnabled=_ambienceOn.Checked;p.AmbienceStrength=(int)_ambienceStrength.Value;p.AmbienceDecay=(int)_ambienceDecay.Value;
        p.AmbienceRateAuto=_ambienceRate.SelectedIndex<=0;
        if (!p.AmbienceRateAuto)
            p.AmbienceRate=AmbienceRates[Math.Clamp(_ambienceRate.SelectedIndex-1,0,AmbienceRates.Length-1)];
        UpdateSelectedSampleRate(false);
        p.CompressorEnabled=_compressorEnabled.Checked;
        p.CompressorTheme=CompressorThemes.IdFromDisplayName(_compressorTheme.SelectedItem?.ToString());
        p.CompressorStrength=(int)_compressorStrength.Value;
        p.Custom=_bands.Select(b=>(double)b.Value).ToList();
        if (p.DepthComparisonMode.Length>0 &&
            (!p.SurroundEnabled || !p.Surround3Enabled || p.LowLatency ||
             !p.Enabled || !_state.GlobalEnabled || _state.BypassAllEffects))
            p.DepthComparisonMode="";
        if (p.AmbienceDiagnostic &&
            (p.LowLatency || !p.Enabled || !_state.GlobalEnabled || _state.BypassAllEffects))
            p.AmbienceDiagnostic=false;
        RefreshDepthComparisonButtons();
        RefreshAmbienceTestButton();
        // Model is the single source of truth. Persist each UI edit before asking APO
        // to reload, so closing/crashing the app never makes the display lie.
        try { SettingsStore.Save(_state); }
        catch (Exception ex) { _status.Text="설정을 저장하지 못했습니다: " + ex.Message; return; }
        _autoSource.Text=string.IsNullOrWhiteSpace(p.Source)?"AutoEQ 파일 없음":p.Source;
        UpdatePreampSummary(p);
        _output.Text=ApoRenderer.Render(_state, _apoRoot);
        if (_connected)
        {
            _pendingChanges = true;
            _status.ForeColor=Color.FromArgb(231,202,131);
            _status.Text=$"{_lastChangedEffect} 적용 중...";
            _applyRetryCount=0;_applyTimer.Interval=480;
            _applyTimer.Stop();_applyTimer.Start();
        }
        else _status.Text = !_engine.Checked || _bypassAll.Checked
            ? "전체 음향 효과 OFF" : "음향 설정 연결 필요";
    }
    private void SaveState()
    {
        ChangeCurrent(); SettingsStore.Save(_state);
        _status.Text = _connected ? UiStatusCatalog.SettingsSaved : UiStatusCatalog.BridgeRequired;
    }
    private void ImportLegacy()
    {
        if(!SettingsStore.CanImportLegacy){MessageBox.Show("가져올 이전 설정 파일이 없습니다.");return;}
        if(MessageBox.Show("이전 프로그램의 설정을 가져오시겠습니까? 기존 설정 파일은 변경되지 않습니다.","설정 가져오기",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        _state=SettingsStore.ImportLegacyReadOnly();_state.BypassAllEffects=true;SelectDevice();RefreshMasterButton();SaveState();
        if (_connected) { _pendingChanges = true; _applyTimer.Stop(); _applyTimer.Start(); }
        _status.Text="이전 설정을 가져왔습니다. 음향 효과는 꺼진 상태입니다.";
    }
    private void ImportAutoEq()
    {
        var p=CurrentProfile();if(p is null)return;
        using var dlg=new OpenFileDialog{Filter="AutoEQ TXT (*.txt)|*.txt|모든 파일 (*.*)|*.*",Title="AutoEQ Equalizer APO 파일 가져오기"};
        if(dlg.ShowDialog()!=DialogResult.OK)return;
        AutoEqImporter.Import(File.ReadAllText(dlg.FileName),p,Path.GetFileName(dlg.FileName));
        _autoEnabled.Checked=true;SelectDevice();ChangeCurrent("AutoEQ");
        _status.Text=_connected ? "AutoEQ 적용 중... 기존 프리앰프 설정은 유지됩니다."
            : "AutoEQ를 저장했습니다. 출력 장치 연결이 필요합니다.";
    }
    private void SearchAutoEq()
    {
        if(string.IsNullOrWhiteSpace(_selectedId))return;
        string selectedDeviceId=_selectedId;
        string suggested=(_devices.SelectedItem as AudioDevice)?.Name ?? "";
        using var dialog=new AutoEqSearchDialog(selectedDeviceId, "");
        if(dialog.ShowDialog(this)!=DialogResult.OK || dialog.Download is null || dialog.Selection is null)return;
        if(!_state.Profiles.TryGetValue(selectedDeviceId,out var profile))return;
        AutoEqImporter.Import(dialog.Download.Text,profile,$"{dialog.Selection.Name} ({dialog.Download.Filename})");
        if(string.Equals(_selectedId,selectedDeviceId,StringComparison.OrdinalIgnoreCase))SelectDevice();
        // Persist to the model first; only then ask APO to render the new state.
        SettingsStore.Save(_state);
        _output.Text=ApoRenderer.Render(_state,_apoRoot);
        if(_connected)
        {
            _lastChangedEffect="AutoEQ";
            _pendingChanges=true;
            _applyTimer.Stop();_applyTimer.Start();
        }
        _status.Text=_connected?"AutoEQ 적용 중...":"AutoEQ를 저장했습니다. 출력 장치 연결이 필요합니다.";
    }
    private void ClearAutoEq()
    {
        var profile=CurrentProfile();if(profile is null)return;
        if(MessageBox.Show("선택 장치의 AutoEQ 보정값을 제거할까요? 사용자 31밴드 EQ는 유지됩니다.","AutoEQ 제거",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        profile.AutoEnabled=false;profile.AutoFilters.Clear();profile.Graphic.Clear();
        profile.AutoPreamp=0;profile.Source="";
        SelectDevice();ChangeCurrent("AutoEQ");
    }
    private void ShowAutoEqCurve()
    {
        var p=CurrentProfile();if(p is null)return;
        if(p.AutoFilters.Count==0 && p.Graphic.Count==0){MessageBox.Show("AutoEQ 보정값이 없습니다.","AutoEQ 곡선");return;}
        using var window=new AutoEqCurveDialog(p);
        window.ShowDialog(this);
    }
    private void ResetEq()
    {
        if(MessageBox.Show("선택 장치의 31밴드 EQ 31개 슬라이더와 수치를 0dB로 초기화할까요?\n다른 음향 효과는 유지됩니다.","31밴드 EQ 초기화",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        _loading=true;
        try { for(int i=0;i<_bands.Count;i++){_bands[i].Value=0;_sliders[i].Value=0;} }
        finally { _loading=false; }
        ChangeCurrent("31밴드 EQ");
    }
    private void ResetAllEffects()
    {
        var p=CurrentProfile();if(p is null)return;
        if(MessageBox.Show("선택한 장치의 EQ·AutoEQ·프리앰프·컴프레서·스테레오 확장을 모두 초기화하고 끌까요?\n이 작업은 선택 장치에만 적용됩니다. 이후 설정은 즉시 저장됩니다.",
            "선택 장치 전체 효과 초기화",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        p.Custom=Enumerable.Repeat(0d,ApoRenderer.Bands.Length).ToList();
        p.CustomEnabled=false;
        p.AutoEnabled=false;p.AutoFilters.Clear();p.Graphic.Clear();p.AutoPreamp=0;p.Source="";
        p.ManualPreamp=0;p.PreampMode="manual";p.DepthComparisonMode="";
        p.CompressorEnabled=false;p.StereoWidthEnabled=false;p.StereoWidth=0;
        p.AmbienceEnabled=false;p.AmbienceStrength=3;p.AmbienceDecay=180;p.AmbienceRate=48000;
        p.SurroundEnabled=false;p.Surround3Enabled=false;p.Surround3Depth=80;p.Surround3Rear=75;p.Surround3Front=75;p.Surround3Separation=80;p.SurroundBinaural=false;p.SurroundStable=false;p.SurroundBalanced=false;p.SurroundBalanceStability=65;p.SurroundBalanceSpace=80;p.SurroundBalanceDepth=55;p.SurroundMode="natural";p.SurroundStrength=45;p.SurroundCenter=80;p.SurroundSpread=45;
        SelectDevice();
        ChangeCurrent();
        if (_connected) SynchronizeNow();
    }
    private void ResetEveryDevice()
    {
        if(MessageBox.Show("모든 출력 장치의 음향 설정을 초기화하시겠습니까? 저장된 EQ, AutoEQ 및 공간 음향 설정이 초기화됩니다.",
            "모든 장치 효과 초기화",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        foreach(var p in _state.Profiles.Values)
        {
            p.Custom=Enumerable.Repeat(0d,ApoRenderer.Bands.Length).ToList();
            p.CustomEnabled=false;p.AutoEnabled=false;p.AutoFilters.Clear();p.Graphic.Clear();p.AutoPreamp=0;p.Source="";
            p.ManualPreamp=0;p.PreampMode="manual";p.DepthComparisonMode="";
            p.CompressorEnabled=false;p.StereoWidthEnabled=false;p.StereoWidth=0;
            p.AmbienceEnabled=false;p.AmbienceStrength=3;p.AmbienceDecay=180;p.AmbienceRate=48000;
        p.SurroundEnabled=false;p.Surround3Enabled=false;p.Surround3Depth=80;p.Surround3Rear=75;p.Surround3Front=75;p.Surround3Separation=80;p.SurroundBinaural=false;p.SurroundStable=false;p.SurroundBalanced=false;p.SurroundBalanceStability=65;p.SurroundBalanceSpace=80;p.SurroundBalanceDepth=55;p.SurroundMode="natural";p.SurroundStrength=45;p.SurroundCenter=80;p.SurroundSpread=45;
        }
        _state.BypassAllEffects=true;
        SelectDevice();
        ChangeCurrent();
        if (_connected) SynchronizeNow();
    }
    private void SynchronizeNow()
    {
        if (!_connected || _apoRoot is null)
        {
            _apoRoot = ApoBridge.DiscoverConfig(_state);
            _connected = _apoRoot is not null && ApoBridge.IsConnected(_apoRoot);
            if (!_connected) { _status.Text="APO 미연결 · 동기화할 수 없습니다."; return; }
        }
        _applyTimer.Stop(); _pendingChanges=true;
        ApplyLive();
    }
    private void ReapplyWithFeedback()
    {
        SynchronizeNow();
        if (_connected && !_pendingChanges && !string.IsNullOrEmpty(_appliedFingerprint))
            ShowActionComplete("EQ 설정 파일을 다시 저장했습니다.");
        else
            MessageBox.Show(this, "연결 상태를 확인한 후 다시 시도해 주세요.",
                "음향 설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void CheckBridge()
    {
        // A leftover config.txt is not proof that EqualizerAPO-XT is still installed.
        bool xtInstalled=FindXtSelector() is not null;
        _apoRoot = xtInstalled ? ApoBridge.DiscoverConfig(_state) : null;
        _connected = _apoRoot is not null && ApoBridge.IsConnected(_apoRoot);
        RefreshDepthComparisonButtons();
        RefreshAmbienceTestButton();
        _bridgeStatus.Text = _apoRoot is null ? "APO 연결 필요"
            : _connected ? "APO 설정 연결됨" : "음향 설정 연결 필요";
        if (_apoRoot is not null)
            _bridgePathTip.SetToolTip(_bridgeStatus,
                "APO 설정 파일: " + Path.Combine(_apoRoot, "config.txt"));
        UpdateApoRegistrationStatus();
        if (_connected)
        {
            // Previous versions could leave an old managed.txt after reconnect.
            // Synchronize the real output with saved UI state immediately.
            SynchronizeNow();
        }
    }
    // Single user-facing action: perform a first-time bridge connection if needed,
    // otherwise force a full render of the current UI/model into the active XT path.
    // A successful file write is NOT proof that the audio driver is processing DSP.
    // Only explicit, user-initiated commands show completion dialogs.
    // Automatic slider updates never display a modal popup.
    private void ShowActionComplete(string message, string title = "음향 설정")
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Config file health is NOT Windows endpoint APO registration or proof of audible DSP.
    // A selector/repair failure can leave the config connected while the endpoint is inactive.
    // Offer one-click access to the official device selector instead of declaring success.
    private void ShowOutputActivationGuidance(string fileResult)
    {
        string guidance = fileResult +
            "\n\n음향효과가 적용되지 않으면 '출력 장치 등록'에서 사용하는 재생 장치를 등록하세요." +
            "\n등록 후 Windows 오디오 재시작이 필요할 수 있습니다.";
        // The selector is already open/finishing; offering to open it again here
        // would lead to a dead-end because _selectorBusy prevents a second launch.
        if (_selectorBusy)
        {
            MessageBox.Show(this, guidance,
                "Equalizer APO 출력 상태 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var choice = MessageBox.Show(this,
            guidance + "\n\n지금 출력 장치 등록 도구를 여시겠습니까?",
            "Equalizer APO 출력 상태 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (choice == DialogResult.Yes)
            _ = OpenDeviceSelectorAsync();
    }

    // Called only on an explicit OFF -> ON transition. OFF and window/tray exit
    // never require XT, a working endpoint or any confirmation dialog.
    // This is a file/installation check, NOT a claim that Windows runs the DSP.
    private void PromptForMissingApoOnActivation(ActivationUiSnapshot uiBeforeActivation)
    {
        if (IsDisposed || Disposing || _loading || _activationPromptBusy || _selectorBusy || _xtInstallBusy
            || !_engine.Checked || _bypassAll.Checked) return;
        _activationPromptBusy = true;
        try
        {
            ActivationIssue issue = IndependentlyDiagnoseActivation(out string? detectedRoot);
            _apoRoot = detectedRoot;
            _connected = detectedRoot is not null && issue != ActivationIssue.BridgeMissing
                && ApoBridge.IsConnected(detectedRoot);
            if (issue == ActivationIssue.None) return;
            // Explicit endpoint registry evidence takes priority: the former UI may
            // have shown only file-link status and could not diagnose registration.
            // For the other issues, retain the previous cross-check policy.
            if (issue is not (ActivationIssue.DeviceNotRegistered or ActivationIssue.XtMissing) && !UiConfirmsIssue(issue, uiBeforeActivation))
            {
                ShowActivationCheckRequired(issue, uiBeforeActivation);
                return;
            }
            switch (issue)
            {
                case ActivationIssue.DeviceNotRegistered:
                    _bridgeStatus.Text = "출력 장치 등록 필요";
                    _status.Text = "출력 장치 등록이 필요합니다.";
                    if (MessageBox.Show(this,
                        "선택한 출력 장치에 Equalizer APO가 등록되어 있지 않습니다.\n\n지금 출력 장치를 등록하시겠습니까?",
                        "출력 장치 등록", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        _ = OpenDeviceSelectorAsync();
                    break;
                case ActivationIssue.XtMissing:
                    _status.Text = "EqualizerAPO-XT 설치 필요";
                    if (MessageBox.Show(this,
                        "전체 효과를 사용하려면 EqualizerAPO-XT가 필요합니다.\n\n지금 설치하시겠습니까?",
                        "음향 효과 연결", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        InstallXt();
                    break;
                case ActivationIssue.ConfigMissing:
                    _connected = false;
                    _bridgeStatus.Text = "APO 설정 경로 선택";
                    if (MessageBox.Show(this,
                        "EqualizerAPO-XT는 확인되지만 설정 폴더를 찾지 못했습니다.\n\n지금 설정 경로를 선택하시겠습니까?",
                        "음향 효과 연결", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        _ = RecoverApoConfigFromActivationAsync();
                    break;
                case ActivationIssue.BridgeMissing:
                    _bridgeStatus.Text = "Infinity Audio Equalizer Pro 음향 설정 연결 필요";
                    if (MessageBox.Show(this,
                        "Infinity Audio Equalizer Pro 음향 설정이 EqualizerAPO-XT에 연결되어 있지 않습니다.\n\n지금 연결하시겠습니까?",
                        "음향 효과 연결", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        _ = CompleteApoSetupAfterRegistrationAsync(selectorWasOpened:false);
                    break;
            }
        }
        catch (Exception ex)
        {
            DiagnosticLog.Error("전체 효과 ON 교차 진단", ex);
            _bridgeStatus.Text = "APO 연결";
            _status.Text = "음향 효과 ON";
        }
        finally { _activationPromptBusy = false; }
    }

    private async Task RecoverApoConfigFromActivationAsync()
    {
        if (await CompleteApoSetupAfterRegistrationAsync(selectorWasOpened:false)) return;
        if (IsDisposed || Disposing || _apoSetupCompletionBusy) return;
        if (ApoBridge.DiscoverConfig(_state) is null)
            ChooseApoPath(); // Only after user consent and automatic discovery failed.
    }

    private void EnsureConnectedAndApply(bool showOutputGuidance=true)
    {
        _applyTimer.Stop();
        _apoRoot = FindXtSelector() is not null
            ? ApoBridge.DiscoverConfig(_state) : null;
        if (_apoRoot is null)
        {
            ShowInitialApoGuidance(force:true);
            return;
        }
        _connected = ApoBridge.IsConnected(_apoRoot);
        bool wasConnected = _connected;
        if (!_connected) ConnectApo();
        // If the user cancelled an install/consent dialog, do not report success.
        if (!_connected) return;
        // First connect builds a new managed.txt; always refresh the active settings.
        _pendingChanges = true;
        SynchronizeNow();
        if (_connected && !_pendingChanges && !string.IsNullOrEmpty(_appliedFingerprint))
        {
            string result = wasConnected ? "EQ 설정 파일을 다시 저장했습니다."
                : "EqualizerAPO-XT 설정 파일 연결을 완료했습니다.";
            if (!wasConnected && _apoRoot is not null)
            {
                string? backup = ApoBridge.LastConnectionBackup(_apoRoot);
                result += "\n\n이전 설정 백업 위치:\n" + (backup ?? _apoRoot);
            }
            if (ProbeSelectedEndpoint() == EndpointApoRegistration.Missing)
            {
                _status.Text = "출력 장치 등록이 필요합니다.";
                _bridgeStatus.Text = "출력 장치 등록 필요";
                if (showOutputGuidance) ShowOutputActivationGuidance(result);
            }
            else if (showOutputGuidance) ShowOutputActivationGuidance(result);
            else _status.Text = "EQ 설정 연결 완료";
        }
        else
        {
            MessageBox.Show(this, "음향 설정을 적용하지 못했습니다. 연결 상태와 설정 폴더 권한을 확인해 주세요.",
                "음향 설정", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ConnectApo()
    {
        _apoRoot = ApoBridge.DiscoverConfig(_state);
        if (_apoRoot is null)
        {
            MessageBox.Show("EqualizerAPO-XT 설치 및 설정 폴더를 확인하지 못했습니다. XT 설치와 출력 장치 등록을 먼저 확인하세요.",
                "APO 연결", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!_connected)
        {
            var unsupported = ApoBridge.GetUnmanagedRootCommands(_apoRoot);
            if (unsupported.Count > 0)
            {
                MessageBox.Show("알 수 없는 최상단 설정을 자동으로 분리할 수 없습니다. 기존 설정은 변경하지 않았습니다.\n\n"
                    + string.Join("\n", unsupported.Take(5)), "APO 연결 확인",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // The initial XT config contains sample Preamp/GraphicEQ/Include entries.
            // Back it up automatically. No duplicate confirmation dialogs for a
            // user-initiated connection; the successful operation shows its path.
            SaveState();
            ApoBridge.RunElevated("--bridge-install", _apoRoot);
        }
        _connected = ApoBridge.IsConnected(_apoRoot);
        if (!_connected) throw new InvalidOperationException("설정에 APO 연결이 반영되지 않았습니다.");
        _bridgeStatus.Text = "APO 설정 연결됨";
        _pendingChanges = true;
        _applyTimer.Stop();
        // The caller performs one complete render and verifies the written file.
    }
    private void RepairApo()
    {
        _apoRoot = ApoBridge.DiscoverConfig(_state);
        if (_apoRoot is null) { MessageBox.Show("XT 설치 경로를 찾지 못했습니다."); return; }
        if (MessageBox.Show("음향 장치 연결을 다시 설정하시겠습니까?\n\n이전 감쇄 효과가 해제되면서 음량이 커질 수 있습니다. 먼저 볼륨을 낮춰 주세요. 관리자 권한 승인이 필요할 수 있습니다.", "APO 연결 재구성",MessageBoxButtons.YesNo,MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _applyTimer.Stop();
        SettingsStore.Save(_state);
        ApoBridge.RunElevated("--bridge-install",_apoRoot);
        CheckBridge();
        if (!_connected) throw new IOException("APO 재구성 후 연결 확인 실패");
        SynchronizeNow();
        if (_connected && !_pendingChanges && !string.IsNullOrEmpty(_appliedFingerprint))
            ShowOutputActivationGuidance("Equalizer APO 설정 파일 연결을 재구성했습니다.");
        else
            MessageBox.Show(this, "연결은 완료됐지만 음향 설정을 적용하지 못했습니다. 다시 시도해 주세요.",
                "Equalizer APO 연결", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    private void InstallXt()
    {
        if (_xtInstallBusy) return;

        // Detect an actual XT installation, not merely a saved path or config folder.
        // Users can intentionally reinstall XT, but must explicitly approve it.
        if (FindXtSelector() is not null)
        {
            var reinstall = MessageBox.Show(this,
                "EqualizerAPO-XT가 이미 설치되어 있습니다.\n\n" +
                "정상적으로 사용 중이라면 다시 설치할 필요가 없습니다.\n" +
                "재설치 또는 업데이트를 진행하시겠습니까?\n\n" +
                "재설치 후 출력 장치 등록 상태를 다시 확인해야 할 수 있습니다.",
                "EqualizerAPO-XT 재설치 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (reinstall != DialogResult.Yes) return;
        }

        // EqualizerAPO-XT is a separate application. Explain this before
        // downloading or launching its installer, not at every app startup.
        using var dialog = new Form
        {
            Text = "EqualizerAPO-XT 설치 전 안내",
            ClientSize = new Size(740, 345),
            MinimumSize = new Size(740, 345),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            BackColor = _bg,
            ForeColor = _fg,
            Font = Font
        };

        dialog.Controls.Add(new Label
        {
            Text = "EqualizerAPO-XT 설치 안내",
            Location = new Point(24, 19),
            Size = new Size(690, 35),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = _fg
        });

        dialog.Controls.Add(new Label
        {
            Text = "현재 설치 예정인 EqualizerAPO-XT는 별도의 프로그램으로 음향 효과 설정을 실제 적용시켜 주는 앱입니다.",
            Location = new Point(24, 66),
            Size = new Size(688, 55),
            Font = new Font("Segoe UI", 10f),
            ForeColor = _fg
        });

        // This warning is deliberately bold; it must not be mistaken for a
        // minor status message or a background diagnostic disclaimer.
        dialog.Controls.Add(new Label
        {
            Text = "Infinity Audio Equalizer Pro를 제거한 이후 음량이 작아지거나 음색이 달라진 경우 " +
                   "EqualizerAPO-XT(EQ APO XT AVX2) 구성 편집기에서 프리앰프와 EQ 설정을 확인해 주세요.\n\n" +
                   "Infinity Audio Equalizer Pro를 제거해도 EqualizerAPO-XT는 같이 제거되지 않기 때문에 " +
                   "필요한 경우 또는 기존 사운드가 돌아오지 않는 경우 Windows의 설치된 앱에서 별도로 제거하시기 바랍니다.",
            Location = new Point(24, 131),
            Size = new Size(688, 126),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = _fg
        });

        var proceed = new AccentButton
        {
            Text = "해당 사항을 인지하였고 설치 진행하겠습니다.",
            Location = new Point(24, 283),
            Size = new Size(486, 40),
            DialogResult = DialogResult.OK
        };
        var cancel = new AccentButton
        {
            Text = "취소",
            Location = new Point(522, 283),
            Size = new Size(190, 40),
            DialogResult = DialogResult.Cancel
        };
        dialog.Controls.Add(proceed);
        dialog.Controls.Add(cancel);
        dialog.AcceptButton = proceed;
        dialog.CancelButton = cancel;

        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _status.Text = UiStatusCatalog.Downloading;
        _ = DownloadAndLaunchXt();
    }

    private async Task DownloadAndLaunchXt()
    {
        if (_xtInstallBusy) return;
        _xtInstallBusy = true;
        try
        {
            _status.Text = UiStatusCatalog.Downloading;
            var setup = await XtSetupService.DownloadVerifiedAsync();
            using var installer = XtSetupService.RunInstaller(setup);
            _status.Text = "EqualizerAPO-XT 설치 중...";
            await installer.WaitForExitAsync();
            if (IsDisposed || Disposing) return;
            if (installer.ExitCode != 0)
            {
                _status.Text = "설치가 완료되지 않았습니다.";
                MessageBox.Show(this, "설치가 완료되지 않았습니다. 다시 시도해 주세요.",
                    "EqualizerAPO-XT 설치", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // The XT installer may start its own Editor/Device Selector after
            // its bootstrap process exits. A transient registry/path lookup
            // failure must NOT open a second dialog over XT's setup UI.
            // Allow Windows' installed-app entry/current directory a short
            // opportunity to settle, without launching anything or blocking UI.
            string? selector = FindXtSelector();
            for (int attempt = 0; selector is null && attempt < 8; attempt++)
            {
                await Task.Delay(500);
                if (IsDisposed || Disposing) return;
                selector = FindXtSelector();
            }
            if (selector is not null)
            {
                // XT itself can offer Device Selector. Do not duplicate that
                // prompt or claim endpoint registration is finished here.
                _status.Text = "EqualizerAPO-XT 설치 완료";
            }
            else
            {
                // Some installers finish registering their selected variant
                // after their launcher exits. Explicit user actions use the
                // existing Install / Select Folder choice; never prompt here.
                _status.Text = "설치 후 출력 장치 등록을 진행해 주세요.";
            }
        }
        catch (OperationCanceledException) { _status.Text = "설치가 취소되었습니다."; }
        catch (Exception ex)
        {
            DiagnosticLog.Error("XT 설치", ex);
            _status.Text = "설치 파일을 가져오지 못했습니다.";
            var choice = MessageBox.Show(this,
                "설치 프로그램을 실행하지 못했습니다.\n\n" + ex.Message +
                "\n\n공식 배포 페이지를 여시겠습니까?", "EqualizerAPO-XT 설치",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (choice == DialogResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "https://github.com/115dkk/EqualizerAPO-XT/releases") { UseShellExecute = true });
        }
        finally { _xtInstallBusy = false; }
    }

    private void LocateXtInstallation()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "EqualizerAPO-XT 설치 폴더 선택",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if(dlg.ShowDialog(this)!=DialogResult.OK)return;
        var root=XtSetupService.ValidateInstallationFolder(dlg.SelectedPath);
        if(root is null)
        {
            MessageBox.Show(this,"EqualizerAPO-XT 설치 폴더가 아닙니다.\nEditor.exe와 DeviceSelector.exe가 있는 폴더를 선택해 주세요.",
                "설치 폴더 확인",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;
        }
        _manuallySelectedXtSelector = Path.Combine(root, "DeviceSelector.exe");
        _state.XtInstallDir=root;
        SettingsStore.Save(_state);
        CheckBridge();
        if(_apoRoot is null)
        {
            MessageBox.Show(this,"설치 파일을 확인했습니다. 설정 파일(config.txt)이 있는 폴더를 지정해 주세요.",
                "EqualizerAPO-XT",MessageBoxButtons.OK,MessageBoxIcon.Information);
            ChooseApoPath();
        }
    }

    private DialogResult ShowMissingXtForDeviceSelector()
    {
        using var dialog = new Form
        {
            Text = "EqualizerAPO-XT 설치 경로 안내",
            ClientSize = new Size(665, 180),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = _bg,
            ForeColor = _fg,
            Font = Font
        };
        dialog.Controls.Add(new Label
        {
            Text = "EqualizerAPO-XT가 설치되지 않았다면 설치를 진행하시고, " +
                   "이미 설치되어 있다면 설치 폴더를 선택하시기 바랍니다.",
            Location = new Point(22, 24),
            Size = new Size(620, 72),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = _fg
        });
        void AddOption(string title, int x, int width, DialogResult result)
        {
            dialog.Controls.Add(new AccentButton
            {
                Text = title,
                Location = new Point(x, 115),
                Size = new Size(width, 40),
                DialogResult = result
            });
        }
        AddOption("설치", 22, 195, DialogResult.Yes);
        AddOption("설치 폴더 선택", 232, 216, DialogResult.Retry);
        AddOption("취소", 463, 179, DialogResult.Cancel);
        dialog.CancelButton = dialog.Controls.OfType<Button>().Last();
        return dialog.ShowDialog(this);
    }

    // A single post-registration completion pipeline for both DeviceSelector and
    // explicit ON recovery. Never start DeviceSelector here: registration is a
    // separate, user-approved operation. Recheck actual state after XT exits.
    private async Task<bool> CompleteApoSetupAfterRegistrationAsync(bool selectorWasOpened)
    {
        if (_apoSetupCompletionBusy || IsDisposed || Disposing) return false;
        _apoSetupCompletionBusy = true;
        try
        {
            RefreshDevices();
            string? root = null;
            EndpointApoRegistration endpoint = EndpointApoRegistration.Unknown;
            // XT may publish the selected endpoint and ConfigPath after the
            // DeviceSelector process exits. Both observations can settle in either order.
            for (int attempt = 0; attempt < 12; attempt++)
            {
                if (attempt > 0) await Task.Delay(500);
                if (IsDisposed || Disposing) return false;
                root = FindXtSelector() is null ? null : ApoBridge.DiscoverConfig(_state);
                endpoint = ProbeSelectedEndpoint();
                if (root is not null && endpoint == EndpointApoRegistration.Registered)
                    break;
            }
            if (IsDisposed || Disposing) return false;
            _apoRoot = root;
            if (endpoint == EndpointApoRegistration.Missing)
            {
                _registrationCheckRecommended = true;
                _bridgeStatus.Text = "출력 장치 등록 필요";
                _status.Text = "선택한 출력 장치에 APO가 등록되지 않았습니다.";
                return false; // Never tell the user to register a second time automatically.
            }
            if (root is null)
            {
                _registrationCheckRecommended = true;
                _bridgeStatus.Text = "APO 설정 경로 선택";
                _status.Text = "EqualizerAPO-XT 설정 폴더를 찾지 못했습니다. Equalizer APO 경로에서 선택해 주세요.";
                return false;
            }
            // Finish the SAME operation: bridge include -> render all saved
            // effects -> readback verification. Do not change the master ON/OFF state.
            EnsureConnectedAndApply(showOutputGuidance:false);
            if (!_connected || _pendingChanges || string.IsNullOrEmpty(_appliedFingerprint))
            {
                _registrationCheckRecommended = true;
                return false;
            }
            _registrationCheckRecommended = endpoint != EndpointApoRegistration.Registered;
            _bridgeStatus.Text = "APO 설정 연결됨";
            _status.Text = endpoint == EndpointApoRegistration.Registered
                ? "출력 장치 등록 및 음향 설정 연결 완료"
                : "음향 설정 연결 완료";
            // Unknown endpoint state never claims device registration succeeded.
            return true;
        }
        catch (Exception ex)
        {
            DiagnosticLog.Error(selectorWasOpened ? "등록 후 음향 설정 연결" : "전체 효과 ON 연결 복구", ex);
            _status.Text = "음향 설정 연결 실패";
            return false;
        }
        finally { _apoSetupCompletionBusy = false; }
    }

    private void OpenDeviceSelector() => _ = OpenDeviceSelectorAsync();

    private async Task OpenDeviceSelectorAsync()
    {
        if (_selectorBusy) return;
        _selectorBusy = true;
        try
        {
            var selector = FindXtSelector();
            if (selector is null)
            {
                var answer = ShowMissingXtForDeviceSelector();
                if (answer == DialogResult.Yes)
                {
                    // Existing installation consent dialog still applies here.
                    InstallXt();
                    return;
                }
                if (answer == DialogResult.Retry)
                {
                    LocateXtInstallation();
                    selector = FindXtSelector();
                }
                if (selector is null) return;
            }
            using var process = XtSetupService.RunDeviceSelector(selector);
            _status.Text = "출력 장치 등록 중...";
            await process.WaitForExitAsync();
            if (IsDisposed || Disposing) return;
            // Some selector versions exit with a nonzero code even when the user
            // completed registration. The endpoint evidence outranks exit code.
            if (process.ExitCode != 0)
                DiagnosticLog.Error("XT 장치 선택기 종료 코드", new InvalidOperationException(
                    $"DeviceSelector exit code={process.ExitCode}"));
            bool completed = await CompleteApoSetupAfterRegistrationAsync(selectorWasOpened:true);
            if (!completed && process.ExitCode != 0 &&
                ProbeSelectedEndpoint() != EndpointApoRegistration.Registered)
                _status.Text = "출력 장치 등록이 완료되지 않았습니다.";
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            _registrationCheckRecommended = true;
            _status.Text = "출력 장치 등록이 취소되었습니다.";
        }
        catch (Exception ex)
        {
            DiagnosticLog.Error("출력 장치 등록", ex);
            ShowError(ex);
        }
        finally { _selectorBusy = false; }
    }

    private void DisconnectApo()
    {
        if (_apoRoot is null || !ApoBridge.IsConnected(_apoRoot)) { CheckBridge(); return; }
        if (MessageBox.Show("음향 효과 연결을 해제하시겠습니까?\n\n현재 효과가 제거되고 원음 설정으로 돌아갑니다. EqualizerAPO-XT 자체의 장치 등록은 유지됩니다.",
            "APO 연결 해제", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _applyTimer.Stop();
        ApoBridge.RunElevated("--bridge-remove", _apoRoot);
        _connected = ApoBridge.IsConnected(_apoRoot);
        foreach(var profile in _state.Profiles.Values)
        {
            profile.AmbienceDiagnostic=false;
            profile.DepthComparisonMode="";
        }
        RefreshDepthComparisonButtons();
        RefreshAmbienceTestButton();
        _pendingChanges = false;
        _bridgeStatus.Text = _connected ? "APO 연결 해제 실패" : "음향 설정 연결 필요";
        _appliedFingerprint = "";
        _status.Text = _connected ? "연결 해제 실패" : "음향 효과 연결이 해제되었습니다.";
        if (!_connected) ShowActionComplete("Equalizer APO 연결이 해제되었습니다.", "Equalizer APO 연결");
        else MessageBox.Show(this, "연결을 해제하지 못했습니다. 다시 시도해 주세요.",
                "Equalizer APO 연결", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    private string AppliedEffectStatus()
    {
        if(!_state.GlobalEnabled || _state.BypassAllEffects)
            return "전체 음향 효과가 꺼졌습니다.";
        var p=CurrentProfile();
        if(p is null)return "전체 음향 효과 ON";
        if(!p.Enabled)return "이 장치의 음향 효과가 꺼졌습니다.";
        return _lastChangedEffect switch
        {
            "5밴드 컴프레서" when !p.CompressorEnabled => "5밴드 컴프레서 OFF",
            "5밴드 컴프레서" when p.LowLatency => "5밴드 컴프레서 우회 · 저지연 재생 중",
            "Studio Ambience" when !p.AmbienceEnabled || p.AmbienceStrength==0 => "Studio Ambience OFF",
            "Studio Ambience" when p.LowLatency => "Studio Ambience 우회 · 저지연 재생 중",
            "Surround Stage" when !p.SurroundEnabled || p.SurroundStrength==0 => "Surround Stage OFF",
            "Surround Stage" when p.LowLatency => "Surround Stage 우회 · 저지연 재생 중",
            "31밴드 EQ" when !p.CustomEnabled => "31밴드 EQ OFF",
            "AutoEQ" when !p.AutoEnabled => "AutoEQ OFF",
            "스테레오 확장" when !p.StereoWidthEnabled || p.StereoWidth==0 => "스테레오 확장 OFF",
            "전체 음향 효과" => "전체 음향 효과 ON",
            "장치 음향 효과" => p.Enabled ? "장치 음향 효과 ON" : "장치 음향 효과 OFF",
            "31밴드 EQ" => p.CustomEnabled ? "31밴드 EQ ON" : "31밴드 EQ OFF",
            "AutoEQ" => p.AutoEnabled ? "AutoEQ ON" : "AutoEQ OFF",
            "스테레오 확장" => p.StereoWidthEnabled && p.StereoWidth>0 ? "스테레오 확장 ON" : "스테레오 확장 OFF",
            "5밴드 컴프레서" => "5밴드 컴프레서 ON",
            "Studio Ambience" => "Studio Ambience ON",
            "Surround Stage" => "Surround Stage ON",
            _ => $"{_lastChangedEffect} 적용 완료"
        };
    }
    private void ApplyLive()
    {
        if (!_pendingChanges || !_connected || _apoRoot is null) return;
        try
        {
            // Settings are written independently from the original Python preferences.
            // Re-read current UI inputs, persist the model, then write + verify
            // the identical rendered text that APO will load.
            SettingsStore.Save(_state);
            ApoBridge.Apply(_apoRoot, _state);
            _appliedFingerprint = ApoBridge.ReadManagedHash(_apoRoot);
            _pendingChanges = false;
            _applyRetryCount = 0;
            _applyTimer.Interval = 480;
            _bridgeStatus.Text="APO 설정 연결됨";
            RefreshDepthComparisonButtons();
            RefreshAmbienceTestButton();
            _status.ForeColor=Color.FromArgb(167,216,184);
            _status.Text=AppliedEffectStatus();
            if (_state.GlobalEnabled && !_state.BypassAllEffects)
                UpdateApoRegistrationStatus();
            if (_deviceHint is not null) _deviceHint.Text = UiStatusCatalog.DeviceTestHint;
        }
        catch (Exception ex)
        {
            DiagnosticLog.Error("APO 적용",ex);
            _status.ForeColor=Color.FromArgb(255,160,151);
            _status.Text = ex is UnauthorizedAccessException
                ? $"{_lastChangedEffect} 적용 실패 · 설정 폴더에 접근할 수 없습니다."
                : ex is IOException ? $"{_lastChangedEffect} 적용 실패 · 잠시 후 다시 시도합니다."
                : $"{_lastChangedEffect} 적용 실패 · 연결 상태를 확인해 주세요.";
            _bridgeStatus.Text = "APO 연결됨 · 적용 실패";
            if (ex is not IOException && ex is not UnauthorizedAccessException)
                _status.Text=$"{_lastChangedEffect} 적용 실패. 연결 상태를 확인해 주세요.";
            _pendingChanges = true; // never claim an unsuccessful write was applied
            // Retry after an APO file reader releases its temporary lock. Do not
            // retry permanent access errors indefinitely or claim success.
            if ((ex is IOException || ex is UnauthorizedAccessException) && _applyRetryCount < 2)
            {
                _applyRetryCount++;
                _applyTimer.Interval = 900 * _applyRetryCount;
                _applyTimer.Stop(); _applyTimer.Start();
            }
        }
    }
    private void ExportManaged()
    {
        ChangeCurrent(); using var dlg=new SaveFileDialog{FileName="managed-preview.txt",Filter="Equalizer APO config (*.txt)|*.txt",Title="음향 설정 파일 저장"};
        if(dlg.ShowDialog()!=DialogResult.OK)return;
        File.WriteAllText(dlg.FileName,ApoRenderer.Render(_state),new UTF8Encoding(false));
        _status.Text="APO 설정을 내보냈습니다. 실제 오디오 엔진에는 자동 적용하지 않았습니다.";
    }
    private void RefreshMasterButton()
    {
        bool on=_engine.Checked && !_bypassAll.Checked;
        if (!on)
            foreach (var profile in _state.Profiles.Values)
            {
                profile.DepthComparisonMode="";
                profile.AmbienceDiagnostic=false;
            }
        RefreshAmbienceTestButton();
        _masterButton.Text=on ? "전체 효과 ON" : "전체 효과 OFF";
        _masterButton.Tone=on ? AccentButtonTone.Success : AccentButtonTone.Danger;
        _masterButton.ForeColor=Color.White;
        RefreshDepthComparisonButtons();
    }
    // Shared by tray actions and the second-launch single-instance notification.
    internal void RestoreFromSecondLaunch() => RestoreWindow();
    private void RestoreWindow()
    {
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        if (!Visible) Show();
        BringToFront();
        Activate();
    }
    private void ExitFromTray()
    {
        if (_reallyExit) return; // Ignore repeated clicks during shutdown.
        _reallyExit=true;
        _applyTimer.Stop();
        _deviceTimer.Stop();
        // The tray command is an unconditional request to terminate the app.
        // Losing the EqualizerAPO-XT path must never trap the user in a popup loop.
        bool oldEngine=_state.GlobalEnabled, oldBypass=_state.BypassAllEffects;
        try
        {
            _state.GlobalEnabled=false;
            _state.BypassAllEffects=true;
            if(_connected && !string.IsNullOrWhiteSpace(_apoRoot))
                ApoBridge.Apply(_apoRoot, _state);
        }
        catch(Exception ex)
        {
            // APO configuration can be absent, locked or corrupt during repair/uninstall.
            // Record the failure without blocking shutdown or claiming bypass succeeded.
            DiagnosticLog.Error("트레이 종료 중 음향 효과 해제", ex);
        }
        finally
        {
            // Temporary shutdown bypass is not a saved user preference.
            _state.GlobalEnabled=oldEngine;
            _state.BypassAllEffects=oldBypass;
            try { SettingsStore.Save(_state); }
            catch(Exception ex) { DiagnosticLog.Error("트레이 종료 중 설정 저장", ex); }
            // Hide the icon immediately so no new context menu action can be queued.
            _tray.Visible=false;
            Close();
        }
    }
    private static bool StartupEnabled()
    {
        try{using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");return key?.GetValue("InfinityAudioEqualizerProWindows") is string;}catch{return false;}
    }
    private static string StartupCommand() => "\"" + Environment.ProcessPath + "\" --startup";

    private static void UpgradeExistingStartupCommand()
    {
        // Preserve the user's existing autostart choice across this update.
        // Only migrate a Run value owned by this exact executable path.
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key?.GetValue("InfinityAudioEqualizerProWindows") is not string existing ||
                string.IsNullOrWhiteSpace(Environment.ProcessPath)) return;
            string oldCommand = "\"" + Environment.ProcessPath + "\"";
            if (string.Equals(existing.Trim(), oldCommand, StringComparison.OrdinalIgnoreCase))
                key.SetValue("InfinityAudioEqualizerProWindows", StartupCommand());
        }
        catch (Exception ex)
        {
            DiagnosticLog.Error("Windows 로그인 자동 실행 명령 업데이트", ex);
        }
    }

    private void SetStartup(bool on)
    {
        try
        {
            using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true);
            if(key is null)throw new InvalidOperationException("Windows 시작 프로그램 설정을 열 수 없습니다.");
            if(on)key.SetValue("InfinityAudioEqualizerProWindows", StartupCommand());
            else key.DeleteValue("InfinityAudioEqualizerProWindows",false);
            _state.Startup=on;SettingsStore.Save(_state);
        }
        catch(Exception ex){_loading=true;_startup.Checked=!on;_loading=false;ShowError(ex);}
    }
    private void RefreshDevicesIfChanged()
    {
        if(_loading || _compressorDialogOpen || _devices.DroppedDown || !Visible)return;
        try
        {
            var devices=AudioDeviceEnumerator.GetActiveRenderDevices();
            var signature=DeviceSelectionPolicy.Signature(devices);
            string? defaultId=AudioDeviceEnumerator.GetDefaultRenderDeviceId();
            bool defaultChanged=!string.IsNullOrEmpty(_observedDefaultDeviceId) &&
                !string.IsNullOrEmpty(defaultId) &&
                !string.Equals(defaultId,_observedDefaultDeviceId,StringComparison.OrdinalIgnoreCase);
            if(signature!=_deviceSignature || defaultChanged)RefreshDevices();
            else
            {
                var p=CurrentProfile();
                if(p is {AmbienceRateAuto:true}) UpdateSelectedSampleRate(true);
            }
        }
        catch { /* A transient Bluetooth disconnect should not crash the UI. */ }
    }
    private void ShowStatusHelp()
    {
        using var dialog = new Form { Text="출력 장치 · APO 상태 안내", Size=new Size(790,530),
            StartPosition=FormStartPosition.CenterParent, BackColor=_bg, ForeColor=_fg };
        using var viewer = new TextBox { Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,
            Dock=DockStyle.Fill,BackColor=Color.FromArgb(25,33,46), ForeColor=_fg,
            Font=new Font("Segoe UI",10),Text=UiStatusCatalog.HelpText };
        dialog.Controls.Add(viewer);
        dialog.ShowDialog(this);
    }

    private void ShowInitialApoGuidance(bool force=false)
    {
        if (_startupGuideShown && !force) return;
        if (!force) _startupGuideShown=true;
        // Do not mistake an orphaned config.txt for an installed XT engine.
        var selector=FindXtSelector();
        var root=selector is null ? null : ApoBridge.DiscoverConfig(_state);
        if (root is not null) return;
        if (selector is not null)
        {
            _status.Text=UiStatusCatalog.MissingConfig;
            if (_deviceHint is not null) _deviceHint.Text="EqualizerAPO-XT의 설정 폴더(config.txt)를 먼저 확인해 주세요.";
            var selection=MessageBox.Show(this,
                "EqualizerAPO-XT 설치를 확인했지만 설정 파일(config.txt)의 위치를 찾지 못했습니다.\n\n설정 폴더를 직접 선택하시겠습니까?",
                "EqualizerAPO-XT 설정 경로 확인",MessageBoxButtons.YesNo,MessageBoxIcon.Information);
            if (selection==DialogResult.Yes) ChooseApoPath();
            return;
        }
        _status.Text=UiStatusCatalog.XtMissing;
        if (_deviceHint is not null) _deviceHint.Text=UiStatusCatalog.InstallOrder;
        using var dialog = new Form { Text="EqualizerAPO-XT 설치 안내", ClientSize=new Size(565,185),
            StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,
            MaximizeBox=false,MinimizeBox=false,BackColor=_bg,ForeColor=_fg };
        dialog.Controls.Add(new Label {Text="음향 효과를 사용하려면 EqualizerAPO-XT 설치가 필요합니다.",
            Location=new Point(22,20),Size=new Size(525,29),Font=new Font("Segoe UI",10,FontStyle.Bold)});
        dialog.Controls.Add(new Label {Text="설치 후 현재 출력 장치를 등록해 주세요.\n나중에 설치해도 프리셋 편집과 저장은 가능합니다.",
            Location=new Point(22,56),Size=new Size(525,48)});
        Button Option(string text,int x,int width,DialogResult result)
        {
            var button=new AccentButton {Text=text,Location=new Point(x,124),Size=new Size(width,34),
                DialogResult=result};
            dialog.Controls.Add(button);return button;
        }
        Option("지금 설치",22,142,DialogResult.Yes);
        Option("이미 설치됨 · 경로 찾기",173,220,DialogResult.Retry);
        var later=Option("나중에",402,141,DialogResult.Cancel);
        dialog.CancelButton=later;
        switch(dialog.ShowDialog(this))
        {
            case DialogResult.Yes: InstallXt(); break;
            case DialogResult.Retry: LocateXtInstallation(); break;
        }
    }

    private void ShowAbout()
    {
        using var dialog = new ProductAboutDialog();
        dialog.ShowDialog(this);
    }
    private void ShowEqGainDiagnostics()
    {
        var p=CurrentProfile();
        if(p is null){MessageBox.Show(this,"출력 장치를 먼저 선택하세요.","EQ 진단");return;}
        // Do not change or write a single user DSP setting to run this test.
        var sb=new StringBuilder();
        sb.AppendLine("EQ 중첩·출력 여유 진단 (현재 선택 장치)").AppendLine();
        sb.AppendLine("장치: "+p.Name);
        sb.AppendLine("전체 엔진: "+(_state.GlobalEnabled&&!_state.BypassAllEffects?"ON":"OFF 또는 우회"));
        sb.AppendLine("AutoEQ: "+(p.AutoEnabled?"ON":"OFF")+" · "+(string.IsNullOrWhiteSpace(p.Source)?"모델 미선택":p.Source));
        sb.AppendLine("사용자 31밴드 EQ: "+(p.CustomEnabled?"ON":"OFF"));
        sb.AppendLine("컴프레서: "+(p.CompressorEnabled&&!p.LowLatency?"ON (실제 DSP 여부는 별도 확인)":"OFF 또는 우회"));
        sb.AppendLine("Surround Stage: "+(p.SurroundEnabled&&!p.LowLatency?"ON":"OFF 또는 우회"));
        sb.AppendLine("프리앰프: "+PreampCalculator.Calculate(p).Final.ToString("+0.0;-0.0;0.0")+" dB (장치 EQ 상태에 따름)");
        sb.AppendLine();
        sb.AppendLine("예상 정적 EQ 주파수 응답 (dB, 원음 대비)");
        sb.AppendLine("주파수      AutoEQ     추가 31밴드    합계");
        sb.AppendLine("---------------------------------------------");
        double[] hzList=[20,31.5,40,63,80,100,125,250,500,1000,2000,4000,8000,10000,12500,16000,20000];
        foreach(double hz in hzList)
        {
            var r=PreampCalculator.ResponseAt(p,hz);
            sb.AppendLine($"{hz,8:0.#} {r.AutoEqDb,10:+0.0;-0.0;0.0} {r.UserEqDb,14:+0.0;-0.0;0.0} {r.CombinedDb,10:+0.0;-0.0;0.0}");
        }
        var peak=PreampCalculator.AnalyzePeak(p);
        double preamp=PreampCalculator.Calculate(p).Final;
        sb.AppendLine();
        sb.AppendLine($"예상 EQ 합산 최대 이득: {peak.PeakDb:+0.0;-0.0;0.0} dB (약 {peak.PeakFrequencyHz:0} Hz)");
        sb.AppendLine($"프리앰프 포함 예상 최대 이득: {peak.PeakDb+preamp:+0.0;-0.0;0.0} dB");
        if(peak.PeakDb+preamp>0.2)
            sb.AppendLine("주의: EQ 최대 증폭에 비해 출력 여유가 부족할 수 있습니다. 높은 입력 레벨에서 클리핑 가능성이 있습니다.");
        else sb.AppendLine("정적 EQ 기준 최대 이득은 0 dB 이하입니다. 실제 출력 안전성을 보장하지는 않습니다.");
        sb.AppendLine();
        sb.AppendLine("비교 방법: '추가 31밴드 EQ 비교' 버튼으로 해당 효과만 OFF/ON 해보세요.");
        sb.AppendLine("AutoEQ·프리앰프 설정은 그대로 유지되며 31개 슬라이더 값도 보존됩니다.");
        sb.AppendLine("이 분석은 추정값입니다. VST3, 컨볼루션, Windows 음향 효과, 출력 장치 자체 처리 및 실제 신호 피크는 측정하지 않습니다.");
        using var dialog=new Form{Text="AutoEQ · 31밴드 합산 및 출력 여유 진단",Width=900,Height=690,
            MinimumSize=new Size(720,480),StartPosition=FormStartPosition.CenterParent,
            BackColor=_bg,ForeColor=_fg};
        var output=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,
            WordWrap=false,Font=new Font("Consolas",10),BackColor=Color.FromArgb(35,43,59),ForeColor=Color.White,
            Text=sb.ToString()};
        var close=new Button{Dock=DockStyle.Bottom,Height=36,Text="닫기",DialogResult=DialogResult.OK};
        dialog.Controls.Add(output);dialog.Controls.Add(close);
        dialog.ShowDialog(this);
    }
    private void ShowEffectDiagnostics()
    {
        // The generated APO file is not proof that an actual Windows audio device
        // is running a VST or convolution plugin. Show precisely what can be checked.
        SaveState();
        var p=CurrentProfile();
        string root=_apoRoot ?? "";
        bool linked=!string.IsNullOrEmpty(root) && ApoBridge.IsConnected(root);
        bool isolated=linked && ApoBridge.IsSessionIsolated(root);
        bool globalActive=_state.GlobalEnabled && !_state.BypassAllEffects;
        var lines=new List<string>{
            "Infinity Audio Equalizer Pro for Windows — 효과 적용 상태",
            "",
            "전체 엔진: "+(_state.GlobalEnabled?"ON":"OFF"),
            "전체 우회: "+(_state.BypassAllEffects?"ON (모든 효과 해제)":"OFF"),
            "APO 설정 연결: "+(linked?"연결 확인":"미연결"),
            "APO 설정 분리: "+(!linked?"미연결":isolated?"설정됨":"설정되지 않음"),
            "선택 장치: "+(p?.Name ?? "없음"),
            "장치 프로필: "+(p?.Enabled==true?"ON":"OFF"),
            ""
        };
        if(p is not null)
        {
            bool active=globalActive && linked && isolated && p.Enabled;
            string Status(bool enabled,bool ready=true)=>!enabled?"OFF":!active?"현재 사용 안 함":!ready?"현재 사용 안 함":"설정됨";
            lines.Add("31밴드 EQ: "+Status(p.CustomEnabled));
            lines.Add("AutoEQ: "+Status(p.AutoEnabled));
            lines.Add("프리앰프: "+Status(p.Enabled)+" · "+PreampCalculator.Calculate(p).Final.ToString("+0.0;-0.0;0.0")+" dB");
            lines.Add("스테레오 확장: "+Status(p.StereoWidthEnabled && p.StereoWidth>0));
            lines.Add("5밴드 컴프레서: "+Status(p.CompressorEnabled,!p.LowLatency));
            lines.Add("Surround Stage: "+Status(p.SurroundEnabled && p.SurroundStrength>0,!p.LowLatency));
            lines.Add("Studio Ambience: "+Status(p.AmbienceEnabled && p.AmbienceStrength>0,!p.LowLatency));
            if(p.SurroundEnabled && p.SurroundStrength>0)
            {
                lines.Add("Surround Stage 처리 모드: "+(p.SurroundBinaural?(p.SurroundBalanced?"균형형 공간감":p.SurroundStable?"원음 안정성 우선":"HRTF 공간감"):"기본 공간 확장"));
                if(!p.SurroundBinaural)
                {
                    var (own,cross)=ImpulseResponseService.SurroundMatrix(p);
                    lines.Add($"Surround Stage 신호 행렬: L={own:0.###}L-{cross:0.###}R, R={own:0.###}R-{cross:0.###}L");
                    lines.Add($"Surround Stage 기준: 동일한 L/R 입력은 {own-cross:0.###}배 (중앙 신호 유지), 반대 위상 입력은 최대 {own+cross:0.###}배");
                    lines.Add("Surround Stage 임펄스: "+ImpulseResponseService.SurroundFile(p));
                }
                lines.Add("전후 방향 분리 Stage3: "+(p.Surround3Enabled?"ON · 실측 HRIR 사용":"OFF"));
                lines.Add("주의: 공간 확장은 반대 위상 성분을 증폭할 수 있습니다. 컴프레서/잔향과 결합하면 체감 음량이 크게 달라질 수 있습니다.");
            }
            if((p.SurroundEnabled || p.AmbienceEnabled) && !p.LowLatency)
                lines.Add($"공간 음향 컨볼루션 샘플레이트: {p.AmbienceRate:N0} Hz (실제 장치 믹스 레이트와 같다는 보장은 없음)");
            if(p.LowLatency)lines.Add("저지연 모드: ON — VST3/공간 컨볼루션 우회");
            lines.Add("");
            if(linked)
            {
                string plugin=Path.Combine(root,ApoBridge.Folder,"InfinityCompressor.vst3","Contents","x86_64-win","InfinityCompressor.vst3");
                lines.Add("컴프레서 파일: "+(File.Exists(plugin)?"존재":"없음"));
                string file=ApoBridge.ManagedPath(root);
                if(File.Exists(file))
                {
                    string actual=File.ReadAllText(file);
                    string expected=ApoRenderer.Render(_state,root);
                    lines.Add("APO 파일 내용: "+(actual==expected?"현재 UI/저장값과 일치":"불일치 (설정 다시 적용 필요)"));
                    var selected=ApoRenderer.RenderProfile(p,Path.Combine(root,ApoBridge.Folder,"InfinityCompressor.vst3"));
                    lines.Add("선택 장치용 필터: "+selected.Count+"개 명령");
                    foreach(var item in selected)lines.Add("  "+item);
                    if(p.SurroundEnabled && !p.LowLatency && p.SurroundStrength>0)
                    {
                        var referenced=selected.Where(x=>x.StartsWith("Convolution:",StringComparison.Ordinal))
                            .Select(x=>x["Convolution:".Length..].Trim());
                        foreach(string name in referenced)
                        {
                            string ir=Path.Combine(root,ApoBridge.Folder,name);
                            lines.Add(name+" · "+(File.Exists(ir)?"FIR 파일 존재":"FIR 파일 없음 - 적용 실패 가능"));
                        }
                    }
                    if(p.AmbienceEnabled && !p.LowLatency && p.AmbienceStrength>0)
                    {
                        string ir=Path.Combine(root,ApoBridge.Folder,ImpulseResponseService.AmbienceFile(p));
                        lines.Add("Studio Ambience IR 파일: "+(File.Exists(ir)?"생성 확인":"없음 - 적용 실패 가능"));
                    }
                }
            }
        }
        lines.Add("");
        lines.Add("소리가 달라지지 않으면 출력 장치 등록 및 Windows 음향 효과 설정을 확인해 주세요.");
        using var dialog=new Form{Text="음향 효과 상태",Width=970,Height=650,
            MinimumSize=new Size(760,420),StartPosition=FormStartPosition.CenterParent,
            BackColor=_bg,ForeColor=_fg,Font=Font};
        var output=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,
            ScrollBars=ScrollBars.Both,WordWrap=false,BackColor=Color.FromArgb(33,40,55),
            ForeColor=Color.White,Font=new Font("Consolas",10),Text=string.Join(Environment.NewLine,lines)};
        var close=new Button{Dock=DockStyle.Bottom,Height=40,Text="닫기",DialogResult=DialogResult.OK,
            BackColor=Color.FromArgb(55,65,85),ForeColor=Color.White};
        dialog.Controls.Add(output);dialog.Controls.Add(close);
        dialog.AcceptButton=close;
        dialog.ShowDialog(this);
    }
    private void ShowCompressorDetails()
    {
        var p=CurrentProfile();
        if (p is null)return;
        string originalTheme=p.CompressorTheme;
        int originalStrength=p.CompressorStrength;
        var originalBands=p.CompressorCustomBands.Select(b=>CompressorBand.From(b.ToRuntime())).ToList();
        _compressorDialogOpen=true;
        try
        {
            // A modal dialog receives live changes, but never owns the canonical
            // profile. Cancelling restores BOTH saved profile and active APO output.
            using var dialog=new CompressorDetailsDialog(p,bands=>
            {
                p.CompressorCustomBands=bands.Select(CompressorBand.From).ToList();
                p.CompressorTheme="CUSTOM";
                _loading=true;
                try { _compressorTheme.SelectedItem=CompressorThemes.DisplayName("CUSTOM"); }
                finally { _loading=false; }
                SettingsStore.Save(_state);
                if(_connected)
                {
                    _lastChangedEffect="5밴드 컴프레서";
                    _pendingChanges=true;
                    _applyRetryCount=0;
                    _applyTimer.Stop();
                    _applyTimer.Interval=200;
                    _applyTimer.Start();
                }
            });
            bool accept=dialog.ShowDialog(this)==DialogResult.OK && dialog.ResultBands is not null;
            if(accept)
            {
                p.CompressorCustomBands=dialog.ResultBands!.Select(CompressorBand.From).ToList();
                p.CompressorTheme="CUSTOM";
            }
            else
            {
                p.CompressorTheme=originalTheme;
                p.CompressorStrength=originalStrength;
                p.CompressorCustomBands=originalBands;
            }
            _loading=true;
            try { _compressorTheme.SelectedItem=CompressorThemes.DisplayName(p.CompressorTheme); }
            finally { _loading=false; }
            SettingsStore.Save(_state);
            _lastChangedEffect="5밴드 컴프레서";
            _applyTimer.Stop();
            if(_connected) SynchronizeNow();
            else _status.Text=accept ? "컴프레서 설정을 저장했습니다." : "컴프레서 설정을 복원했습니다.";
            if(!accept && !_pendingChanges && _connected)
                _status.Text="컴프레서 설정을 이전 값으로 복원했습니다.";
        }
        finally { _compressorDialogOpen=false; }
    }

    // Open the path picker at the active XT configuration folder when it is known.
    // Displaying a suggested location never changes the user's saved settings;
    // only an explicit OK selection updates ApoConfig.
    private string? PreferredApoPathForPicker()
    {
        // DiscoverConfig prioritizes the registered config root over saved hints.
        // A previously saved path is only a fallback when discovery is unavailable.
        var paths = new[] { ApoBridge.DiscoverConfig(_state), _state.ApoConfig, _apoRoot };
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            try
            {
                var full = Path.GetFullPath(path);
                if (Directory.Exists(full) && File.Exists(Path.Combine(full, "config.txt")))
                    return full;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or ArgumentException or NotSupportedException or PathTooLongException)
            {
                DiagnosticLog.Error("APO 경로 탐색", ex);
            }
        }
        return null;
    }

    private void ChooseApoPath()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Equalizer APO 설정 폴더 (config.txt가 있는 위치)",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        var initialPath = PreferredApoPathForPicker();
        if (initialPath is not null) dlg.SelectedPath = initialPath;
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!File.Exists(Path.Combine(dlg.SelectedPath, "config.txt")))
        {
            MessageBox.Show(this, "선택한 폴더에 config.txt가 없습니다.",
                "Equalizer APO 경로", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _state.ApoConfig = Path.GetFullPath(dlg.SelectedPath);
        SettingsStore.Save(_state);
        CheckBridge();
        ShowActionComplete("Equalizer APO 설정 경로를 저장했습니다.", "설정 경로");
    }
    private void ExportSettings()
    {
        ChangeCurrent();
        using var choice = new SettingsExportDialog();
        var decision = choice.ShowDialog(this);
        if(decision is not (DialogResult.Yes or DialogResult.No)) return;
        bool sharing = decision == DialogResult.Yes;
        DeviceProfile? p=CurrentProfile();
        if(sharing && p is null){MessageBox.Show("현재 출력 장치가 선택되지 않았습니다.");return;}
        using var dlg=new SaveFileDialog{FileName=sharing?"InfinityAudioEqualizerPro-shared-effects.json":"InfinityAudioEqualizerPro-settings-backup.json",Filter="JSON 파일 (*.json)|*.json",Title="설정 내보내기"};
        if(dlg.ShowDialog(this)!=DialogResult.OK)return;
        string json=sharing?EffectTransfer.ExportShare(p!):JsonSerializer.Serialize(_state,SettingsStore.JsonOptions);
        File.WriteAllText(dlg.FileName,json,new UTF8Encoding(false));
        _status.Text=sharing?"음향 효과 공유 파일을 저장했습니다. 장치 ID와 개인 경로는 제외됩니다.":"전체 설정 백업을 저장했습니다. 개인 PC 정보가 포함될 수 있습니다.";
    }
    private void ImportSettings()
    {
        using var dlg=new OpenFileDialog{Filter="JSON 파일 (*.json)|*.json",Title="설정 불러오기"};
        if(dlg.ShowDialog(this)!=DialogResult.OK)return;
        string content=File.ReadAllText(dlg.FileName);
        if(EffectTransfer.IsShare(content))
        {
            if(string.IsNullOrWhiteSpace(_selectedId)){MessageBox.Show("먼저 적용할 출력 장치를 선택하세요.");return;}
            var imported=EffectTransfer.ImportShare(content);
            if(MessageBox.Show("현재 장치의 AutoEQ는 유지하고 다른 음향 효과 설정을 가져옵니다. 계속할까요?","공유 설정 적용",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            _state.Profiles[_selectedId]=EffectTransfer.MergeIntoDevice(CurrentProfile() ?? new DeviceProfile(), imported);
        }
        else
        {
            var candidate=JsonSerializer.Deserialize<AppSettings>(content,SettingsStore.JsonOptions)
                ??throw new InvalidDataException("설정 파일 형식이 올바르지 않습니다.");
            if(candidate.Profiles is null)throw new InvalidDataException("장치 프로필이 없습니다.");
            if(MessageBox.Show("내 컴퓨터용 전체 백업을 복원합니다. 현재 설정이 변경됩니다. 계속할까요?","전체 설정 복원",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
            _state=candidate;
            _state.ApoConfig=""; // Never redirect to a foreign machine's protected config folder.
        }
        SettingsStore.Save(_state);RefreshDevices();SelectDevice();RefreshMasterButton();
        if(_connected)SynchronizeNow();
    }

}
