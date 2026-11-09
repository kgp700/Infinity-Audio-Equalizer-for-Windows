// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Drawing;

namespace InfinityAudioEqualizerPro;

/// <summary>Shared manual and automatic per-device recovery UI.</summary>
public sealed class RecoverySettingsDialog : Form
{
    private sealed record DeviceOption(AudioDevice Device) { public override string ToString()=>Device.Name; }
    private sealed record SourceOption(RecoveryBackupStore.RecoveryEntry Entry)
    {
        public override string ToString()
        {
            string when=Entry.SavedAt==DateTime.MinValue?"시각 정보 없음":Entry.SavedAt.ToString("yyyy-MM-dd HH:mm:ss");
            string label=Entry.FromSnapshot?"백업: "+Entry.Source:"현재 저장: "+Entry.Source;
            string timeLabel=Entry.FromSnapshot?"백업 시각":"설정 파일 수정";
            return $"{Entry.DeviceName}  |  {label}  |  {timeLabel}: {when}";
        }
    }
    private readonly ComboBox _target=new() {DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top,Height=32};
    private readonly ListBox _sources=new() {Dock=DockStyle.Fill,IntegralHeight=false};
    private Button? _restoreButton;
    private readonly CheckBox _all=new() {Text="다른 장치에서 사용한 설정도 표시",AutoSize=true};
    private readonly Label _details=new() {Dock=DockStyle.Bottom,Height=52,ForeColor=Color.FromArgb(185,206,232)};
    private readonly List<RecoveryBackupStore.RecoveryEntry> _entries;
    public string? TargetId=> (_target.SelectedItem as DeviceOption)?.Device.Id;
    public RecoveryBackupStore.RecoveryEntry? SelectedEntry=>(_sources.SelectedItem as SourceOption)?.Entry;

    public RecoverySettingsDialog(IReadOnlyList<AudioDevice> devices, string? selectedId,
        List<RecoveryBackupStore.RecoveryEntry> entries, bool automatic)
    {
        Text=automatic?"새 출력 장치 · 기존 음향 설정 복구":"기존 음향 설정 복구";
        Size=new Size(790,535);MinimumSize=new Size(650,445);
        StartPosition=FormStartPosition.CenterParent;
        BackColor=Color.FromArgb(21,26,36);ForeColor=Color.FromArgb(230,238,250);
        Font=new Font("Segoe UI",9.5f);ShowInTaskbar=false;
        _entries=entries;
        var header=new Label {Dock=DockStyle.Top,Height=86,Padding=new Padding(16,13,16,4),
            Text=automatic
                ?"새 Windows 출력 장치가 감지되었습니다. 이전에 사용한 음향 설정을 복원할 수 있습니다.\n복구하지 않아도 기존 설정과 백업은 삭제되지 않습니다."
                :"현재 장치에 기존 음향 설정을 복원할 수 있습니다.\n복구할 설정이 사용되었던 장치와 저장 시각을 확인하세요."};
        var footer=new Panel{Dock=DockStyle.Bottom,Height=76,Padding=new Padding(13,9,13,10),BackColor=Color.FromArgb(31,40,55)};
        var actions=new FlowLayoutPanel{Dock=DockStyle.Right,Width=365,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};
        var restore=new Button{Text="선택한 설정 복구",Width=165,Height=36,BackColor=Color.FromArgb(44,85,143),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
        var close=new Button{Text=automatic?"새 설정 사용":"닫기",Width=120,Height=36};
        _restoreButton=restore;
        actions.Controls.Add(restore);actions.Controls.Add(close);
        footer.Controls.Add(actions);
        close.Click+=(_,_)=>{DialogResult=DialogResult.Cancel;Close();};
        restore.Click+=(_,_)=>
        {
            if(TargetId is null || SelectedEntry is null)
            {MessageBox.Show(this,"복구할 장치와 설정을 선택해주세요.","기존 설정 복구");return;}
            DialogResult=DialogResult.OK;Close();
        };
        CancelButton=close;
        var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(16,4,16,8)};
        var top=new Panel{Dock=DockStyle.Top,Height=88};
        var targetLabel=new Label{Text="설정을 적용할 출력 장치",Dock=DockStyle.Top,Height=28};
        top.Controls.Add(_target);top.Controls.Add(targetLabel);
        foreach(var device in devices)_target.Items.Add(new DeviceOption(device));
        for(int i=0;i<_target.Items.Count;i++)
            if(_target.Items[i] is DeviceOption d && d.Device.Id.Equals(selectedId,StringComparison.OrdinalIgnoreCase))
            {_target.SelectedIndex=i;break;}
        if(_target.SelectedIndex<0 && _target.Items.Count>0)_target.SelectedIndex=0;
        var listHeader=new Label{Text="복구 가능한 장치별 음향 설정",Dock=DockStyle.Top,Height=29};
        var toggle=new Panel{Dock=DockStyle.Top,Height=37};
        _all.Location=new Point(0,6);toggle.Controls.Add(_all);
        _sources.BackColor=Color.FromArgb(30,39,54);_sources.ForeColor=ForeColor;
        _sources.BorderStyle=BorderStyle.FixedSingle;_sources.ItemHeight=24;
        _all.CheckedChanged+=(_,_)=>RefreshSources();
        _target.SelectedIndexChanged+=(_,_)=>RefreshSources();
        _sources.SelectedIndexChanged+=(_,_)=>
        {
            var entry=SelectedEntry;
            var profile=entry?.Profile;
            restore.Enabled=profile is not null;
            _details.Text=profile is null?"":$"원본 장치: {entry!.DeviceName}  ·  {(entry.FromSnapshot?"복구 백업":"이전 장치에 보존된 설정 (백업 아님)")}\n"+
                $"31밴드 EQ {profile.Custom?.Count ?? 0}개 · AutoEQ {profile.AutoFilters?.Count ?? 0}개 필터 · 컴프레서 및 공간 음향효과 포함";
        };
        body.Controls.Add(_sources);body.Controls.Add(_details);body.Controls.Add(toggle);
        body.Controls.Add(listHeader);body.Controls.Add(top);
        Controls.Add(body);Controls.Add(footer);Controls.Add(header);
        RefreshSources();
    }
    private void RefreshSources()
    {
        string? id=TargetId;
        string? name=(_target.SelectedItem as DeviceOption)?.Device.Name;
        _sources.BeginUpdate();
        try
        {
            _sources.Items.Clear();
            // Show historical backups and prior devices first; never offer the
            // active endpoint's own live profile as a recovery source.
            var candidates=RecoveryBackupStore.FindRecoveryCandidates(_entries,id,name,_all.Checked);
            foreach(var entry in candidates)
                _sources.Items.Add(new SourceOption(entry));
            if(_sources.Items.Count>0)_sources.SelectedIndex=0;
        }
        finally{_sources.EndUpdate();}
        if(_sources.Items.Count==0)
        {
            if(_restoreButton is not null)_restoreButton.Enabled=false;
            _details.Text="복구 가능한 이전 설정이 없습니다. 다른 장치의 설정을 보려면 체크박스를 선택하세요.";
        }

    }
}
