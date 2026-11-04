// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InfinityAudioEqualizerPro;

internal sealed record UserGuideItem(Control Target, string Title, string Description);

/// <summary>
/// Non-layout, read-only guided overlay over the existing main form. Navigation scrolls
/// the existing viewport without changing canvas dimensions or adding scroll bars.
/// A snapshot is used to keep all underlying WinForms controls non-interactive.
/// </summary>
internal sealed class UserGuideOverlay : Control
{
    private readonly Panel _scroller;
    private readonly IReadOnlyList<UserGuideItem> _items;
    private readonly Action _close;
    private Bitmap? _snapshot;
    private int _index;
    private Rectangle _highlight;
    private Rectangle _card;
    private Rectangle _prev;
    private Rectangle _next;
    private Rectangle _exit;
    private readonly Font _titleFont = new("Segoe UI", 12f, FontStyle.Bold);
    private readonly Font _bodyFont = new("Segoe UI", 9.5f);
    private readonly Font _smallFont = new("Segoe UI", 9f, FontStyle.Bold);
    private readonly Font _closeFont = new("Segoe UI", 12f, FontStyle.Bold);

    internal UserGuideOverlay(Panel scroller, IReadOnlyList<UserGuideItem> items, Action close)
    {
        _scroller = scroller;
        _items = items;
        _close = close;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        BackColor = Color.FromArgb(21, 26, 36);
        TabStop = true;
        Cursor = Cursors.Default;
    }

    internal void Start()
    {
        SelectStep(0);
        Focus();
    }

    private void CaptureViewport()
    {
        _snapshot?.Dispose();
        _snapshot = null;
        if (_scroller.Width <= 0 || _scroller.Height <= 0) return;
        try
        {
            // Capture the existing UI before repainting the overlay; only a screenshot
            // is drawn, so mouse clicks never reach the underlying audio settings.
            var image = new Bitmap(_scroller.Width, _scroller.Height);
            _scroller.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
            _snapshot = image;
        }
        catch (Exception)
        {
            // Some graphics drivers/custom controls may not support DrawToBitmap.
            // The help remains usable with its solid dark background.
        }
    }

    private void SelectStep(int requested)
    {
        if (_items.Count == 0) return;
        _index = Math.Clamp(requested, 0, _items.Count - 1);
        var target = _items[_index].Target;
        if (target.IsHandleCreated)
        {
            // Scroll only within the already-existing viewport.
            _scroller.ScrollControlIntoView(target);
        }
        CaptureViewport();
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (IsHandleCreated) { CaptureViewport(); Invalidate(); }
    }

    protected override bool IsInputKey(Keys keyData)
        => keyData is Keys.Escape or Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) { _close(); e.Handled = true; }
        else if (e.KeyCode is Keys.Right or Keys.Enter) { SelectStep(_index + 1); e.Handled = true; }
        else if (e.KeyCode == Keys.Left) { SelectStep(_index - 1); e.Handled = true; }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (_exit.Contains(e.Location)) _close();
        else if (_prev.Contains(e.Location) && _index > 0) SelectStep(_index - 1);
        else if (_next.Contains(e.Location) && _index + 1 < _items.Count) SelectStep(_index + 1);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Delta < 0 && _index + 1 < _items.Count) SelectStep(_index + 1);
        else if (e.Delta > 0 && _index > 0) SelectStep(_index - 1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        if (_snapshot is not null)
            g.DrawImageUnscaled(_snapshot, 0, 0);
        using (var shade = new SolidBrush(Color.FromArgb(194, 10, 15, 25)))
            g.FillRectangle(shade, ClientRectangle);

        _exit = new Rectangle(Math.Max(8, Width - 54), 10, 40, 37);
        using (var exitBrush = new SolidBrush(Color.FromArgb(51, 69, 93)))
            g.FillRoundedRect(exitBrush, _exit, 8);
        TextRenderer.DrawText(g, "×", _closeFont, _exit, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        if (_items.Count == 0) return;
        var item = _items[_index];
        _highlight = Rectangle.Empty;
        if (item.Target.IsHandleCreated)
        {
            var topLeft = PointToClient(item.Target.PointToScreen(Point.Empty));
            _highlight = new Rectangle(topLeft, item.Target.Size);
            _highlight.Inflate(5, 5);
            // Restore the control's original appearance through the dim layer.
            if (_snapshot is not null)
            {
                var clipped = Rectangle.Intersect(_highlight,
                    new Rectangle(Point.Empty, _snapshot.Size));
                if (!clipped.IsEmpty) g.DrawImage(_snapshot, clipped, clipped, GraphicsUnit.Pixel);
            }
            using var glow = new Pen(Color.FromArgb(115, 179, 255), 2.5f);
            if (_highlight.Width > 0 && _highlight.Height > 0)
                g.DrawRoundedRect(glow, _highlight, 6);
        }

        int cardWidth = Math.Clamp(Width - 32, 220, 460);
        int cardHeight = 220;
        // Prefer the immediate right-hand side of the highlighted control; use
        // a position above/below it when the viewport doesn't have enough room.
        int x = Math.Clamp(_highlight.Right + 16, 12, Math.Max(12, Width - cardWidth - 12));
        int y = _highlight.Top;
        if (x < _highlight.Right + 8 || _highlight.IsEmpty)
        {
            x = Math.Clamp(_highlight.Left, 12, Math.Max(12, Width - cardWidth - 12));
            y = _highlight.Bottom + 13;
            if (y + cardHeight > Height - 12) y = _highlight.Top - cardHeight - 13;
        }
        y = Math.Clamp(y, 55, Math.Max(55, Height - cardHeight - 12));
        _card = new Rectangle(x, y, cardWidth, cardHeight);
        using (var shadow = new SolidBrush(Color.FromArgb(225, 29, 41, 60)))
            g.FillRoundedRect(shadow, _card, 12);
        using (var border = new Pen(Color.FromArgb(91, 141, 206), 1.3f))
            g.DrawRoundedRect(border, _card, 12);

        var inner = new Rectangle(_card.X + 18, _card.Y + 14, _card.Width - 36, _card.Height - 28);
        TextRenderer.DrawText(g, "사용자 가이드  ·  " + (_index + 1) + " / " + _items.Count,
            _smallFont, new Rectangle(inner.X, inner.Y, inner.Width, 24), Color.FromArgb(154, 197, 255));
        TextRenderer.DrawText(g, item.Title, _titleFont,
            new Rectangle(inner.X, inner.Y + 30, inner.Width, 32), Color.White,
            TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, item.Description, _bodyFont,
            new Rectangle(inner.X, inner.Y + 67, inner.Width, 88), Color.FromArgb(222, 233, 247),
            TextFormatFlags.WordBreak | TextFormatFlags.Top);

        _prev = new Rectangle(inner.X, _card.Bottom - 45, 95, 30);
        _next = new Rectangle(_card.Right - 113, _card.Bottom - 45, 95, 30);
        DrawNavButton(g, _prev, "이전", _index > 0);
        DrawNavButton(g, _next, _index == _items.Count - 1 ? "마지막" : "다음", _index < _items.Count - 1);
    }

    private static void DrawNavButton(Graphics g, Rectangle bounds, string text, bool enabled)
    {
        using var brush = new SolidBrush(enabled ? Color.FromArgb(51, 89, 142) : Color.FromArgb(49, 58, 72));
        g.FillRoundedRect(brush, bounds, 6);
        TextRenderer.DrawText(g, text, SystemFonts.MessageBoxFont, bounds,
            enabled ? Color.White : Color.FromArgb(160, 171, 189),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _snapshot?.Dispose();
            _titleFont.Dispose();
            _bodyFont.Dispose();
            _smallFont.Dispose();
            _closeFont.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal static class RoundedRectDrawing
{
    internal static void FillRoundedRect(this Graphics g, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreatePath(bounds, radius);
        g.FillPath(brush, path);
    }

    internal static void DrawRoundedRect(this Graphics g, Pen pen, Rectangle bounds, int radius)
    {
        using var path = CreatePath(bounds, radius);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath CreatePath(Rectangle rect, int radius)
    {
        int d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        p.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        p.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
