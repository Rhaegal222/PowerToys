# Create-Profiles.ps1
# Crea profili MonitorPowerExtension dal layout corrente

$profilesDir = "$env:LOCALAPPDATA\MonitorPowerExtension\profiles"
$layoutPath = "C:\Users\Rhaegal222\GitHub\PowerToys\display-layout.json"
if (!(Test-Path $profilesDir)) { New-Item -ItemType Directory -Path $profilesDir -Force | Out-Null }

# Leggi layout di riferimento
$layout = Get-Content $layoutPath -Raw | ConvertFrom-Json

$csharp = @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class DisplayProbe
{
    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint np, out uint nm);
    [DllImport("user32.dll")]
    private static unsafe extern int QueryDisplayConfig(uint flags, ref uint np, DISPLAYCONFIG_PATH_INFO* p, ref uint nm, DISPLAYCONFIG_MODE_INFO* m, IntPtr topo);
    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(IntPtr p);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int EnumDisplaySettings(string n, uint i, ref DEVMODE m);

    private const uint QDC_ONLY_ACTIVE_PATHS = 2;
    private const uint QDC_ALL_PATHS = 1;
    private const uint QDC_VIRTUAL_MODE_AWARE = 0x10;

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID { public uint LowPart; public int HighPart; public override string ToString() { return HighPart.ToString("X8") + LowPart.ToString("X8"); } }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_INFO { public DISPLAYCONFIG_PATH_SOURCE_INFO si; public DISPLAYCONFIG_PATH_TARGET_INFO ti; public uint flags; }
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_SOURCE_INFO { public LUID adapterId; public uint id; public uint modeInfoIdx; public uint statusFlags; }
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_TARGET_INFO { public LUID adapterId; public uint id; public uint modeInfoIdx; public uint outputTechnology; public uint rotation; public uint scaling; public DISPLAYCONFIG_RATIONAL refreshRate; public uint scanlineOrdering; public int targetAvailable; public uint statusFlags; }
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_RATIONAL { public uint Numerator; public uint Denominator; }
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_MODE_INFO { public uint infoType; public uint id; public LUID adapterId; public DISPLAYCONFIG_MODE_INFO_UNION modeInfo; }
    [StructLayout(LayoutKind.Explicit)]
    public struct DISPLAYCONFIG_MODE_INFO_UNION { [FieldOffset(0)] public DISPLAYCONFIG_TARGET_MODE targetMode; [FieldOffset(0)] public DISPLAYCONFIG_SOURCE_MODE sourceMode; }
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_TARGET_MODE { public long pixelRate; public DISPLAYCONFIG_RATIONAL hSyncFreq; public DISPLAYCONFIG_RATIONAL vSyncFreq; public DISPLAYCONFIG_2DREGION activeSize; public DISPLAYCONFIG_2DREGION totalSize; public uint videoStandard; public uint scanlineOrdering; }
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_SOURCE_MODE { public uint width; public uint height; public int pixelFormat; public DISPLAYCONFIG_POINT position; }
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_POINT { public int x; public int y; }
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_2DREGION { public uint cx; public uint cy; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string viewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_DEVICE_INFO_HEADER { public DISPLAYCONFIG_DEVICE_INFO_TYPE type; public uint size; public LUID adapterId; public uint id; }
    public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : uint { GetSourceName = 1, GetTargetName = 2 }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion; public short dmDriverVersion; public short dmSize; public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX; public int dmPositionY; public int dmDisplayOrientation;
        public int dmDisplayFixedOutput; public short dmColor; public short dmDuplex; public short dmYResolution;
        public short dmTTOption; public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName; public short dmLogPixels; public int dmBitsPerPel;
        public int dmPelsWidth; public int dmPelsHeight; public int dmDisplayFlags; public int dmDisplayFrequency;
        public int dmICMMethod; public int dmICMIntent; public int dmMediaType; public int dmDitherType;
        public int dmReserved1; public int dmReserved2; public int dmPanningWidth; public int dmPanningHeight;
    }

    private const uint ENUM_CURRENT_SETTINGS = 0xFFFFFFFF;

    public static string GetMappingJson()
    {
        uint np, nm;
        int hr = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out np, out nm);
        if (hr != 0) return "[]";

        var pa = new DISPLAYCONFIG_PATH_INFO[np];
        var ma = new DISPLAYCONFIG_MODE_INFO[nm];
        unsafe
        {
            fixed (DISPLAYCONFIG_PATH_INFO* pp = pa) fixed (DISPLAYCONFIG_MODE_INFO* mm = ma)
            {
                hr = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref np, pp, ref nm, mm, IntPtr.Zero);
                if (hr != 0) return "[]";
            }
        }
        Array.Resize(ref pa, (int)np);

        var entries = new List<string>();
        for (int i = 0; i < pa.Length; i++)
        {
            var p = pa[i];
            string adapter = p.ti.adapterId.ToString();
            uint tid = p.ti.id;
            uint sid = p.si.id;

            string gdiName = "";
            var srcHeader = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DISPLAYCONFIG_DEVICE_INFO_TYPE.GetSourceName,
                size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME)),
                adapterId = p.si.adapterId,
                id = sid
            };
            var srcName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME { header = srcHeader };
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME)));
            try
            {
                Marshal.StructureToPtr(srcName, ptr, false);
                if (DisplayConfigGetDeviceInfo(ptr) == 0)
                {
                    srcName = (DISPLAYCONFIG_SOURCE_DEVICE_NAME)Marshal.PtrToStructure(ptr, typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME));
                    gdiName = srcName.viewGdiDeviceName;
                }
            }
            finally { Marshal.FreeHGlobal(ptr); }

            // Get current position via EnumDisplaySettings
            var dm = default(DEVMODE);
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            EnumDisplaySettings(gdiName, ENUM_CURRENT_SETTINGS, ref dm);

            bool primary = dm.dmPositionX == 0 && dm.dmPositionY == 0;

            entries.Add("{\"Adapter\":\"" + adapter + "\",\"LowPart\":" + p.ti.adapterId.LowPart
                + ",\"HighPart\":" + p.ti.adapterId.HighPart
                + ",\"TargetId\":" + tid
                + ",\"DeviceName\":\"" + gdiName.Replace("\\", "\\\\") + "\""
                + ",\"Width\":" + dm.dmPelsWidth
                + ",\"Height\":" + dm.dmPelsHeight
                + ",\"PositionX\":" + dm.dmPositionX
                + ",\"PositionY\":" + dm.dmPositionY
                + ",\"Orientation\":" + dm.dmDisplayOrientation
                + ",\"BitsPerPel\":" + dm.dmBitsPerPel
                + ",\"Frequency\":" + dm.dmDisplayFrequency
                + ",\"IsPrimary\":" + (primary ? "true" : "false") + "}");
        }
        return "[" + string.Join(",", entries.ToArray()) + "]";
    }
}
'@

# Compila con csc.exe
$src = "$env:TEMP\DisplayProbe.cs"
$dll = "$env:TEMP\DisplayProbe.dll"
Set-Content -Path $src -Value $csharp -Encoding UTF8
$csc = @("C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
         "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe") |
       Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) { Write-Error "csc.exe non trovato."; exit 1 }
& $csc /unsafe /target:library /out:$dll $src 2>&1 | Out-Null
if (!(Test-Path $dll)) { Write-Error "Compilazione fallita."; exit 1 }
Add-Type -Path $dll

$mappingJson = [DisplayProbe]::GetMappingJson()
$mapping = $mappingJson | ConvertFrom-Json

Write-Host "=== Mappatura target attuali ===" -ForegroundColor Cyan
$mapping | ForEach-Object {
    Write-Host "[$($_.TargetId)] $($_.DeviceName) -> $($_.Width)x$($_.Height) @ ($($_.PositionX),$($_.PositionY)) Orient=$($_.Orientation)"
}

# Funzione per creare un profilo
function New-Profile($name, $targetIds) {
    $selectedLayout = $mapping | Where-Object { $_.TargetId -in $targetIds }
    $targets = @($selectedLayout | ForEach-Object {
        @{ "AdapterId" = @{ "LowPart" = $_.LowPart; "HighPart" = $_.HighPart }; "TargetId" = $_.TargetId }
    })
    $layoutEntries = @($selectedLayout | ForEach-Object {
        $isPrimary = if ($targetIds.Count -eq 1) { $true } else { $_.IsPrimary }
        @{
            "LowPart" = $_.LowPart; "HighPart" = $_.HighPart; "TargetId" = $_.TargetId
            "DeviceName" = $_.DeviceName; "Width" = $_.Width; "Height" = $_.Height
            "Frequency" = $_.Frequency; "PositionX" = $_.PositionX; "PositionY" = $_.PositionY
            "Orientation" = $_.Orientation; "BitsPerPel" = $_.BitsPerPel; "IsPrimary" = $isPrimary
        }
    })
    $profile = @{
        "Name" = $name
        "Targets" = $targets
        "Layout" = $layoutEntries
    }
    $safeName = $name -replace '[<>:"/\\|?*]', ''
    $path = Join-Path $profilesDir "$safeName.json"
    $profile | ConvertTo-Json -Depth 3 | Out-File -FilePath $path -Encoding UTF8
    Write-Host "Creato: $path" -ForegroundColor Green
    return $path
}

# Trova i target ID per nome descrittivo
$tv = $mapping | Where-Object { $_.Width -eq 3840 -and $_.Height -eq 2160 }
$centrale = $mapping | Where-Object { $_.Width -eq 3440 -and $_.Height -eq 1440 }
$sinistra = $mapping | Where-Object { $_.PositionX -lt 0 -and $_.Width -eq 1080 -and $_.Orientation -eq 1 }
$destra = $mapping | Where-Object { $_.PositionX -gt 0 -and $_.Width -eq 1080 -and $_.Orientation -eq 3 }

Write-Host "`n=== Identificazione monitor ===" -ForegroundColor Cyan
Write-Host "TV (4K): TargetId=$($tv.TargetId)" -ForegroundColor Yellow
Write-Host "Centrale: TargetId=$($centrale.TargetId)" -ForegroundColor Yellow
Write-Host "Sinistra: TargetId=$($sinistra.TargetId)" -ForegroundColor Yellow
Write-Host "Destra: TargetId=$($destra.TargetId)" -ForegroundColor Yellow

# Crea profili
Write-Host "`n=== Creazione profili ===" -ForegroundColor Cyan
New-Profile "Passa a TV" @($tv.TargetId)
New-Profile "Centrale + TV" @($centrale.TargetId, $tv.TargetId)
New-Profile "Centrale + Sinistra" @($centrale.TargetId, $sinistra.TargetId)
New-Profile "Centrale + Destra" @($centrale.TargetId, $destra.TargetId)

Write-Host "`nFATTO. 4 profili creati." -ForegroundColor Green
