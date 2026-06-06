# Extend-AllMonitors-Fixed.ps1
# Ripristina la modalita Estendi su tutti i monitor.
# Tenta in ordine: DisplaySwitch.exe (piu affidabile), poi SetDisplayConfig via P/Invoke.

Write-Host "Tentativo 1: DisplaySwitch.exe /extend"
$proc = Start-Process -FilePath "DisplaySwitch.exe" -ArgumentList "/extend" -Wait -PassThru
Write-Host "DisplaySwitch exit code: $($proc.ExitCode)"
if ($proc.ExitCode -eq 0) {
    Start-Sleep -Milliseconds 1500
    Write-Host "SUCCESS: modalita Estendi applicata." -ForegroundColor Green
    exit 0
}

Write-Host "DisplaySwitch fallito, provo SetDisplayConfig..."

$csharpCode = @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class DisplayExtender
{
    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(
        uint flags, out uint np, out uint nm);

    [DllImport("user32.dll", EntryPoint = "QueryDisplayConfig")]
    private static extern int QueryDisplayConfigRaw(
        uint flags, ref uint np,
        [Out] byte[] pathArray,
        ref uint nm,
        [Out] byte[] modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    private static extern int SetDisplayConfig(
        uint np, [In] byte[] pathArray,
        uint nm, [In] byte[] modeInfoArray,
        uint flags);

    private const uint QDC_ALL_PATHS        = 0x00000001;
    private const uint SDC_APPLY            = 0x00000080;
    private const uint SDC_SAVE_TO_DATABASE = 0x00000200;
    private const uint SDC_USE_SUPPLIED     = 0x00000020;
    private const uint SDC_ALLOW_CHANGES    = 0x00000400;
    private const uint SDC_TOPOLOGY_EXTEND  = 4;
    private const uint PATH_ACTIVE          = 0x00000001;
    private const int  PATH_STRUCT_SIZE     = 72;
    private const int  SOURCE_MODEINFOIDX   = 12;
    private const int  TARGET_MODEINFOIDX   = 32;
    private const int  PATH_FLAGS_OFFSET    = 68;

    private static void WriteU32(byte[] b, int o, uint v)
    {
        b[o]   = (byte)(v);
        b[o+1] = (byte)(v >> 8);
        b[o+2] = (byte)(v >> 16);
        b[o+3] = (byte)(v >> 24);
    }
    private static uint ReadU32(byte[] b, int o)
    {
        return (uint)(b[o] | (b[o+1]<<8) | (b[o+2]<<16) | (b[o+3]<<24));
    }

    public static int ExtendAll()
    {
        // Step 1: topology hint
        int hr = SetDisplayConfig(0, null, 0, null,
            SDC_APPLY | SDC_ALLOW_CHANGES | SDC_TOPOLOGY_EXTEND);
        Console.WriteLine("Step1 Topology: " + hr);
        if (hr == 0) return 0;

        // Step 2: real path array
        uint np, nm;
        hr = GetDisplayConfigBufferSizes(QDC_ALL_PATHS, out np, out nm);
        if (hr != 0) { Console.WriteLine("GetBufferSizes: " + hr); return hr; }

        var pathBuf = new byte[np * PATH_STRUCT_SIZE];
        var modeBuf = new byte[nm * 64];
        hr = QueryDisplayConfigRaw(QDC_ALL_PATHS, ref np, pathBuf, ref nm, modeBuf, IntPtr.Zero);
        Console.WriteLine("QueryDisplayConfig: hr=" + hr + " np=" + np);
        if (hr != 0) return hr;

        var seen = new HashSet<string>();
        var paths = new List<byte[]>();
        for (int i = 0; i < (int)np; i++)
        {
            int off = i * PATH_STRUCT_SIZE;
            string key = ReadU32(pathBuf, off+20) + ":" + ReadU32(pathBuf, off+24) + ":" + ReadU32(pathBuf, off+28);
            if (seen.Contains(key)) continue;
            seen.Add(key);
            var p = new byte[PATH_STRUCT_SIZE];
            Array.Copy(pathBuf, off, p, 0, PATH_STRUCT_SIZE);
            WriteU32(p, SOURCE_MODEINFOIDX, 0xFFFFFFFF);
            WriteU32(p, TARGET_MODEINFOIDX, 0xFFFFFFFF);
            WriteU32(p, PATH_FLAGS_OFFSET,  PATH_ACTIVE);
            paths.Add(p);
        }

        Console.WriteLine("Unique targets: " + paths.Count);
        if (paths.Count == 0) return -1;

        var selBuf = new byte[paths.Count * PATH_STRUCT_SIZE];
        for (int i = 0; i < paths.Count; i++)
            Array.Copy(paths[i], 0, selBuf, i * PATH_STRUCT_SIZE, PATH_STRUCT_SIZE);

        hr = SetDisplayConfig((uint)paths.Count, selBuf, 0, null,
            SDC_APPLY | SDC_SAVE_TO_DATABASE | SDC_USE_SUPPLIED | SDC_ALLOW_CHANGES);
        Console.WriteLine("Step2 WithPaths+Save: " + hr);
        if (hr == 0) return 0;

        hr = SetDisplayConfig((uint)paths.Count, selBuf, 0, null,
            SDC_APPLY | SDC_USE_SUPPLIED | SDC_ALLOW_CHANGES);
        Console.WriteLine("Step3 WithPaths NoDB: " + hr);
        return hr;
    }
}
'@

try {
    Add-Type -TypeDefinition $csharpCode -Language CSharp -ErrorAction Stop
} catch {
    $src = "$env:TEMP\DisplayExtenderFix4.cs"
    $dll = "$env:TEMP\DisplayExtenderFix4.dll"
    Set-Content -Path $src -Value $csharpCode -Encoding UTF8
    $csc = @("C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
             "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe") |
           Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $csc) { Write-Error "csc.exe non trovato."; exit 1 }
    & $csc /target:library /out:$dll $src 2>&1 | Write-Host
    if (-not (Test-Path $dll)) { Write-Error "Compilazione fallita."; exit 1 }
    Add-Type -Path $dll
}

$result = [DisplayExtender]::ExtendAll()
if ($result -eq 0) {
    Write-Host "SUCCESS: tutti i monitor in modalita Estendi." -ForegroundColor Green
} else {
    Write-Host "ERRORE codice $result" -ForegroundColor Red
}
exit $result
