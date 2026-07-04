# Create-Profiles2.ps1
# Crea profili unendo target ID (da QueryDisplayConfig) + layout (da display-layout.json)

$profilesDir = "$env:LOCALAPPDATA\MonitorPowerExtension\profiles"
$layoutPath = "C:\Users\Rhaegal222\GitHub\PowerToys\display-layout.json"
if (!(Test-Path $profilesDir)) { New-Item -ItemType Directory -Path $profilesDir -Force | Out-Null }

# Layout di riferimento con posizioni/orientamenti corretti
$layoutRef = Get-Content $layoutPath -Raw | ConvertFrom-Json

$csharp = @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class TargetProbe
{
    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint np, out uint nm);
    [DllImport("user32.dll")]
    private static unsafe extern int QueryDisplayConfig(uint flags, ref uint np, DISPLAYCONFIG_PATH_INFO* p, ref uint nm, DISPLAYCONFIG_MODE_INFO* m, IntPtr topo);
    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(IntPtr p);

    private const uint QDC_ONLY_ACTIVE_PATHS = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID { public uint LowPart; public int HighPart; }

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
    public struct DISPLAYCONFIG_DEVICE_INFO_HEADER { public int type; public uint size; public LUID adapterId; public uint id; }

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
            uint tid = p.ti.id;
            string gdiName = "";

            var srcName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
            {
                header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    type = 1, size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME)),
                    adapterId = p.si.adapterId, id = p.si.id
                }
            };
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME)));
            try
            {
                Marshal.StructureToPtr(srcName, ptr, false);
                if (DisplayConfigGetDeviceInfo(ptr) == 0)
                {
                    srcName = (DISPLAYCONFIG_SOURCE_DEVICE_NAME)Marshal.PtrToStructure(ptr, typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME));
                    gdiName = srcName.viewGdiDeviceName ?? "";
                }
            }
            finally { Marshal.FreeHGlobal(ptr); }

            entries.Add("{\"LowPart\":" + p.ti.adapterId.LowPart
                + ",\"HighPart\":" + p.ti.adapterId.HighPart
                + ",\"TargetId\":" + tid
                + ",\"DeviceName\":\"" + gdiName.Replace("\\", "\\\\") + "\"}");
        }
        return "[" + string.Join(",", entries.ToArray()) + "]";
    }
}
'@

$src = "$env:TEMP\TargetProbe.cs"
$dll = "$env:TEMP\TargetProbe.dll"
Set-Content -Path $src -Value $csharp -Encoding UTF8
$csc = @("C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
         "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe") |
       Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) { Write-Error "csc.exe non trovato."; exit 1 }
& $csc /unsafe /target:library /out:$dll $src 2>&1 | Out-Null
if (!(Test-Path $dll)) { Write-Error "Compilazione fallita."; exit 1 }
Add-Type -Path $dll

# Ottieni la mappa target → device name
$mappingJson = [TargetProbe]::GetMappingJson()
$mapping = $mappingJson | ConvertFrom-Json

Write-Host "=== Target attivi ===" -ForegroundColor Cyan
$mapping | ForEach-Object { 
    Write-Host "  TargetId=$($_.TargetId) -> $($_.DeviceName)" 
}

# Unisci con layout di riferimento
$merged = @()
foreach ($t in $mapping) {
    $ref = $layoutRef.targets | Where-Object { $_.DeviceName -eq $t.DeviceName }
    if ($ref) {
        $merged += [PSCustomObject]@{
            LowPart = $t.LowPart
            HighPart = $t.HighPart
            TargetId = $t.TargetId
            DeviceName = $t.DeviceName
            Width = $ref.Width
            Height = $ref.Height
            Frequency = $ref.Frequency
            PositionX = $ref.PositionX
            PositionY = $ref.PositionY
            Orientation = $ref.Orientation
            BitsPerPel = $ref.BitsPerPel
            IsPrimary = $ref.IsPrimary
        }
    }
}

Write-Host "`n=== Mappatura completa ===" -ForegroundColor Cyan
$merged | ForEach-Object {
    Write-Host "[$($_.TargetId)] $($_.DeviceName) $($_.Width)x$($_.Height) @ ($($_.PositionX),$($_.PositionY)) O=$($_.Orientation) P=$($_.IsPrimary)"
}

# Funzione per creare profilo
function New-Profile($name, $targetIds) {
    $selected = $merged | Where-Object { $_.TargetId -in $targetIds }
    $targets = @($selected | ForEach-Object {
        @{ AdapterId = @{ LowPart = $_.LowPart; HighPart = $_.HighPart }; TargetId = $_.TargetId }
    })
    $layoutEntries = @($selected | ForEach-Object {
        $isPrimary = if ($targetIds.Count -eq 1) { $true } else { $_.IsPrimary }
        @{
            LowPart = $_.LowPart; HighPart = $_.HighPart; TargetId = $_.TargetId
            DeviceName = $_.DeviceName; Width = $_.Width; Height = $_.Height
            Frequency = $_.Frequency; PositionX = $_.PositionX; PositionY = $_.PositionY
            Orientation = $_.Orientation; BitsPerPel = $_.BitsPerPel; IsPrimary = $isPrimary
        }
    })
    $profile = @{
        Name = $name
        Targets = $targets
        Layout = $layoutEntries
    }
    $safeName = $name -replace '[<>:"/\\|?*]', ''
    $path = Join-Path $profilesDir "$safeName.json"
    $profile | ConvertTo-Json -Depth 3 | Out-File -FilePath $path -Encoding UTF8
    Write-Host "Creato: $path" -ForegroundColor Green
    return $path
}

# Identifica monitor per ruolo
$tv = $merged | Where-Object { $_.Width -eq 3840 -and $_.Height -eq 2160 } | Select-Object -First 1
$centrale = $merged | Where-Object { $_.Width -eq 3440 -and $_.Height -eq 1440 } | Select-Object -First 1
$sinistra = $merged | Where-Object { $_.PositionX -lt 0 -and $_.Width -eq 1080 } | Select-Object -First 1
$destra = $merged | Where-Object { $_.PositionX -gt 0 -and $_.Width -eq 1080 } | Select-Object -First 1

Write-Host "`n=== Monitor identifikati ===" -ForegroundColor Cyan
Write-Host "TV  (4K): TargetId=$($tv.TargetId) $($tv.DeviceName)"
Write-Host "Centrale:  TargetId=$($centrale.TargetId) $($centrale.DeviceName)"
Write-Host "Sinistra:  TargetId=$($sinistra.TargetId) $($sinistra.DeviceName)"
Write-Host "Destra:    TargetId=$($destra.TargetId) $($destra.DeviceName)"

# Crea profili
Write-Host "`n=== Creazione profili ===" -ForegroundColor Cyan
New-Profile "Passa a TV" @($tv.TargetId)
New-Profile "Centrale + TV" @($centrale.TargetId, $tv.TargetId)
New-Profile "Centrale + Sinistra" @($centrale.TargetId, $sinistra.TargetId)
New-Profile "Centrale + Destra" @($centrale.TargetId, $destra.TargetId)

Write-Host "`nFATTO." -ForegroundColor Green
