$csharpCode = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

public static class DisplaySnapshot
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, uint iModeNum, ref DEVMODE lpDevMode);

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

    public class SnapshotEntry
    {
        public string DeviceName { get; set; }
        public bool IsPrimary { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int DisplayFrequency { get; set; }
        public uint DisplayOrientation { get; set; }
        public int BitsPerPel { get; set; }
    }

    public static void Run()
    {
        string snapshotDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MonitorPowerExtension");
        Directory.CreateDirectory(snapshotDir);
        string path = Path.Combine(snapshotDir, "snapshot.json");

        var entries = new List<SnapshotEntry>();

        for (uint i = 0; ; i++)
        {
            var dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf<DISPLAY_DEVICE>();
            if (!EnumDisplayDevices(null, i, ref dd, 0))
                break;

            var dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            bool ok = EnumDisplaySettings(dd.DeviceName, 0xFFFFFFFE, ref dm);
            if (!ok)
            {
                dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
                ok = EnumDisplaySettings(dd.DeviceName, 0xFFFFFFFF, ref dm);
            }

            if (!ok || dm.dmPelsWidth == 0 || dm.dmPelsHeight == 0)
            {
                Console.WriteLine($"Skipping {dd.DeviceName} (no valid mode)");
                continue;
            }

            bool isPrimary = (dd.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0;
            var entry = new SnapshotEntry
            {
                DeviceName = dd.DeviceName,
                IsPrimary = isPrimary,
                Width = dm.dmPelsWidth,
                Height = dm.dmPelsHeight,
                PositionX = dm.dmPositionX,
                PositionY = dm.dmPositionY,
                DisplayFrequency = dm.dmDisplayFrequency,
                DisplayOrientation = dm.dmDisplayOrientation,
                BitsPerPel = dm.dmBitsPerPel
            };
            entries.Add(entry);

            Console.WriteLine($"  {dd.DeviceName}: {entry.Width}x{entry.Height} @{entry.DisplayFrequency}Hz orient={entry.DisplayOrientation} pos=({entry.PositionX},{entry.PositionY}) primary={entry.IsPrimary}");
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(entries, options);
        File.WriteAllText(path, json);
        Console.WriteLine($"\nSnapshot salvato in: {path}");
    }
}
'@

Add-Type -TypeDefinition $csharpCode -Language CSharp -ErrorAction Stop
[DisplaySnapshot]::Run()
