// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Drawing;

namespace InfinityAudioEqualizerPro;

/// <summary>Explicit mapping between backup profiles and new Windows output endpoints.</summary>
public sealed class FullSettingsRestoreDialog : Form
{
    private sealed record TargetOption(string Id,string Caption)
    {
        public override string ToString()=>Caption;
    }
    private readonly Dictionary<string,ComboBox> _choices=new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string,string> Mappings { get; }=new(StringComparer.OrdinalIgnoreCase);

    public FullSettingsRestoreDialog(IReadOnlyDictionary<string,DeviceProfile> archived,IReadOnlyList<AudioDevice> unmatched,string? defaultRenderDeviceId)
    {
        Text="전체 설정 복원 · 출력 장치 연결";
        StartPosition=FormStartPosition.CenterParent;
        FormBorderStyle=FormBorderStyle.Sizable;
        MinimumSize=new Size(760,455);
        Size=new Size(900,Math.Min(770,395+archived.Count*65));
        ShowInTaskbar=false;
        BackColor=Color.FromArgb(21,26,36);
        ForeColor=Color.FromArgb(230,238,250);
        Font=new Font("Segoe UI",9.5f);

        bool defaultAvailable=unmatched.Any(d=>string.Equals(d.Id,defaultRenderDeviceId,StringComparison.OrdinalIgnoreCase));
        var heading=new Label {
            Dock=DockStyle.Top,Height=105,Padding=new Padding(18,13,18,4),
            Text="백업된 음향 설정을 현재 출력 장치에 복원합니다.\n" +
                (defaultAvailable ? "Windows 기본 재생 장치를 우선 선택했습니다." : "복원 가능한 출력 장치를 선택했습니다. 기본 재생 장치가 대상에 없을 수 있습니다.") +
                " 필요하면 다른 장치를 선택하세요.\n연결되지 않은 다른 백업 프로필은 삭제하지 않고 보관합니다.",
            ForeColor=Color.FromArgb(220,235,253)
        };
        // Keep the explanation and action buttons in separate layout rows.
        // The previous 85px footer was smaller than its contents plus padding,
        // causing the buttons to be clipped (especially with Windows DPI scaling).
        var footer=new Panel{Dock=DockStyle.Bottom,Height=120,Padding=new Padding(18,7,18,9),BackColor=Color.FromArgb(31,40,55)};
        var footerLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty,Padding=Padding.Empty};
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        var instruction=new Label {
            Dock=DockStyle.Fill,AutoSize=false,TextAlign=ContentAlignment.MiddleLeft,
            Text="현재 재생 장치를 확인한 뒤 복원하세요. 취소를 누르면 설정을 가져오지 않습니다.\n한 출력 장치를 여러 백업 프로필에 중복 지정할 수 없습니다.",
            ForeColor=Color.FromArgb(182,207,237)
        };
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Margin=Padding.Empty,Padding=new Padding(0,6,0,0)};
        var ok=new Button{Text="선택한 장치로 복원",Size=new Size(185,35),Margin=new Padding(4,2,0,2),BackColor=Color.FromArgb(44,85,143),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
        var cancel=new Button{Text="취소",Size=new Size(100,35),Margin=new Padding(4,2,0,2)};
        actions.Controls.Add(ok);actions.Controls.Add(cancel);
        footerLayout.Controls.Add(instruction,0,0);
        footerLayout.Controls.Add(actions,0,1);
        footer.Controls.Add(footerLayout);
        cancel.Click+=(_,_)=>{DialogResult=DialogResult.Cancel;Close();};
        CancelButton=cancel;
        ok.Click+=(_,_)=>Confirm();

        var scrolling=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(12,5,12,5)};
        var rows=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Padding=new Padding(0),GrowStyle=TableLayoutPanelGrowStyle.AddRows};
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,52));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,48));
        var suggested=FullSettingsRestore.SuggestMappingsWithDefault(archived,unmatched,defaultRenderDeviceId);
        int row=0;
        foreach(var entry in archived)
        {
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute,66));
            string label=entry.Value.Name;
            int bands=entry.Value.Custom?.Count ?? 0;
            var left=new Label{
                Text=$"백업 장치: {label}\n수동 EQ {bands}밴드 · AutoEQ/컴프레서/공간효과 포함 가능",
                Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(5,4,10,4),
                ForeColor=Color.FromArgb(231,241,253)
            };
            var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top,Margin=new Padding(8,16,7,4),Width=330};
            foreach(var device in unmatched)
                combo.Items.Add(new TargetOption(device.Id,device.Name));
            if(suggested.TryGetValue(entry.Key,out var suggestedId))
            {
                for(int i=0;i<combo.Items.Count;i++)
                    if(combo.Items[i] is TargetOption t && t.Id.Equals(suggestedId,StringComparison.OrdinalIgnoreCase))
                        {combo.SelectedIndex=i;break;}
            }
            // Blank selection means no target was assigned to this extra archived profile.
            // There is deliberately no "keep only" choice; Cancel aborts the import.
            // The old profile still remains in the full backup if there are more archived
            // profiles than current outputs.
            _choices.Add(entry.Key,combo);
            rows.Controls.Add(left,0,row);
            rows.Controls.Add(combo,1,row);
            row++;
        }
        scrolling.Controls.Add(rows);
        Controls.Add(scrolling);
        Controls.Add(footer);
        Controls.Add(heading);
    }

    private void Confirm()
    {
        var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var entry in _choices)
        {
            if(entry.Value.SelectedItem is not TargetOption selection || selection.Id.Length==0) continue;
            if(!used.Add(selection.Id))
            {
                MessageBox.Show(this,"하나의 출력 장치에 여러 백업 프로필을 연결할 수 없습니다.","복원 대상 중복",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }
            result.Add(entry.Key,selection.Id);
        }
        if(result.Count==0)
        {
            MessageBox.Show(this,"복원할 출력 장치를 선택하세요. 가져오지 않으려면 취소를 누르세요.","출력 장치 선택",MessageBoxButtons.OK,MessageBoxIcon.Information);
            return;
        }
        Mappings.Clear();
        foreach(var entry in result)Mappings.Add(entry.Key,entry.Value);
        DialogResult=DialogResult.OK;
        Close();
    }
}
