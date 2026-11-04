// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Diagnostics;
using System.Drawing;

namespace InfinityAudioEqualizerPro;

public sealed class AutoEqSearchDialog : Form
{
    private static readonly Color Bg = Color.FromArgb(21,26,36);
    private static readonly Color Field = Color.FromArgb(32,42,58);
    private static readonly Color Fg = Color.FromArgb(238,245,255);
    private readonly TextBox _search = new() { Dock=DockStyle.Fill, PlaceholderText="모델명 입력 (예: AirPods 4, Sony, HD 600)", BorderStyle=BorderStyle.FixedSingle, BackColor=Field, ForeColor=Fg };
    private readonly ListBox _results = new() { Dock=DockStyle.Fill, IntegralHeight=false, DrawMode=DrawMode.OwnerDrawFixed, ItemHeight=29, BorderStyle=BorderStyle.FixedSingle, BackColor=Field, ForeColor=Fg, HorizontalScrollbar=true };
    private readonly Label _info = new() { Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft, ForeColor=Fg, Text="공식 AutoEq 모델 목록을 불러오는 중입니다." };
    private readonly Button _apply = new AccentButton { Text="선택 모델 적용", Width=132, Enabled=false };
    private readonly Button _reload = new AccentButton { Text="목록 새로고침", Width=110 };
    private readonly CancellationTokenSource _lifetime = new();
    private IReadOnlyList<AutoEqCatalogService.Model> _models = [];
    private bool _busy;
    public AutoEqCatalogService.ProfileDownload? Download { get; private set; }
    public AutoEqCatalogService.Model? Selection { get; private set; }

    public AutoEqSearchDialog(string targetId, string suggestedSearch)
    {
        // The selection is applied by the caller to the device identified at dialog opening.
        // Never pre-fill searches with an audio endpoint's name: the device name often
        // differs from the measurement catalog's model name.
        _ = targetId;
        _ = suggestedSearch;
        Text="AutoEQ 모델 검색 · 공식 AutoEq 데이터";
        StartPosition=FormStartPosition.CenterParent;
        MinimumSize=new Size(620,480);
        ClientSize=new Size(760,570);
        BackColor=Bg; ForeColor=Fg; Font=new Font("Segoe UI",9);
        var layout = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=4, Padding=new Padding(14,12,14,12), BackColor=Bg };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
        Controls.Add(layout);
        layout.Controls.Add(_search,0,0);
        layout.Controls.Add(_results,0,1);
        layout.Controls.Add(_info,0,2);
        var buttons = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false, FlowDirection=FlowDirection.RightToLeft, BackColor=Bg };
        Button cancel = new AccentButton { Text="취소", Width=82 };
        Button web = new AccentButton { Text="AutoEQ 웹사이트", Width=135 };
        foreach (var b in new[] { cancel, _apply, _reload, web }) { b.Height=31; b.Margin=new Padding(5,5,0,3); }
        cancel.Click+=(_,_)=>Close();
        _apply.Click+=async (_,_)=>await ApplySelectedAsync();
        _reload.Click+=async (_,_)=>await LoadCatalogAsync();
        web.Click+=(_,_)=>Process.Start(new ProcessStartInfo("https://autoeq.app/") { UseShellExecute=true });
        buttons.Controls.Add(cancel);buttons.Controls.Add(_apply);buttons.Controls.Add(_reload);buttons.Controls.Add(web);
        layout.Controls.Add(buttons,0,3);
        _results.DrawItem+=(_,e)=>
        {
            if(e.Index<0 || e.Index>=_results.Items.Count)return;
            bool selected=(e.State & DrawItemState.Selected)!=0;
            using var background = new SolidBrush(selected ? Color.FromArgb(49,93,151) : Field);
            e.Graphics.FillRectangle(background,e.Bounds);
            using var brush=new SolidBrush(Fg);
            string text = _results.Items[e.Index]?.ToString() ?? "";
            TextRenderer.DrawText(e.Graphics,text,_results.Font,new Rectangle(e.Bounds.X+8,e.Bounds.Y,e.Bounds.Width-12,e.Bounds.Height),
                Fg,TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
            if((e.State & DrawItemState.Focus)!=0)e.DrawFocusRectangle();
        };
        _search.TextChanged+=(_,_)=>Filter();
        _results.SelectedIndexChanged+=(_,_)=>_apply.Enabled=!_busy && _results.SelectedItem is AutoEqCatalogService.Model;
        _results.DoubleClick+=async (_,_)=>await ApplySelectedAsync();
        FormClosing+=(_,_)=>_lifetime.Cancel();
        Shown+=async (_,_)=>await LoadCatalogAsync();
        // Search intentionally starts empty.
    }
    private void Filter()
    {
        var tokens=_search.Text.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        var chosen=_models.Where(m=>tokens.All(t=>(m.Name+" "+m.Folder).Contains(t,StringComparison.OrdinalIgnoreCase))).Take(250).ToArray();
        _results.BeginUpdate();
        try { _results.Items.Clear();_results.Items.AddRange(chosen.Cast<object>().ToArray()); }
        finally { _results.EndUpdate(); }
        _info.Text=$"{chosen.Length}개 표시 · 동일 기기라도 측정 출처가 다를 수 있습니다. 모델을 선택하세요.";
    }
    private async Task LoadCatalogAsync()
    {
        if(_busy)return;
        SetBusy(true,"공식 AutoEq 모델 목록을 다운로드하는 중입니다...");
        try { _models=await AutoEqCatalogService.FetchIndexAsync(_lifetime.Token);if(!IsDisposed)Filter(); }
        catch(OperationCanceledException) { }
        catch(Exception ex) { if(!IsDisposed)_info.Text="모델 목록을 불러오지 못했습니다. 다시 시도해 주세요. " + ex.Message; }
        finally { if(!IsDisposed)SetBusy(false); }
    }
    private async Task ApplySelectedAsync()
    {
        if(_busy || _results.SelectedItem is not AutoEqCatalogService.Model selected)return;
        SetBusy(true,$"{selected.Name} 보정값을 다운로드하는 중입니다...");
        try
        {
            var downloaded=await AutoEqCatalogService.FetchProfileAsync(selected,_lifetime.Token);
            var validation=new DeviceProfile();
            AutoEqImporter.Import(downloaded.Text,validation,downloaded.Filename);
            Download=downloaded;Selection=selected;
            if(!IsDisposed){DialogResult=DialogResult.OK;Close();}
        }
        catch(OperationCanceledException){ }
        catch(Exception ex){if(!IsDisposed)MessageBox.Show(this,"AutoEQ 보정값을 가져오지 못했습니다.\n"+ex.Message,"AutoEQ",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        finally{if(!IsDisposed)SetBusy(false);}
    }
    private void SetBusy(bool busy,string? message=null)
    {
        _busy=busy;_apply.Enabled=!busy&&_results.SelectedItem is AutoEqCatalogService.Model;
        _reload.Enabled=!busy;_search.Enabled=!busy;_results.Enabled=!busy;
        if(message is not null)_info.Text=message;
    }
}
