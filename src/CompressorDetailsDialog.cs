// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
// End-user editing UI: every editable compressor parameter has its own slider.
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace InfinityAudioEqualizerPro;

public sealed class CompressorDetailsDialog : Form
{
    private sealed record Parameter(string Label, decimal Min, decimal Max, decimal Step, int Decimals);
    private static readonly Parameter[] Parameters =
    [
        new("대역 끝 주파수 (Hz)",30,20000,10,0),
        new("Threshold (dB)",-48,0,.1m,1),
        new("Ratio",1,6,.01m,2),
        new("Attack (ms)",2,100,1,0),
        new("Release (ms)",40,500,1,0),
        new("Knee (dB)",0,12,.1m,1),
        new("Gain (dB)",-6,3,.01m,2)
    ];
    private readonly NumericUpDown[,] _numbers = new NumericUpDown[5,7];
    private readonly SlimSlider[,] _sliders = new SlimSlider[5,7];
    private readonly Label[] _cutoffLabels = new Label[5];
    private readonly ToolTip _cutoffHints = new();
    private readonly string _theme;
    private readonly int _strength;
    private readonly Action<CompressorThemes.Band[]>? _livePreview;
    private readonly System.Windows.Forms.Timer _previewTimer = new() { Interval = 160 };
    private bool _populating;
    private bool _ready;
    private Label? _previewMessage;
    private readonly Color _background = Color.FromArgb(21,26,36);
    private readonly Color _panel = Color.FromArgb(31,38,52);
    private readonly Color _foreground = Color.FromArgb(229,239,251);
    public CompressorThemes.Band[]? ResultBands { get; private set; }

    public CompressorDetailsDialog(DeviceProfile profile, Action<CompressorThemes.Band[]>? livePreview = null)
    {
        _theme=profile.CompressorTheme;
        _strength=profile.CompressorStrength;
        _livePreview=livePreview;
        _previewTimer.Tick+=(_,_)=>
        {
            _previewTimer.Stop();
            if(!_ready || _populating || _livePreview is null)return;
            try
            {
                _livePreview(ReadBands());
                if(_previewMessage is not null)
                {
                    _previewMessage.ForeColor=_foreground;
                    _previewMessage.Text="변경한 설정이 적용되고 있습니다. [적용]으로 유지하거나 [취소]로 복원하세요.";
                }
            }
            catch(Exception ex)
            {
                if(_previewMessage is not null)
                {
                    _previewMessage.ForeColor=Color.FromArgb(255,169,156);
                    _previewMessage.Text="실시간 적용 실패: "+ex.Message;
                }
            }
        };
        FormClosing+=(_,_)=>_previewTimer.Stop();
        FormClosed+=(_,_)=>{ _previewTimer.Stop(); _previewTimer.Dispose(); _cutoffHints.Dispose(); };
        Text="5밴드 컴프레서 · 상세 조절";
        StartPosition=FormStartPosition.CenterParent;
        Size=new Size(1000,615);
        MinimumSize=new Size(900,560);
        BackColor=_background;
        ForeColor=_foreground;
        Font=new Font("Segoe UI",10);
        try { Icon=new Icon(Path.Combine(AppContext.BaseDirectory,"assets","Infinity_Audio_Equalizer.ico")); } catch { }

        var intro=new Label
        {
            Dock=DockStyle.Top,Height=55,Padding=new Padding(16,13,10,5),
            Text="슬라이더나 숫자를 바꾸면 바로 반영됩니다. [적용]은 변경값을 유지하고, [취소]는 이전 설정으로 복원합니다.",
            BackColor=_panel,ForeColor=_foreground,TextAlign=ContentAlignment.MiddleLeft
        };
        _previewMessage=intro;
        // Native TabControl paints a light frame even with owner-drawn tabs.
        // Use dark, keyboard-accessible tab buttons and an ordinary content panel.
        var tabHeader=new Panel { Dock=DockStyle.Top,Height=40,BackColor=_panel };
        var tabHost=new Panel { Dock=DockStyle.Fill,BackColor=_background };
        var tabButtons=new BandTabButton[5];
        var tabPages=new Panel[5];
        void SelectBand(int selected)
        {
            for(int i=0;i<5;i++)
            {
                tabButtons[i].Selected=i==selected;
                tabPages[i].Visible=i==selected;
            }
            tabPages[selected].BringToFront();
            tabButtons[selected].Focus();
        }
        string[] names=["Sub","Bass","Mid","High","Air"];
        for (int band=0;band<5;band++)
        {
            int bandIndex=band;
            var page=new Panel { Dock=DockStyle.Fill,BackColor=_background,Visible=band==0 };
            tabPages[band]=page;
            tabHost.Controls.Add(page);
            var tabButton=new BandTabButton
            {
                Text=names[band],Location=new Point(5+band*98,3),Size=new Size(94,33),
                Font=new Font("Segoe UI",9.4f,FontStyle.Bold),Selected=band==0
            };
            tabButtons[band]=tabButton;
            tabButton.Click+=(_,_)=>SelectBand(bandIndex);
            tabHeader.Controls.Add(tabButton);
            // Responsive three-column layout keeps every numeric value visible at all sizes.
            var rows=new TableLayoutPanel
            {
                Dock=DockStyle.Fill,ColumnCount=3,RowCount=Parameters.Length,
                Padding=new Padding(14,12,16,12),BackColor=_background,AutoScroll=true
            };
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,206));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));
            for(int row=0;row<Parameters.Length;row++)
                rows.RowStyles.Add(new RowStyle(SizeType.Absolute,55));
            page.Controls.Add(rows);
            for(int param=0;param<Parameters.Length;param++)
            {
                int paramIndex=param;
                var spec=Parameters[param];
                var label=new Label
                {
                    Text=spec.Label,Dock=DockStyle.Fill,Margin=new Padding(5,3,4,3),
                    ForeColor=_foreground,TextAlign=ContentAlignment.MiddleLeft
                };
                if(param==0)
                {
                    _cutoffLabels[bandIndex]=label;
                    if(bandIndex==4) label.Text="최고 대역 (20kHz 고정)";
                }
                var slider=new SlimSlider
                {
                    Dock=DockStyle.Fill,Margin=new Padding(6,7,8,7),
                    Orientation=Orientation.Horizontal,Minimum=0,Maximum=1000,Value=0
                };
                var number=new NumericUpDown
                {
                    Dock=DockStyle.Fill,Margin=new Padding(6,10,6,10),
                    Minimum=paramIndex==0?CompressorFrequencyRanges.BandRange(bandIndex).Min:spec.Min,
                    Maximum=paramIndex==0?CompressorFrequencyRanges.BandRange(bandIndex).Max:spec.Max,
                    Increment=paramIndex==0?1m:spec.Step,DecimalPlaces=spec.Decimals,
                    BackColor=Color.FromArgb(39,52,71),ForeColor=Color.White,
                    BorderStyle=BorderStyle.FixedSingle,TextAlign=HorizontalAlignment.Center,
                    ThousandsSeparator=true
                };
                bool updating=false;
                slider.ValueChanged+=(_,_)=>
                {
                    if (updating || _populating) return;
                    updating=true;
                    try
                    {
                        decimal value=paramIndex==0
                            ? CutoffFromSlider(bandIndex,slider.Value)
                            : ValueFromSlider(spec,slider.Value);
                        number.Value=Math.Clamp(value,number.Minimum,number.Maximum);
                    }
                    finally { updating=false; }
                    if(paramIndex==0) UpdateCutoffRanges();
                    QueuePreview();
                };
                number.ValueChanged+=(_,_)=>
                {
                    if (updating || _populating) return;
                    updating=true;
                    try
                    {
                        slider.Value=paramIndex==0
                            ? CutoffToSlider(bandIndex,number.Value)
                            : SliderFromValue(spec,number.Value);
                    }
                    finally { updating=false; }
                    if(paramIndex==0) UpdateCutoffRanges();
                    QueuePreview();
                };
                _numbers[bandIndex,paramIndex]=number;
                _sliders[bandIndex,paramIndex]=slider;
                if(paramIndex==0 && bandIndex==4)
                {
                    number.Enabled=false;
                    slider.Enabled=false;
                }
                rows.Controls.Add(label,0,param);
                rows.Controls.Add(slider,1,param);
                rows.Controls.Add(number,2,param);
            }
        }
        var actionBar=new FlowLayoutPanel
        {
            Dock=DockStyle.Bottom,Height=62,BackColor=_panel,Padding=new Padding(10,11,5,5),
            WrapContents=false
        };
        AccentButton Button(string label,Action action,int width)
        {
            var b=new AccentButton
            {
                Text=label,Size=new Size(width,35),Margin=new Padding(4,0,4,0),
                Font=new Font("Segoe UI",9),BackColor=Color.FromArgb(44,71,112)
            };
            b.Click+=(_,_)=>
            {
                try { action(); }
                catch (Exception ex) { MessageBox.Show(this,ex.Message,"컴프레서 설정",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
            };
            return b;
        }
        actionBar.Controls.Add(Button("상세값 초기화",()=>
            Populate(CompressorThemes.MakeBands(_theme=="CUSTOM"?"PREMIUM_BALANCE":_theme,_strength)),135));
        actionBar.Controls.Add(Button("설정 파일 불러오기",Import,156));
        actionBar.Controls.Add(Button("설정 파일 저장",Export,140));
        actionBar.Controls.Add(Button("적용",Apply,100));
        actionBar.Controls.Add(Button("취소",Close,100));
        Controls.Add(tabHost);
        Controls.Add(tabHeader);
        Controls.Add(actionBar);
        Controls.Add(intro);

        var current=profile.CompressorTheme=="CUSTOM" && profile.CompressorCustomBands.Count==5
            ? profile.CompressorCustomBands.Select(b=>b.ToRuntime()).ToArray()
            : CompressorThemes.MakeBands(profile.CompressorTheme,profile.CompressorStrength);
        Populate(current);
        _ready=true;
    }

    private void QueuePreview()
    {
        if(!_ready || _populating || _livePreview is null)return;
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    public static int SliderFromValue(int parameterIndex,decimal value)=>
        SliderFromValue(Parameters[parameterIndex],value);
    private static int SliderFromValue(Parameter spec,decimal value)
    {
        if(spec.Min>=spec.Max) return 0;
        double f=spec.Min==30 && spec.Max==20000
            ? Math.Log((double)value/(double)spec.Min)/Math.Log((double)spec.Max/(double)spec.Min)
            : (double)((value-spec.Min)/(spec.Max-spec.Min));
        return Math.Clamp((int)Math.Round(Math.Clamp(f,0,1)*1000),0,1000);
    }
    private static decimal ValueFromSlider(Parameter spec,int value)
    {
        double f=Math.Clamp(value/1000d,0,1);
        decimal raw=spec.Min==30 && spec.Max==20000
            ? (decimal)((double)spec.Min*Math.Pow((double)spec.Max/(double)spec.Min,f))
            : spec.Min+(spec.Max-spec.Min)*(decimal)f;
        return Math.Round(raw,spec.Decimals,MidpointRounding.AwayFromZero);
    }
    private static int FrequencyToSlider(decimal min,decimal max,decimal value)
    {
        if(min>=max) return 0;
        double fraction=Math.Log((double)(value/min))/Math.Log((double)(max/min));
        return Math.Clamp((int)Math.Round(Math.Clamp(fraction,0,1)*1000),0,1000);
    }

    private static decimal FrequencyFromSlider(decimal min,decimal max,int slider)
    {
        if(min>=max) return min;
        double fraction=Math.Clamp(slider/1000d,0,1);
        return Math.Clamp(Math.Round((decimal)((double)min*Math.Pow((double)(max/min),fraction)),0),min,max);
    }

    private decimal[] CurrentCutoffs()=>Enumerable.Range(0,5).Select(i=>_numbers[i,0].Value).ToArray();

    private int CutoffToSlider(int band,decimal value)
    {
        var (min,max)=CompressorFrequencyRanges.Allowed(CurrentCutoffs(),band);
        return FrequencyToSlider(min,max,value);
    }

    private decimal CutoffFromSlider(int band,int slider)
    {
        var (min,max)=CompressorFrequencyRanges.Allowed(CurrentCutoffs(),band);
        return FrequencyFromSlider(min,max,slider);
    }

    private void UpdateCutoffRanges()
    {
        if(_populating)return;
        _populating=true;
        try
        {
            var cutoffs=CurrentCutoffs();
            for(int i=0;i<5;i++)
            {
                var (min,max)=CompressorFrequencyRanges.Allowed(cutoffs,i);
                var number=_numbers[i,0];
                // Existing values are always within the new bounds after normalizing
                // the imported preset, and subsequent edits are limited by neighbors.
                number.Minimum=Math.Min(number.Minimum,min);
                number.Maximum=Math.Max(number.Maximum,max);
                number.Minimum=min;
                number.Maximum=max;
                _sliders[i,0].Value=FrequencyToSlider(min,max,number.Value);
                string hint=i==4 ? "20,000Hz로 고정된 최고 대역입니다."
                    : $"선택 가능한 주파수: {min:N0} ~ {max:N0}Hz (이웃 대역과 겹치지 않도록 자동 제한)";
                _cutoffHints.SetToolTip(_cutoffLabels[i],hint);
                _cutoffHints.SetToolTip(number,hint);
                _cutoffHints.SetToolTip(_sliders[i,0],hint);
                if(i<4)
                {
                    _cutoffLabels[i].Text="대역 끝 주파수 (Hz)";
                    number.Enabled=min<max;
                    _sliders[i,0].Enabled=min<max;
                }
            }
            _numbers[4,0].Enabled=false;
            _sliders[4,0].Enabled=false;
        }
        finally { _populating=false; }
    }

    private void Populate(IReadOnlyList<CompressorThemes.Band> bands)
    {
        if(bands.Count!=5)throw new InvalidDataException("5밴드 설정이 필요합니다.");
        _populating=true;
        try
        {
            decimal[] normalized=CompressorFrequencyRanges.Normalize(bands.Select(b=>b.Cutoff).ToArray());
            // Clear old neighbor-derived limits before importing another preset.
            for(int i=0;i<5;i++)
            {
                var (min,max)=CompressorFrequencyRanges.BandRange(i);
                _numbers[i,0].Minimum=min;
                _numbers[i,0].Maximum=max;
            }
            for(int band=0;band<5;band++)
            {
                var b=bands[band];
                double[] values=[b.Cutoff,b.Threshold,b.Ratio,b.Attack,b.Release,b.Knee,b.Gain];
                for(int param=0;param<7;param++)
                {
                    var spec=Parameters[param];
                    var value=param==0?normalized[band]:(decimal)values[param];
                    _numbers[band,param].Value=Math.Clamp(value,_numbers[band,param].Minimum,_numbers[band,param].Maximum);
                    _sliders[band,param].Value=param==0?0:SliderFromValue(spec,_numbers[band,param].Value);
                }
            }
        }
        finally { _populating=false; }
        UpdateCutoffRanges();
        QueuePreview();
    }
    private CompressorThemes.Band[] ReadBands()
    {
        var result=new CompressorThemes.Band[5];
        for(int i=0;i<5;i++)
        {
            double N(int j)=>(double)_numbers[i,j].Value;
            result[i]=new CompressorThemes.Band(N(0),N(1),N(2),N(3),N(4),N(5),N(6));
        }
        return result;
    }
    private void Apply()
    {
        ResultBands=ReadBands();
        _previewTimer.Stop();
        _livePreview?.Invoke(ResultBands);
        DialogResult=DialogResult.OK;
        Close();
    }
    private void Import()
    {
        using var dialog=new OpenFileDialog { Filter="컴프레서 설정 (*.json)|*.json",Title="설정 파일 불러오기" };
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        if(new FileInfo(dialog.FileName).Length>256*1024)
            throw new InvalidDataException("설정 파일이 너무 큽니다 (최대 256KB).");
        using var doc=JsonDocument.Parse(File.ReadAllText(dialog.FileName,Encoding.UTF8));
        JsonElement bands=doc.RootElement;
        if(bands.ValueKind==JsonValueKind.Object)
        {
            if(!bands.TryGetProperty("bands",out var bandArray))
                throw new InvalidDataException("컴프레서 설정 형식이 올바르지 않습니다.");
            bands=bandArray;
        }
        var values=JsonSerializer.Deserialize<List<CompressorBand>>(bands.GetRawText(),SettingsStore.JsonOptions);
        if(values?.Count!=5) throw new InvalidDataException("5밴드 설정 형식이 올바르지 않습니다.");
        Populate(values.Select(b=>b.ToRuntime()).ToArray());
    }
    private void Export()
    {
        var bands=ReadBands();
        using var dialog=new SaveFileDialog
        {
            FileName="Infinity_Compressor_Preset.json",Filter="컴프레서 설정 (*.json)|*.json",Title="설정 파일 저장"
        };
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        var payload=new
        {
            format="infinity-audio-compressor",version=1,
            bands=bands.Select(CompressorBand.From).ToList()
        };
        File.WriteAllText(dialog.FileName,
            JsonSerializer.Serialize(payload,SettingsStore.JsonOptions)+Environment.NewLine,new UTF8Encoding(false));
    }
}

// Avoid native tab strips: Windows themes can paint bright borders around
// dark, owner-drawn TabControl pages. These tabs use the app palette entirely.
internal sealed class BandTabButton : Button
{
    private bool _selected;
    [System.ComponentModel.DefaultValue(false)]
    public bool Selected { get=>_selected; set { _selected=value;Invalidate(); } }
    public BandTabButton()
    {
        FlatStyle=FlatStyle.Flat;
        FlatAppearance.BorderSize=0;
        BackColor=Color.FromArgb(31,38,52);
        ForeColor=Color.FromArgb(229,239,251);
        UseVisualStyleBackColor=false;
        SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|
                 ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        using var brush=new SolidBrush(Selected?Color.FromArgb(44,71,112):Color.FromArgb(27,34,47));
        e.Graphics.FillRectangle(brush,ClientRectangle);
        using var pen=new Pen(Color.FromArgb(73,91,119));
        e.Graphics.DrawRectangle(pen,0,0,Math.Max(0,Width-1),Math.Max(0,Height-1));
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Color.White,
            TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
        if(Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(3,3,Width-6,Height-6));
    }
}
