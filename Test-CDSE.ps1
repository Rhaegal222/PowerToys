$csharpCode = @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class TestCDSE
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, uint iModeNum, ref DEVMODE lpDevMode);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwFlags, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int ChangeDisplaySettingsEx(IntPtr lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwFlags, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private const int SM_CMONITORS = 80;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    public const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x04;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
    }

    private const uint DM_POSITION = 0x20;
    private const uint DM_PELSWIDTH = 0x80000;
    private const uint DM_PELSHEIGHT = 0x100000;
    private const uint DM_DISPLAYFREQUENCY = 0x400000;
    private const uint DM_BITSPERPEL = 0x40000;
    private const uint DM_DISPLAYORIENTATION = 0x80;

    private const uint CDS_UPDATEREGISTRY = 0x00000001;
    private const uint CDS_GLOBAL = 0x00000008;

    private static string DispResult(int r) => r switch
    {
        0 => "SUCCESS",
        -1 => "FAILED",
        -2 => "BADMODE",
        -3 => "NOTUPDATED",
        -4 => "BADFLAGS",
        -5 => "BADPARAM",
        -6 => "BADDUALVIEW",
        1 => "RESTART",
        _ => $"UNKNOWN({r})"
    };

    public static void Run()
    {
        int before = GetSystemMetrics(SM_CMONITORS);
        Console.WriteLine($"Monitors before: {before}");

        // Enumerate devices
        var deviceNames = new List<(string name, uint flags)>();
        for (uint i = 0; ; i++)
        {
            var dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf<DISPLAY_DEVICE>();
            if (!EnumDisplayDevices(null, i, ref dd, 0))
                break;
            deviceNames.Add((dd.DeviceName, dd.StateFlags));
        }

        // Per ogni monitor: leggi posizione/orientazione correnti + trova miglior refresh rate
        var configs = new List<(string name, DEVMODE dm, bool isPrimary)>();

        foreach (var (name, flags) in deviceNames)
        {
            // 1) Leggi le impostazioni correnti dal registro (contengono posizione, orientazione)
            var cur = new DEVMODE();
            cur.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            bool hasCur = EnumDisplaySettings(name, 0xFFFFFFFE, ref cur);
            if (!hasCur)
            {
                cur = new DEVMODE();
                cur.dmSize = (short)Marshal.SizeOf<DEVMODE>();
                hasCur = EnumDisplaySettings(name, 0xFFFFFFFF, ref cur);
            }

            if (!hasCur || cur.dmPelsWidth == 0 || cur.dmPelsHeight == 0)
            {
                Console.WriteLine($"'{name}': no valid mode, skipping");
                continue;
            }

            bool isPrimary = (flags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0;

            // 2) Cerca la migliore frequenza per questa risoluzione (stessa risoluzione, max refresh)
            int bestFreq = cur.dmDisplayFrequency;
            DEVMODE bestExtra = cur; // copia per tenere i campi extra

            for (uint m = 0; ; m++)
            {
                var dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
                if (!EnumDisplaySettings(name, m, ref dm))
                    break;

                if (dm.dmPelsWidth == cur.dmPelsWidth && dm.dmPelsHeight == cur.dmPelsHeight &&
                    dm.dmDisplayFrequency > bestFreq)
                {
                    bestExtra = dm;
                    bestFreq = dm.dmDisplayFrequency;
                }
            }

            // 3) Combina: posizione/orientazione da 'cur', frequenza/risoluzione da 'bestExtra'
            var applyDm = cur;
            applyDm.dmPelsWidth = bestExtra.dmPelsWidth;
            applyDm.dmPelsHeight = bestExtra.dmPelsHeight;
            applyDm.dmDisplayFrequency = bestExtra.dmDisplayFrequency;
            applyDm.dmBitsPerPel = bestExtra.dmBitsPerPel;
            applyDm.dmDisplayOrientation = cur.dmDisplayOrientation;
            applyDm.dmFields = DM_POSITION | DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY | DM_BITSPERPEL | DM_DISPLAYORIENTATION;

            Console.WriteLine($"'{name}' primary={isPrimary}: {applyDm.dmPelsWidth}x{applyDm.dmPelsHeight} @{applyDm.dmDisplayFrequency}Hz orient={applyDm.dmDisplayOrientation} pos=({applyDm.dmPositionX},{applyDm.dmPositionY})");

            configs.Add((name, applyDm, isPrimary));
        }

        // Applica a tutti i monitor non-primary
        Console.WriteLine("\n=== Applying via ChangeDisplaySettingsEx ===");
        int ok = 0, fail = 0;
        foreach (var (name, dm, isPrimary) in configs)
        {
            if (isPrimary) continue;

            var d2 = dm;
            int r = ChangeDisplaySettingsEx(name, ref d2, IntPtr.Zero, CDS_UPDATEREGISTRY | CDS_GLOBAL, IntPtr.Zero);
            Console.WriteLine($"  {name}: {d2.dmPelsWidth}x{d2.dmPelsHeight} @{d2.dmDisplayFrequency}Hz pos=({d2.dmPositionX},{d2.dmPositionY}) -> {DispResult(r)}");
            if (r == 0) ok++; else fail++;
        }

        int after = GetSystemMetrics(SM_CMONITORS);
        Console.WriteLine($"\nApplied: {ok} ok, {fail} failed");
        Console.WriteLine($"Monitors after: {after}");

        if (ok > 0 && fail == 0)
            Console.WriteLine("\n*** SUCCESSO! Monitor riattivati con frequenze aggiornate ***");
    }
}
'@

Add-Type -TypeDefinition $csharpCode -Language CSharp -ErrorAction Stop
[TestCDSE]::Run()
