// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using System.Text;
namespace InfinityAudioEqualizerPro;
internal static class DiagnosticLog
{
    private static readonly object Gate=new();
    public static void Error(string operation,Exception error)
    {
        try
        {
            string folder=Path.Combine(SettingsStore.CurrentDirectory, "Logs");
            Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"apo-apply.log");
            lock(Gate)
            {
                if(File.Exists(path) && new FileInfo(path).Length>1_000_000)
                {
                    string old=Path.Combine(folder,"apo-apply.previous.log");
                    File.Move(path,old,true);
                }
                File.AppendAllText(path,$"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] {operation}: {error}\n",new UTF8Encoding(false));
            }
        }
        catch { /* Logging must not break audio restoration or UI state. */ }
    }
}
