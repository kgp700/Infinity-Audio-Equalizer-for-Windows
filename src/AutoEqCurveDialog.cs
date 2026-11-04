// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Drawing;
using System.Drawing.Drawing2D;

namespace InfinityAudioEqualizerPro;

public sealed class AutoEqCurveDialog : Form
{
    private static readonly Color Bg=Color.FromArgb(21,26,36);
    private static readonly Color[] CurveColors=[Color.FromArgb(89,187,252),Color.FromArgb(247,187,98),Color.FromArgb(144,223,154)];
    private readonly bool[] _visible=[true,true,true];
    private readonly DeviceProfile _profile;
    private readonly Panel _plot=new() { Dock=DockStyle.Fill,BackColor=Bg };
    private readonly double[][] _series=[new double[1024],new double[1024],new double[1024]];

    public AutoEqCurveDialog(DeviceProfile profile)
    {
        _profile=profile;
        Text="AutoEQ 곡선 보기 · 31밴드 주파수 응답";
        ClientSize=new Size(1030,560);MinimumSize=new Size(760,420);
        StartPosition=FormStartPosition.CenterParent;
        BackColor=Bg;ForeColor=Color.White;Font=new Font("Segoe UI",9);
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(12,10,12,12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,35));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,69));
        Controls.Add(layout);
        var title=new Label { Text="AutoEQ: "+(string.IsNullOrWhiteSpace(profile.Source)?"AutoEQ 파일 없음":profile.Source)+"  |  사용자 EQ 31밴드",
            Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.White,AutoEllipsis=true };
        layout.Controls.Add(title,0,0);layout.Controls.Add(_plot,0,1);
        var legend=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false };
        string[] labels=["AutoEQ","사용자 EQ","AutoEQ + 사용자 EQ"];
        for(int i=0;i<3;i++)
        {
            int series=i;
            var toggle=new CheckBox { Text=labels[i],Checked=true,AutoSize=true,Margin=new Padding(4,8,23,3),ForeColor=CurveColors[i] };
            toggle.CheckedChanged+=(_,_)=>{_visible[series]=toggle.Checked;_plot.Invalidate();};
            legend.Controls.Add(toggle);
        }
        layout.Controls.Add(legend,0,2);
        var footer=new Panel { Dock=DockStyle.Fill };
        footer.Controls.Add(new Label {
            Text="합산 응답은 초록색 점선입니다. 곡선은 계산상 주파수 응답이며 프리앰프·공간 음향·컴프레서는 제외됩니다.\nON/OFF와 관계없이 저장된 보정값을 비교합니다.",
            Dock=DockStyle.Fill,Padding=new Padding(2,4,110,0),ForeColor=Color.FromArgb(209,223,238) });
        var close=new AccentButton { Text="닫기",Size=new Size(93,30),Anchor=AnchorStyles.Right|AnchorStyles.Bottom };
        footer.Controls.Add(close);
        footer.Resize+=(_,_)=>close.Location=new Point(Math.Max(0,footer.Width-102),footer.Height-35);
        close.Click+=(_,_)=>Close();
        layout.Controls.Add(footer,0,3);
        for(int i=0;i<1024;i++)
        {
            double hz=20*Math.Pow(1000,i/1023d);
            var (automatic,custom,combined)=PreampCalculator.PreviewResponseAt(profile,hz);
            _series[0][i]=automatic;_series[1][i]=custom;_series[2][i]=combined;
        }
        _plot.Paint+=DrawCurve;
        _plot.Resize+=(_,_)=>_plot.Invalidate();
    }
    private void DrawCurve(object? sender,PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        var rect=new RectangleF(66,16,Math.Max(30,_plot.Width-91),Math.Max(40,_plot.Height-68));
        double peak=6;
        for(int j=0;j<3;j++) if(_visible[j])
            foreach(double v in _series[j])if(double.IsFinite(v))peak=Math.Max(peak,Math.Abs(v));
        double limit=Math.Max(6,Math.Ceiling((peak+1.5)/3)*3);
        float X(double hz)=>rect.Left+(float)(Math.Log(Math.Clamp(hz,20,20000)/20)/Math.Log(1000)*rect.Width);
        float Y(double db)=>rect.Top+rect.Height/2-(float)(db/limit*rect.Height/2);
        using var grid=new Pen(Color.FromArgb(63,91,112));
        using var fg=new SolidBrush(Color.FromArgb(235,243,251));
        using var labelFont=new Font("Segoe UI",7.2f);
        foreach(var db in new[]{-limit,-limit/2,0,limit/2,limit})
        {
            var y=Y(db);g.DrawLine(grid,rect.Left,y,rect.Right,y);
            g.DrawString($"{db:+0.#;-0.#;0} dB",Font,fg,0,y-8);
        }
        foreach(double hz in ApoRenderer.Bands)
        {
            float x=X(hz);g.DrawLine(grid,x,rect.Top,x,rect.Bottom);
            var txt=hz>=1000?(hz/1000).ToString("0.##")+"k":hz.ToString("0.#");
            var size=g.MeasureString(txt,labelFont);
            g.DrawString(txt,labelFont,fg,x-size.Width/2,rect.Bottom+5);
        }
        var clip=g.Clip;g.SetClip(rect);
        foreach(int j in new[]{2,0,1})
        {
            if(!_visible[j])continue;
            var points=new PointF[1024];
            for(int i=0;i<points.Length;i++)
                points[i]=new PointF(X(20*Math.Pow(1000,i/1023d)),Y(double.IsFinite(_series[j][i])?_series[j][i]:0));
            using var pen=new Pen(CurveColors[j],j==2?2.6f:2f);
            if(j==2){pen.DashStyle=DashStyle.Custom;pen.DashPattern=[7f,5f];}
            g.DrawLines(pen,points);
        }
        g.Clip=clip;
        using var border=new Pen(Color.FromArgb(100,111,129));g.DrawRectangle(border,rect.X,rect.Y,rect.Width,rect.Height);
    }
}
