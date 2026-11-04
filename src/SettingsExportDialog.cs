// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Drawing;

namespace InfinityAudioEqualizerPro;

/// <summary>
/// Explicit export choice. A dedicated dialog replaces the ambiguous Yes/No prompt;
/// the caller still uses Yes for share and No for local full backup.
/// </summary>
public sealed class SettingsExportDialog : Form
{
    private static readonly Color Background = Color.FromArgb(21, 26, 36);
    private static readonly Color Surface = Color.FromArgb(30, 39, 53);
    private static readonly Color TextColor = Color.FromArgb(231, 240, 250);
    private static readonly Color MutedText = Color.FromArgb(173, 192, 212);

    public SettingsExportDialog()
    {
        Text = "설정 내보내기";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(700, 342);
        BackColor = Background;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 9.5f);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18, 16, 18, 14),
            BackColor = Background,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 47));
        Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Text = "내보낼 설정을 선택하세요",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(116, 172, 255),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold)
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = "원하는 유형을 선택한 다음 저장할 파일 위치를 지정할 수 있습니다.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = MutedText
        }, 0, 1);

        layout.Controls.Add(ChoiceRow(
            "현재 장치의 음향 효과",
            "출력 장치 ID와 개인 경로를 제외하고 저장합니다. 다른 사람에게 공유할 때 사용하세요.",
            "타인 공유용",
            DialogResult.Yes), 0, 2);
        layout.Controls.Add(ChoiceRow(
            "전체 장치 및 환경 백업",
            "장치별 프리셋과 개인 설정을 모두 저장합니다. 내 컴퓨터에서 복원할 때 사용하세요.",
            "내 컴퓨터용",
            DialogResult.No), 0, 3);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Background,
            Padding = new Padding(0, 6, 1, 0)
        };
        var cancel = new AccentButton { Text = "취소", Width = 100, Height = 32, Margin = new Padding(0) };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        bottom.Controls.Add(cancel);
        layout.Controls.Add(bottom, 0, 4);
        CancelButton = cancel;
    }

    private Control ChoiceRow(string title, string description, string buttonLabel, DialogResult result)
    {
        var frame = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 4),
            Padding = new Padding(12, 7, 10, 7),
            BackColor = Surface,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        frame.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        frame.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));
        frame.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var text = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = Surface
        };
        text.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        text.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = TextColor
        }, 0, 0);
        text.Controls.Add(new Label
        {
            Text = description,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = MutedText
        }, 0, 1);
        frame.Controls.Add(text, 0, 0);

        var choose = new AccentButton
        {
            Text = buttonLabel,
            Width = 150,
            Height = 38,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        choose.Click += (_, _) => { DialogResult = result; Close(); };
        frame.Controls.Add(choose, 1, 0);
        return frame;
    }
}
