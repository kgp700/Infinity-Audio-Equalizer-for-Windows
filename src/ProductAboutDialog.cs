// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityAudioEqualizerPro;

internal sealed class ProductAboutDialog : Form
{
    private static readonly Color Bg=Color.FromArgb(21,26,36);
    private static readonly Color Fg=Color.FromArgb(230,238,246);
    private static readonly Color Accent=Color.FromArgb(138,180,255);
    internal ProductAboutDialog()
    {
        Text="버전 정보";ClientSize=new Size(515,467);MinimumSize=new Size(515,467);
        FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
        StartPosition=FormStartPosition.CenterParent;BackColor=Bg;ForeColor=Fg;
        Font=new Font("Segoe UI",9);
        string version=Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "";
        int y=18;
        void Label(string value,bool header=false)
        {
            Controls.Add(new Label { Text=value,Location=new Point(21,y),Size=new Size(474,header?41:27),
                TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,ForeColor=header?Accent:Fg,
                Font=new Font("Segoe UI",header?15f:9.5f,header?FontStyle.Bold:FontStyle.Regular) });
            y+=header?49:30;
        }
        Label("Infinity Audio Equalizer Pro for Windows",true);
        Label("제작자: KGP Software Lab");
        Label("버전: "+version+" Beta");
        Label("Build date: "+ReadBuildDate());
        Label("Copyright © 2026 KGP Software Lab · MIT License");
        y+=8;
        void Button(string label,Action action)
        {
            var button=new AccentButton {Text=label,Location=new Point(21,y),Size=new Size(472,33),Font=Font};
            button.Click+=(_,_)=>{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"문서",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
            Controls.Add(button);y+=40;
        }
        Button("개인정보처리방침",()=>OpenLegal("개인정보처리방침","PRIVACY_POLICY.md"));
        Button("앱 이용약관 / 라이선스",()=>OpenLegal("앱 이용약관 / 라이선스","TERMS_AND_LICENSE.md"));
        Button("오픈소스 라이선스",()=>OpenLegal("오픈소스 라이선스","OPEN_SOURCE_LICENSES.md"));
        Button("프로그램 소스 코드 (GitHub)",()=>Process.Start(new ProcessStartInfo(
            "https://github.com/kgp700/Infinity-Audio-Equalizer-for-Windows"){UseShellExecute=true}));
        Button("닫기",Close);
    }
    private static string ReadBuildDate()
    {
        try
        {
            string file=Path.Combine(AppContext.BaseDirectory,"build_metadata.json");
            if(File.Exists(file))
            {
                using var doc=JsonDocument.Parse(File.ReadAllText(file));
                if(doc.RootElement.TryGetProperty("build_date",out var value))
                {
                    string? date=value.GetString();
                    if(!string.IsNullOrWhiteSpace(date))return date;
                }
            }
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException) { }
        return "정보 없음";
    }
    private void OpenLegal(string title,string name)
    {
        string path=Path.Combine(AppContext.BaseDirectory,"legal",name);
        if(!File.Exists(path))
        {
            MessageBox.Show(this,"문서를 열 수 없습니다. 설치 파일을 확인해 주세요.",title,MessageBoxButtons.OK,MessageBoxIcon.Information);
            return;
        }
        using var viewer=new Form {Text=title,Size=new Size(750,610),MinimumSize=new Size(580,430),
            StartPosition=FormStartPosition.CenterParent,BackColor=Bg,ForeColor=Fg,Font=Font};
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=new Padding(14)};
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));
        viewer.Controls.Add(layout);
        var doc=new RichTextBox {ReadOnly=true,BorderStyle=BorderStyle.FixedSingle,Dock=DockStyle.Fill,
            BackColor=Color.FromArgb(24,33,45),ForeColor=Fg,Font=new Font("Segoe UI",10),
            DetectUrls=true,HideSelection=false,ScrollBars=RichTextBoxScrollBars.Vertical,WordWrap=true};
        doc.LinkClicked+=(_,e)=>{if(Uri.TryCreate(e.LinkText,UriKind.Absolute,out var uri) &&
            (uri.Scheme==Uri.UriSchemeHttps||uri.Scheme==Uri.UriSchemeHttp))
            Process.Start(new ProcessStartInfo(uri.ToString()){UseShellExecute=true});};
        RenderMarkdown(doc,File.ReadAllText(path));
        layout.Controls.Add(doc,0,0);
        var bar=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};
        var close=new AccentButton {Text="닫기",Width=98,Height=30};
        close.Click+=(_,_)=>viewer.Close();bar.Controls.Add(close);
        layout.Controls.Add(bar,0,1);
        viewer.ShowDialog(this);
    }
    // Small local Markdown renderer: preserve heading hierarchy, emphasis, paragraphs,
    // list structure and two-column license tables, without a browser dependency.
    private static void RenderMarkdown(RichTextBox view,string markdown)
    {
        view.SuspendLayout();view.Clear();
        var normal=new Font("Segoe UI",10f);
        var strong=new Font("Segoe UI",10f,FontStyle.Bold);
        var heading=new Font("Segoe UI",15.5f,FontStyle.Bold);
        var subheading=new Font("Segoe UI",12.3f,FontStyle.Bold);
        var section=new Font("Segoe UI",10.7f,FontStyle.Bold);
        void Append(string text,Font font,Color tint)
        {
            view.SelectionStart=view.TextLength;view.SelectionLength=0;
            view.SelectionFont=font;view.SelectionColor=tint;view.AppendText(text);
        }
        void Inline(string value,Font current,Color tint)
        {
            string[] parts=Regex.Split(value,@"(\*\*[^*]+\*\*)");
            foreach(string part in parts)
            {
                if(part.StartsWith("**") && part.EndsWith("**") && part.Length>4)
                    Append(part[2..^2],strong,tint);
                else Append(part.Replace("  "," "),current,tint);
            }
        }
        foreach(string raw in markdown.Replace("\r\n","\n").Split('\n'))
        {
            string line=raw.TrimEnd();
            if(string.IsNullOrWhiteSpace(line)){Append("\n",normal,Fg);continue;}
            if(Regex.IsMatch(line,@"^\|\s*:?-{3,}"))continue;
            int level=0;while(level<line.Length && line[level]=='#')level++;
            bool isHeading=level>0 && level<=4 && line.Length>level && line[level]==' ';
            if(isHeading)line=line[(level+1)..];
            else if(line.StartsWith("- "))line="• "+line[2..];
            else if(line.StartsWith("| "))
            {
                var cells=line.Trim().Trim('|').Split('|').Select(x=>x.Trim()).ToArray();
                if(cells.Length>=2)line=cells[0]+"   —   "+string.Join(" · ",cells.Skip(1));
            }
            Inline(line,isHeading?(level==1?heading:level==2?subheading:section):normal,isHeading?Color.White:Fg);
            Append("\n",normal,Fg);
            if(isHeading)Append("\n",normal,Fg);
        }
        view.SelectionStart=0;view.SelectionLength=0;view.ScrollToCaret();
        view.ResumeLayout();
    }
}
