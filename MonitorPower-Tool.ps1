# MonitorPower-Tool.ps1
# Strumento unificato per gestione monitor
# Comandi:
#   extend          - Estendi su tutti i monitor (via DisplaySwitch.exe o SetDisplayConfig)
#   save [file]     - Salva layout corrente come JSON (default: defaultLayout.json)
#   restore [file]  - Ripristina layout da JSON (default: defaultLayout.json)
#   test            - Test varie combinazioni di flag SetDisplayConfig

param(
    [ValidateSet("extend", "save", "restore", "test")]
    [string]$Command = "extend",
    [string]$File = ""
)

$ConfigDir = "$env:LOCALAPPDATA\MonitorPowerExtension"
$DefaultFile = Join-Path $ConfigDir "defaultLayout.json"
if ($File -eq "") { $File = $DefaultFile }

# ─── helpers ────────────────────────────────────────────────

function Write-Result($ok, $msg) {
    if ($ok) { Write-Host "OK: $msg" -ForegroundColor Green }
    else     { Write-Host "ERRORE: $msg" -ForegroundColor Red }
}

# ─── extend ─────────────────────────────────────────────────

function Invoke-ExtendAll {
    Write-Host "=== Estendi tutti i monitor ===" -ForegroundColor Cyan
    Write-Host "Tentativo 1: DisplaySwitch.exe /extend"
    $proc = Start-Process -FilePath "DisplaySwitch.exe" -ArgumentList "/extend" -Wait -PassThru
    Write-Host "DisplaySwitch exit code: $($proc.ExitCode)"
    if ($proc.ExitCode -eq 0) {
        Start-Sleep -Milliseconds 1500
        Write-Result $true "modalità Estendi applicata."
        return $true
    }

    Write-Host "DisplaySwitch fallito, provo SetDisplayConfig..."

    $csharp = @'
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

    private static void WriteU32(byte[] b, int o, uint v) {
        b[o]= (byte)(v); b[o+1]= (byte)(v>>8); b[o+2]= (byte)(v>>16); b[o+3]= (byte)(v>>24);
    }
    private static uint ReadU32(byte[] b, int o) {
        return (uint)(b[o]|(b[o+1]<<8)|(b[o+2]<<16)|(b[o+3]<<24));
    }

    public static int ExtendAll() {
        int hr = SetDisplayConfig(0, null, 0, null,
            SDC_APPLY | SDC_ALLOW_CHANGES | SDC_TOPOLOGY_EXTEND);
        Console.WriteLine("Step1 Topology: " + hr);
        if (hr == 0) return 0;

        uint np, nm;
        hr = GetDisplayConfigBufferSizes(QDC_ALL_PATHS, out np, out nm);
        if (hr != 0) { Console.WriteLine("GetBufferSizes: " + hr); return hr; }

        var pathBuf = new byte[np * PATH_STRUCT_SIZE];
        var modeBuf = new byte[nm * 64];
        hr = QueryDisplayConfigRaw(QDC_ALL_PATHS, ref np, pathBuf, ref nm, modeBuf, IntPtr.Zero);
        Console.WriteLine("Query: hr=" + hr + " np=" + np);
        if (hr != 0) return hr;

        var seen = new HashSet<string>();
        var paths = new List<byte[]>();
        for (int i = 0; i < (int)np; i++) {
            int off = i * PATH_STRUCT_SIZE;
            string key = ReadU32(pathBuf, off+20) + ":" + ReadU32(pathBuf, off+24) + ":" + ReadU32(pathBuf, off+28);
            if (seen.Contains(key)) continue;
            seen.Add(key);
            var p = new byte[PATH_STRUCT_SIZE];
            Array.Copy(pathBuf, off, p, 0, PATH_STRUCT_SIZE);
            WriteU32(p, SOURCE_MODEINFOIDX, 0xFFFFFFFF);
            WriteU32(p, TARGET_MODEINFOIDX, 0xFFFFFFFF);
            WriteU32(p, PATH_FLAGS_OFFSET, PATH_ACTIVE);
            paths.Add(p);
        }
        Console.WriteLine("Unique: " + paths.Count);
        if (paths.Count == 0) return -1;

        var selBuf = new byte[paths.Count * PATH_STRUCT_SIZE];
        for (int i = 0; i < paths.Count; i++)
            Array.Copy(paths[i], 0, selBuf, i * PATH_STRUCT_SIZE, PATH_STRUCT_SIZE);

        hr = SetDisplayConfig((uint)paths.Count, selBuf, 0, null,
            SDC_APPLY | SDC_SAVE_TO_DATABASE | SDC_USE_SUPPLIED | SDC_ALLOW_CHANGES);
        Console.WriteLine("Step2 +Save: " + hr);
        if (hr == 0) return 0;

        hr = SetDisplayConfig((uint)paths.Count, selBuf, 0, null,
            SDC_APPLY | SDC_USE_SUPPLIED | SDC_ALLOW_CHANGES);
        Console.WriteLine("Step3 NoDB: " + hr);
        return hr;
    }
}
'@

    try {
        Add-Type -TypeDefinition $csharp -Language CSharp -ErrorAction Stop
        $result = [DisplayExtender]::ExtendAll()
    } catch {
        $src = "$env:TEMP\MonitorPowerExtender.cs"
        $dll = "$env:TEMP\MonitorPowerExtender.dll"
        Set-Content -Path $src -Value $csharp -Encoding UTF8
        $csc = @("C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
                 "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe") |
               Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $csc) { Write-Error "csc.exe non trovato."; return $false }
        & $csc /target:library /out:$dll $src 2>&1 | Write-Host
        if (-not (Test-Path $dll)) { Write-Error "Compilazione fallita."; return $false }
        Add-Type -Path $dll
        $result = [DisplayExtender]::ExtendAll()
    }

    if ($result -eq 0) {
        Write-Result $true "tutti i monitor in modalità Estendi."
        return $true
    }
    Write-Result $false "codice $result"
    return $false
}

# ─── capture layout ─────────────────────────────────────────

function Save-LayoutToJson($filePath) {
    Write-Host "=== Salva layout monitor in: $filePath ===" -ForegroundColor Cyan

    $csharp = @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class CaptureLayout
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int EnumDisplaySettings(
        string lpszDeviceName, uint iModeNum, ref DEVMODE lpDevMode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(
        string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX; public int dmPositionY;
        public int dmDisplayOrientation; public int dmDisplayFixedOutput;
        public short dmColor; public short dmDuplex; public short dmYResolution;
        public short dmTTOption; public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels; public int dmBitsPerPel;
        public int dmPelsWidth; public int dmPelsHeight;
        public int dmDisplayFlags; public int dmDisplayFrequency;
        public int dmICMMethod; public int dmICMIntent; public int dmMediaType;
        public int dmDitherType; public int dmReserved1; public int dmReserved2;
        public int dmPanningWidth; public int dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public uint cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    private const uint ENUM_CURRENT_SETTINGS = 0xFFFFFFFF;

    public static string CaptureAllMonitorsJson() {
        var targets = new List<string>();
        uint devIndex = 0;
        var dd = new DISPLAY_DEVICE { cb = (uint)Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };

        while (EnumDisplayDevices(null, devIndex, ref dd, 0)) {
            string dn = dd.DeviceName;
            if (!dn.StartsWith("\\\\.\\DISPLAY")) {
                devIndex++; dd = new DISPLAY_DEVICE { cb = (uint)Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };
                continue;
            }
            var dm = default(DEVMODE);
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            int ok = EnumDisplaySettings(dn, ENUM_CURRENT_SETTINGS, ref dm);
            if (ok == 0 || dm.dmPelsWidth == 0 || dm.dmPelsHeight == 0) {
                devIndex++; dd = new DISPLAY_DEVICE { cb = (uint)Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };
                continue;
            }

            bool primary = dm.dmPositionX == 0 && dm.dmPositionY == 0;
            targets.Add("{\"DeviceName\":\"" + dn.Replace("\\", "\\\\") +
                "\",\"Width\":" + (int)dm.dmPelsWidth +
                ",\"Height\":" + (int)dm.dmPelsHeight +
                ",\"Frequency\":" + (int)dm.dmDisplayFrequency +
                ",\"PositionX\":" + dm.dmPositionX +
                ",\"PositionY\":" + dm.dmPositionY +
                ",\"Orientation\":" + dm.dmDisplayOrientation +
                ",\"BitsPerPel\":" + (int)dm.dmBitsPerPel +
                ",\"IsPrimary\":" + (primary?"true":"false") + "}");
            devIndex++;
            dd = new DISPLAY_DEVICE { cb = (uint)Marshal.SizeOf(typeof(DISPLAY_DEVICE)) };
        }
        return "{\"targets\":[" + string.Join(",", targets.ToArray()) + "]}";
    }
}
'@

    try {
        Add-Type -TypeDefinition $csharp -Language CSharp -ErrorAction Stop
        $json = [CaptureLayout]::CaptureAllMonitorsJson()
    } catch {
        $src = "$env:TEMP\MonitorPowerCapture.cs"
        $dll = "$env:TEMP\MonitorPowerCapture.dll"
        Set-Content -Path $src -Value $csharp -Encoding UTF8
        $csc = @("C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
                 "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe") |
               Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $csc) { Write-Error "csc.exe non trovato."; return $false }
        & $csc /target:library /out:$dll $src 2>&1 | Write-Host
        if (-not (Test-Path $dll)) { Write-Error "Compilazione fallita."; return $false }
        Add-Type -Path $dll
        $json = [CaptureLayout]::CaptureAllMonitorsJson()
    }

    $json | Out-File -FilePath $filePath -Encoding UTF8
    Write-Host "Layout salvato: $filePath" -ForegroundColor Green
    Write-Host $json
    return $true
}

# ─── restore layout ─────────────────────────────────────────

function Restore-LayoutFromJson($filePath) {
    Write-Host "=== Ripristina layout monitor da: $filePath ===" -ForegroundColor Cyan
    if (!(Test-Path $filePath)) {
        Write-Result $false "file non trovato: $filePath"
        return $false
    }

    $snapshot = Get-Content $filePath -Raw | ConvertFrom-Json
    Write-Host "Trovati $($snapshot.targets.Count) monitor." -ForegroundColor Cyan

    $csharp = @'
using System;
using System.Runtime.InteropServices;

public static class DisplayRestore
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(
        string lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwFlags, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int EnumDisplaySettings(
        string lpszDeviceName, uint iModeNum, ref DEVMODE lpDevMode);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion; public short dmDriverVersion;
        public short dmSize; public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX; public int dmPositionY;
        public int dmDisplayOrientation; public int dmDisplayFixedOutput;
        public short dmColor; public short dmDuplex; public short dmYResolution;
        public short dmTTOption; public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels; public int dmBitsPerPel;
        public int dmPelsWidth; public int dmPelsHeight;
        public int dmDisplayFlags; public int dmDisplayFrequency;
        public int dmICMMethod; public int dmICMIntent; public int dmMediaType;
        public int dmDitherType; public int dmReserved1; public int dmReserved2;
        public int dmPanningWidth; public int dmPanningHeight;
    }

    private const int DM_DISPLAYORIENTATION = 0x00000080;
    private const int DM_POSITION = 0x00000020;
    private const uint CDS_UPDATEREGISTRY = 0x01;
    private const uint CDS_GLOBAL = 0x08;
    private const uint CDS_NORESET = 0x10000000;
    private const uint CDS_SET_PRIMARY = 0x10;
    private const uint ENUM_CURRENT_SETTINGS = 0xFFFFFFFF;
    private const int DISP_CHANGE_SUCCESSFUL = 0;

    public static int ApplyOrientationAndPosition(
        string deviceName, int orientation, int posX, int posY, bool isPrimary)
    {
        var dm = default(DEVMODE);
        dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        int ok = EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm);
        if (ok == 0) return -1;
        dm.dmFields = DM_DISPLAYORIENTATION | DM_POSITION;
        dm.dmDisplayOrientation = orientation;
        dm.dmPositionX = posX; dm.dmPositionY = posY;
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DEVMODE)));
        try {
            Marshal.StructureToPtr(dm, ptr, false);
            uint flags = CDS_UPDATEREGISTRY | CDS_GLOBAL | CDS_NORESET;
            if (isPrimary) flags |= CDS_SET_PRIMARY;
            int r1 = ChangeDisplaySettingsEx(deviceName, ptr, IntPtr.Zero, flags, IntPtr.Zero);
            if (r1 != 0) return r1;
        } finally { Marshal.FreeHGlobal(ptr); }
        return ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
    }
}
'@

    try {
        Add-Type -TypeDefinition $csharp -Language CSharp -ErrorAction Stop
    } catch {
        $src = "$env:TEMP\MonitorPowerRestore.cs"
        $dll = "$env:TEMP\MonitorPowerRestore.dll"
        Set-Content -Path $src -Value $csharp -Encoding UTF8
        $csc = @("C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
                 "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe") |
               Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $csc) { Write-Error "csc.exe non trovato."; return $false }
        & $csc /target:library /out:$dll $src 2>&1 | Write-Host
        if (-not (Test-Path $dll)) { Write-Error "Compilazione fallita."; return $false }
        Add-Type -Path $dll
    }

    $globalResult = 0
    foreach ($t in $snapshot.targets) {
        $dn = $t.DeviceName
        if ($t.Width -eq 0 -or $t.Height -eq 0) {
            Write-Host "Salto $dn (ghost target)" -ForegroundColor DarkGray
            continue
        }
        Write-Host "Ripristino $dn -> Orientation=$($t.Orientation) Pos=($($t.PositionX),$($t.PositionY)) Primary=$($t.IsPrimary)"
        $r = [DisplayRestore]::ApplyOrientationAndPosition($dn, $t.Orientation, $t.PositionX, $t.PositionY, $t.IsPrimary)
        if ($r -eq 0) { Write-Host "  OK" -ForegroundColor Green }
        else { Write-Host "  ERRORE $r" -ForegroundColor Red; $globalResult = $r }
        Start-Sleep -Milliseconds 500
    }

    if ($globalResult -eq 0) { Write-Result $true "layout ripristinato." }
    else { Write-Result $false "codice $globalResult" }
    return ($globalResult -eq 0)
}

# ─── test (da Extend-AllMonitors.ps1) ───────────────────────

function Invoke-TestFlags {
    Write-Host "=== Test flag SetDisplayConfig ===" -ForegroundColor Cyan
    $csharp = @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public static class DisplayExtender
{
    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint np, out uint nm);
    [DllImport("user32.dll")]
    private static unsafe extern int QueryDisplayConfig(uint flags, ref uint np,
        DISPLAYCONFIG_PATH_INFO* p, ref uint nm, DISPLAYCONFIG_MODE_INFO* m, IntPtr topo);
    [DllImport("user32.dll")]
    private static unsafe extern int SetDisplayConfig(uint np, DISPLAYCONFIG_PATH_INFO* p,
        uint nm, DISPLAYCONFIG_MODE_INFO* m, uint flags);

    private const uint QDC_ALL_PATHS = 1;
    private const uint SDC_APPLY = 0x80;
    private const uint SDC_TOPOLOGY = 0x10;
    private const uint SDC_USE_SUPPLIED = 0x20;
    private const uint SDC_ALLOW = 0x400;
    private const uint SDC_FORCE = 0x1000;
    private const uint SDC_VM = 0x8000;
    private const uint SDC_SAVE = 0x200;

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO { public DISPLAYCONFIG_PATH_SOURCE_INFO si; public DISPLAYCONFIG_PATH_TARGET_INFO ti; public uint flags; }
    [StructLayout(LayoutKind.Explicit)]
    private struct DISPLAYCONFIG_PATH_SOURCE_INFO { [FieldOffset(0)] public long adapterId; [FieldOffset(8)] public uint id; [FieldOffset(12)] public uint modeInfoIdx; [FieldOffset(16)] public uint statusFlags; }
    [StructLayout(LayoutKind.Explicit)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO { [FieldOffset(0)] public long adapterId; [FieldOffset(8)] public uint id; [FieldOffset(12)] public uint modeInfoIdx; [FieldOffset(16)] public uint outputTechnology; [FieldOffset(20)] public uint rotation; [FieldOffset(24)] public uint scaling; [FieldOffset(28)] public DISPLAYCONFIG_RATIONAL refreshRate; [FieldOffset(36)] public uint scanlineOrdering; [FieldOffset(40)] public int targetAvailable; [FieldOffset(44)] public uint statusFlags; }
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_RATIONAL { public uint Numerator; public uint Denominator; }
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_MODE_INFO { public uint infoType; public uint id; public long adapterId; public DISPLAYCONFIG_MODE_INFO_UNION modeInfo; }
    [StructLayout(LayoutKind.Explicit)]
    private struct DISPLAYCONFIG_MODE_INFO_UNION { [FieldOffset(0)] public DISPLAYCONFIG_SOURCE_MODE sourceMode; [FieldOffset(0)] public DISPLAYCONFIG_TARGET_MODE targetMode; }
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_SOURCE_MODE { public uint width; public uint height; public int pixelFormat; public DISPLAYCONFIG_POINT position; }
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_POINT { public int x; public int y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_TARGET_MODE { public long pixelRate; public DISPLAYCONFIG_RATIONAL hSyncFreq; public DISPLAYCONFIG_RATIONAL vSyncFreq; public DISPLAYCONFIG_2DREGION activeSize; public DISPLAYCONFIG_2DREGION totalSize; public uint videoStandard; public uint scanlineOrdering; }
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_2DREGION { public uint cx; public uint cy; }

    private static unsafe int Call(string label, uint np, DISPLAYCONFIG_PATH_INFO* p, uint nm, DISPLAYCONFIG_MODE_INFO* m, uint flags)
    {
        int hr = SetDisplayConfig(np, p, nm, m, flags);
        Console.WriteLine(label + ": 0x" + flags.ToString("X8") + " -> " + hr);
        return hr;
    }

    public static unsafe int TestAll()
    {
        uint oldStyle = SDC_TOPOLOGY | SDC_USE_SUPPLIED | SDC_FORCE | SDC_VM | SDC_ALLOW | 4;
        Call("OldStyle", 0, null, 0, null, oldStyle);
        Call("Apply+Topo", 0, null, 0, null, SDC_APPLY | SDC_TOPOLOGY | 4);
        Call("ApplyOnly", 0, null, 0, null, SDC_APPLY);

        uint qflags = 1;
        int hr = GetDisplayConfigBufferSizes(qflags, out var np, out var nm);
        if (hr != 0) return hr;
        var pa = new DISPLAYCONFIG_PATH_INFO[np];
        var ma = new DISPLAYCONFIG_MODE_INFO[nm];
        fixed (DISPLAYCONFIG_PATH_INFO* pp = pa) fixed (DISPLAYCONFIG_MODE_INFO* mm = ma)
        {
            hr = QueryDisplayConfig(qflags, ref np, pp, ref nm, mm, IntPtr.Zero);
            if (hr != 0) return hr;
        }
        Array.Resize(ref pa, (int)np); Array.Resize(ref ma, (int)nm);

        var activePaths = pa.Where(p => (p.flags & 1u) != 0).ToList();
        if (activePaths.Count > 0)
        {
            Console.WriteLine("Re-applying " + activePaths.Count + " active paths...");
            var ap = activePaths.ToArray();
            var allUsed = new HashSet<uint>();
            foreach (var p in ap) { allUsed.Add(p.si.modeInfoIdx); allUsed.Add(p.ti.modeInfoIdx); }
            var am = ma.Where((m, i) => allUsed.Contains((uint)i)).ToArray();
            fixed (DISPLAYCONFIG_PATH_INFO* ppp = ap) fixed (DISPLAYCONFIG_MODE_INFO* mmm = am)
            {
                Call("Reapply", (uint)ap.Length, ppp, (uint)am.Length, mmm, SDC_APPLY | SDC_USE_SUPPLIED | SDC_ALLOW);
            }
        }
        return 0;
    }
}
'@

    try {
        Add-Type -TypeDefinition $csharp -Language CSharp -CompilerOptions "/unsafe" -ErrorAction Stop
        [DisplayExtender]::TestAll()
    } catch {
        $src = "$env:TEMP\MonitorPowerTest.cs"
        $dll = "$env:TEMP\MonitorPowerTest.dll"
        Set-Content -Path $src -Value $csharp -Encoding UTF8
        $csc = @("C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
                 "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe") |
               Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $csc) { Write-Error "csc.exe non trovato."; return $false }
        & $csc /unsafe /target:library /out:$dll $src 2>&1 | Write-Host
        if (-not (Test-Path $dll)) { Write-Error "Compilazione fallita."; return $false }
        Add-Type -Path $dll
        [DisplayExtender]::TestAll()
    }
}

# ─── main ───────────────────────────────────────────────────

if (!(Test-Path $ConfigDir)) { New-Item -ItemType Directory -Path $ConfigDir -Force | Out-Null }

switch ($Command) {
    "extend"  { Invoke-ExtendAll }
    "save"    { Save-LayoutToJson $File }
    "restore" { Restore-LayoutFromJson $File }
    "test"    { Invoke-TestFlags }
}
