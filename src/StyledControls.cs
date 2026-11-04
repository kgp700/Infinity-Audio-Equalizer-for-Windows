// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace InfinityAudioEqualizerPro;

// Replaces the native WinForms TrackBar triangle with the original Python UI's
// thin EQ track and rounded handle. All state remains in the owning model.
public sealed class SlimSlider : Control
{
    private int _min=-120, _max=120, _value;
    private bool _dragging;
    private Orientation _orientation=System.Windows.Forms.Orientation.Vertical;
    public event EventHandler? ValueChanged;
    [DefaultValue(-120)]
    public int Minimum { get => _min; set { _min=value; if(_max<_min)_max=_min; Value=_value;Invalidate(); } }
    [DefaultValue(120)]
    public int Maximum { get => _max; set { _max=Math.Max(value,_min); Value=_value;Invalidate(); } }
    [DefaultValue(0)]
    public int Value
    {
        get=>_value;
        set
        {
            int v=Math.Clamp(value,_min,_max);
            if(v==_value)return;
            _value=v;Invalidate();ValueChanged?.Invoke(this,EventArgs.Empty);
        }
    }
    [DefaultValue(System.Windows.Forms.Orientation.Vertical)]
    public Orientation Orientation {get=>_orientation;set{_orientation=value;Invalidate();}}
    private static readonly Color Track=Color.FromArgb(62,84,115);
    private static readonly Color Accent=Color.FromArgb(101,165,254);
    public SlimSlider()
    {
        DoubleBuffered=true;TabStop=true;BackColor=Color.FromArgb(21,26,36);
        SetStyle(ControlStyles.ResizeRedraw|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);
    }
    private Rectangle TrackRectangle()=>Orientation==System.Windows.Forms.Orientation.Vertical
        ? new Rectangle(Width/2-2,9,4,Math.Max(4,Height-18))
        : new Rectangle(10,Height/2-2,Math.Max(4,Width-20),4);
    private Point HandlePoint()
    {
        var track=TrackRectangle();
        double fraction=(_value-_min)/(double)Math.Max(1,_max-_min);
        return Orientation==System.Windows.Forms.Orientation.Vertical
            ? new Point(track.Left+track.Width/2,track.Bottom-(int)Math.Round(fraction*track.Height))
            : new Point(track.Left+(int)Math.Round(fraction*track.Width),track.Top+track.Height/2);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        Rectangle r=TrackRectangle();
        using(var brush=new SolidBrush(Track))e.Graphics.FillRectangle(brush,r);
        Point p=HandlePoint();
        if(Orientation==System.Windows.Forms.Orientation.Vertical)
        {
            using var brush=new SolidBrush(Color.FromArgb(101,165,254));
            e.Graphics.FillRectangle(brush, new Rectangle(r.Left,p.Y,4,r.Bottom-p.Y));
        }
        else
        {
            using var brush=new SolidBrush(Accent);
            e.Graphics.FillRectangle(brush,new Rectangle(r.Left,r.Top,Math.Max(0,p.X-r.Left),4));
        }
        // PySide6-style horizontal rounded rectangular thumb, not a circular dot.
        var thumb=new Rectangle(p.X-8,p.Y-5,16,10);
        using(var path=new GraphicsPath())
        {
            int d=5;
            path.AddArc(thumb.Left,thumb.Top,d,d,180,90);
            path.AddArc(thumb.Right-d,thumb.Top,d,d,270,90);
            path.AddArc(thumb.Right-d,thumb.Bottom-d,d,d,0,90);
            path.AddArc(thumb.Left,thumb.Bottom-d,d,d,90,90);
            path.CloseFigure();
            using var brush=new SolidBrush(Enabled?Accent:Color.FromArgb(95,111,134));
            e.Graphics.FillPath(brush,path);
        }
        if(Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(1,1,Width-2,Height-2));
    }
    private void Pick(Point pos)
    {
        var track=TrackRectangle();
        double t=Orientation==System.Windows.Forms.Orientation.Vertical
            ? (track.Bottom-pos.Y)/(double)Math.Max(1,track.Height)
            : (pos.X-track.Left)/(double)Math.Max(1,track.Width);
        Value=_min+(int)Math.Round(Math.Clamp(t,0,1)*(_max-_min));
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left || !Enabled)return;
        Focus();_dragging=true;Capture=true;Pick(e.Location);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(_dragging)Pick(e.Location);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);if(!_dragging)return;_dragging=false;Capture=false;
    }
    // Deliberately ignore wheel events: accidental EQ adjustments while scrolling
    // the main view must never change a frequency band.
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        // No base call: the slider consumes no wheel-to-value mapping.
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if(e.KeyCode is Keys.Up or Keys.Right){Value++;e.Handled=true;}
        else if(e.KeyCode is Keys.Down or Keys.Left){Value--;e.Handled=true;}
        else if(e.KeyCode==Keys.Home){Value=_min;e.Handled=true;}
        else if(e.KeyCode==Keys.End){Value=_max;e.Handled=true;}
    }
}

// Consistent WinForms group border close to Python QGroupBox: subtle gray stroke.
public sealed class SubtleGroupBox : GroupBox
{
    public SubtleGroupBox()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|
                 ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        using var edge=new Pen(Color.FromArgb(69,79,95),1f);
        using var titleBrush=new SolidBrush(ForeColor);
        string title=Text.Trim();
        var font=Font;
        int titleWidth=TextRenderer.MeasureText(e.Graphics,title,font,Size.Empty,
            TextFormatFlags.SingleLine|TextFormatFlags.NoPadding).Width;
        int lineY=Math.Max(8,font.Height/2);
        int bottom=Math.Max(lineY+2,Height-1);
        // Leave a gap for the section heading just like a PySide6 group box.
        e.Graphics.DrawLine(edge,0,lineY,12,lineY);
        e.Graphics.DrawLine(edge,Math.Min(Width-1,18+titleWidth),lineY,Width-1,lineY);
        e.Graphics.DrawLine(edge,0,lineY,0,bottom);
        e.Graphics.DrawLine(edge,Width-1,lineY,Width-1,bottom);
        e.Graphics.DrawLine(edge,0,bottom,Width-1,bottom);
        TextRenderer.DrawText(e.Graphics,title,font,new Point(15,0),ForeColor,
            TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
    }
}

public enum AccentButtonTone { Standard, Success, Danger }

public sealed class AccentButton : Button
{
    private bool _hover;
    private AccentButtonTone _tone;
    [DefaultValue(AccentButtonTone.Standard)]
    public AccentButtonTone Tone
    {
        get => _tone;
        set { if (_tone != value) { _tone = value; Invalidate(); } }
    }
    public AccentButton()
    {
        FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;
        UseVisualStyleBackColor=false;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|
                 ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
    }
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);_hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);_hover=false;Invalidate();}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        // Explicitly clear every pixel before drawing. Invalidation / resize had
        // previously left odd broken edges and visual artifacts on buttons.
        e.Graphics.Clear(Parent?.BackColor ?? Color.FromArgb(21,26,36));
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        if(Width<=3 || Height<=3)return;
        int radius=Math.Min(6,Math.Min(Width-2,Height-2)/2);
        using var path=new GraphicsPath();
        var r=new Rectangle(1,1,Width-3,Height-3);
        int d=radius*2;
        path.AddArc(r.Left,r.Top,d,d,180,90);
        path.AddArc(r.Right-d,r.Top,d,d,270,90);
        path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);
        path.AddArc(r.Left,r.Bottom-d,d,d,90,90);
        path.CloseFigure();
        Color bg = !Enabled ? Color.FromArgb(43,49,60) : _tone switch
        {
            AccentButtonTone.Success => _hover ? Color.FromArgb(33,132,112) : Color.FromArgb(28,115,96),
            AccentButtonTone.Danger  => _hover ? Color.FromArgb(158,66,82) : Color.FromArgb(132,54,70),
            _ => _hover ? Color.FromArgb(56,88,138) : Color.FromArgb(44,71,112)
        };
        using(var brush=new SolidBrush(bg)) e.Graphics.FillPath(brush,path);
        using(var edge=new Pen(ControlPaint.Dark(bg,.15f),1f))e.Graphics.DrawPath(edge,path);
        var flags=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|
                  TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding;
        TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(5,2,Width-10,Height-4),
            Enabled?Color.FromArgb(232,242,253):Color.FromArgb(143,154,172),flags);
        if(Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(3,3,Width-6,Height-6));
    }
}
