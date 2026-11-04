// Copyright (c) 2026 KGP Software Lab. SPDX-License-Identifier: MIT
using Microsoft.Win32;
using System.Security;

namespace InfinityAudioEqualizerPro;

// Read-only Windows endpoint registration probe. Never changes FxProperties.
internal enum EndpointApoRegistration { Registered, Missing, Unknown }

internal static class ApoEndpointRegistration
{
    private const string RenderEndpoints = @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render";
    private const string FxPrefix = "{d04e05a6-594b-4fb6-a80d-01af5eed7d1d}";

    public static EndpointApoRegistration Read(string? endpointGuid, string? selectorPath)
    {
        if (string.IsNullOrWhiteSpace(endpointGuid) || string.IsNullOrWhiteSpace(selectorPath))
            return EndpointApoRegistration.Unknown;
        if (!Guid.TryParse(endpointGuid, out var parsed)) return EndpointApoRegistration.Unknown;
        string xtRoot = Path.GetDirectoryName(selectorPath) ?? "";
        if (xtRoot.Length == 0) return EndpointApoRegistration.Unknown;
        try
        {
            using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var fx = machine.OpenSubKey($@"{RenderEndpoints}\{{{parsed:D}}}\FxProperties");
            if (fx is null) return EndpointApoRegistration.Missing;
            bool anySlot = false;
            foreach (var name in fx.GetValueNames())
            {
                if (!name.StartsWith(FxPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                if (fx.GetValue(name) is not string raw || !Guid.TryParse(raw, out var clsid)) continue;
                anySlot = true;
                using var classKey = machine.OpenSubKey($@"SOFTWARE\Classes\CLSID\{{{clsid:D}}}\InprocServer32");
                string? server = classKey?.GetValue(null) as string;
                if (string.IsNullOrWhiteSpace(server))
                {
                    // Unknown third-party server / unresolved CLSID: don't guess.
                    continue;
                }
                string fullServer = Path.GetFullPath(Environment.ExpandEnvironmentVariables(server.Trim('"')));
                string fullRoot = Path.GetFullPath(xtRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (fullServer.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(fullServer).Contains("EqualizerAPO", StringComparison.OrdinalIgnoreCase))
                    return EndpointApoRegistration.Registered;
            }
            // An unrelated APO is not proof of XT registration.
            return EndpointApoRegistration.Missing;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException or ArgumentException)
        {
            return EndpointApoRegistration.Unknown;
        }
    }
}
