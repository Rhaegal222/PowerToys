// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

#pragma warning disable IL2026, IL3050

#pragma warning disable SA1300, SA1307, SA1310, SA1316, SA1649, CA1069, CA1305, CA1863

namespace MonitorPowerExtension;

internal enum DISPLAYCONFIG_TOPOLOGY_ID : uint
{
    Zero = 0,
    Internal = 1,
    Clone = 2,
    Extend = 4,
    External = 8,
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_INFO
{
    public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
    public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_SOURCE_INFO
{
    public LUID adapterId;
    public uint id;
    public uint modeInfoIdx;
    public uint statusFlags;

    // Virtual-mode-aware sub-fields of modeInfoIdx
    public readonly ushort cloneGroupId => (ushort)(modeInfoIdx & 0xFFFF);

    public readonly ushort sourceModeInfoIdx => (ushort)(modeInfoIdx >> 16);
}

[StructLayout(LayoutKind.Sequential)]
internal struct LUID
{
    public uint LowPart { get; set; }

    public int HighPart { get; set; }

    public override readonly string ToString() => $"{HighPart:X8}{LowPart:X8}";
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_TARGET_INFO
{
    public LUID adapterId;
    public uint id;
    public uint modeInfoIdx;
    public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
    public DISPLAYCONFIG_ROTATION rotation;
    public DISPLAYCONFIG_SCALING scaling;
    public DISPLAYCONFIG_RATIONAL refreshRate;
    public DISPLAYCONFIG_SCANLINE_ORDERING scanlineOrdering;
    public int targetAvailable;
    public uint statusFlags;

    // Virtual-mode-aware sub-fields of modeInfoIdx
    public readonly ushort desktopModeInfoIdx => (ushort)(modeInfoIdx & 0xFFFF);

    public readonly ushort targetModeInfoIdx => (ushort)(modeInfoIdx >> 16);
}

internal enum DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY : uint
{
    Other = unchecked((uint)-1),
    VGA = 0,
    SVideo = 1,
    CompositeVideo = 2,
    ComponentVideo = 3,
    DVI = 4,
    HDMI = 5,
    LVDS = 6,
    D_JPN = 8,
    SDI = 9,
    DisplayPortExternal = 10,
    DisplayPortEmbedded = 11,
    UDI = 12,
    DConnector = 13,
    IndirectWired = 14,
    IndirectVirtual = 15,
    Internal = 0x80000000,
}

internal enum DISPLAYCONFIG_ROTATION : uint
{
    Identity = 1,
    Rotate90 = 2,
    Rotate180 = 3,
    Rotate270 = 4,
}

internal enum DISPLAYCONFIG_SCALING : uint
{
    Identity = 1,
    Centered = 2,
    Stretched = 3,
    AspectRatioCenteredMax = 4,
    Custom = 5,
    Preferred = 128,
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_RATIONAL
{
    public uint Numerator;
    public uint Denominator;
}

internal enum DISPLAYCONFIG_SCANLINE_ORDERING : uint
{
    Unspecified = 0,
    Progressive = 1,
    Interlaced = 2,
    InterlacedUpperFieldFirst = Interlaced,
    InterlacedLowerFieldFirst = 3,
    ForceUInt32 = 0xFFFFFFFF,
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_MODE_INFO
{
    public DISPLAYCONFIG_MODE_INFO_TYPE infoType;
    public uint id;
    public LUID adapterId;
    public DISPLAYCONFIG_MODE_INFO_UNION modeInfo;
}

[StructLayout(LayoutKind.Explicit)]
internal struct DISPLAYCONFIG_MODE_INFO_UNION
{
    [FieldOffset(0)]
    public DISPLAYCONFIG_TARGET_MODE targetMode;

    [FieldOffset(0)]
    public DISPLAYCONFIG_SOURCE_MODE sourceMode;

    [FieldOffset(0)]
    public DISPLAYCONFIG_DESKTOP_IMAGE_INFO desktopImageInfo;
}

internal enum DISPLAYCONFIG_MODE_INFO_TYPE : uint
{
    Zero = 0,
    Source = 1,
    Target = 2,
    DesktopImage = 3,
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_TARGET_MODE
{
    public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
{
    public long pixelRate;
    public DISPLAYCONFIG_RATIONAL hSyncFreq;
    public DISPLAYCONFIG_RATIONAL vSyncFreq;
    public DISPLAYCONFIG_2DREGION activeSize;
    public DISPLAYCONFIG_2DREGION totalSize;
    public uint videoStandard;
    public DISPLAYCONFIG_SCANLINE_ORDERING scanlineOrdering;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_2DREGION
{
    public uint cx;
    public uint cy;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_SOURCE_MODE
{
    public uint width;
    public uint height;
    public int pixelFormat;
    public DISPLAYCONFIG_POINT position;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_POINT
{
    public int x;
    public int y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO
{
    public DISPLAYCONFIG_POINT PathSourceSize;
    public DISPLAYCONFIG_RATIONAL DesktopImageRegion;
    public DISPLAYCONFIG_RATIONAL DesktopImageClip;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_DEVICE_INFO_HEADER
{
    public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
    public uint size;
    public LUID adapterId;
    public uint id;
}

internal enum DISPLAYCONFIG_DEVICE_INFO_TYPE : uint
{
    GetSourceName = 1,
    GetTargetName = 2,
    GetTargetPreferredMode = 3,
    GetAdapterName = 4,
    SetTargetPersistence = 5,
    GetTargetBaseType = 6,
    GetSupportVirtualResolution = 7,
    SetSupportVirtualResolution = 8,
    GetAdvancedColorInfo = 9,
    SetAdvancedColorState = 10,
    GetSdrWhiteLevel = 11,
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAYCONFIG_TARGET_DEVICE_NAME
{
    public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
    public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
    public ushort edidManufactureId;
    public ushort edidProductCodeId;
    public uint connectorInstance;
    public fixed ushort monitorFriendlyDeviceName[64];
    public fixed ushort monitorDevicePath[128];

    public readonly string GetMonitorFriendlyDeviceName()
    {
        fixed (ushort* ptr = monitorFriendlyDeviceName)
        {
            return new string((char*)ptr);
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS
{
    public uint value;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
{
    public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    public fixed ushort viewGdiDeviceName[32];

    public readonly string GetViewGdiDeviceName()
    {
        fixed (ushort* ptr = viewGdiDeviceName)
        {
            return new string((char*)ptr);
        }
    }
}

internal static partial class DisplayHelpers
{
    private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
    private const uint QDC_ALL_PATHS = 0x00000001;
    private const uint QDC_DATABASE_CURRENT = 0x00000004;
    private const uint QDC_VIRTUAL_MODE_AWARE = 0x00000010;
    private const uint SDC_APPLY = 0x00000080;
    private const uint SDC_SAVE_TO_DATABASE = 0x00000200;
    private const uint SDC_TOPOLOGY_SUPPLIED = 0x00000010;
    private const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
    private const uint SDC_ALLOW_CHANGES = 0x00000400;
    private const uint SDC_VIRTUAL_MODE_AWARE = 0x00008000;

    private static readonly object StateLock = new();
    private static (DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes)? _savedState;
    private static KeyboardHook? _activeHook;
    private static Dictionary<DisplayTargetId, string>? _displayNameCache;
    private static DisplayTargetId[]? _cachedTargetOrder;
    private static Timer? _xboxPollTimer;
    private static bool _guideWasPressed;

    public static event Action? GuideViewComboPressed;

    public static event Action? DisplayProfileApplyStarted;

    public static event Action? DisplayProfileApplyCompleted;

    private const ushort XinputGamepadBack = 0x0020;
    private const ushort XinputGamepadGuide = 0x0400;
    private const int XinputMaxControllers = 4;

    internal const DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY INTERNAL_TECH = (DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY)0x80000000;

    internal readonly record struct DisplayTargetId(LUID AdapterId, uint TargetId) : IComparable<DisplayTargetId>
    {
        public int CompareTo(DisplayTargetId other)
        {
            var c = AdapterId.HighPart.CompareTo(other.AdapterId.HighPart);
            return c != 0 ? c : AdapterId.LowPart.CompareTo(other.AdapterId.LowPart);
        }
    }

    [LibraryImport("user32.dll")]
    private static partial int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [LibraryImport("user32.dll")]
    private static unsafe partial int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        DISPLAYCONFIG_PATH_INFO* pathArray,
        ref uint numModeInfoArrayElements,
        DISPLAYCONFIG_MODE_INFO* modeInfoArray,
        nint currentTopologyId);

    [LibraryImport("user32.dll")]
    private static unsafe partial int SetDisplayConfig(
        uint numPathArrayElements,
        DISPLAYCONFIG_PATH_INFO* pathArray,
        uint numModeInfoArrayElements,
        DISPLAYCONFIG_MODE_INFO* modeInfoArray,
        uint flags);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    private static extern int DisplayConfigGetDeviceInfo_IntPtr(IntPtr requestPacket);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(string lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwFlags, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int EnumDisplaySettings(string lpszDeviceName, uint iModeNum, ref DEVMODE lpDevMode);

    private const uint CDS_UPDATEREGISTRY = 0x01;
    private const uint CDS_GLOBAL = 0x08;
    private const uint CDS_ENABLE = 0x04;
    private const uint CDS_NORESET = 0x10000000;
    private const uint ENUM_REGISTRY_SETTINGS = 0xFFFFFFFE;
    private const int DISP_CHANGE_SUCCESSFUL = 0;

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
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    private const uint DIGCF_PRESENT = 0x00000002;
    private const uint DIGCF_PROFILE = 0x00000008;
    private const uint SPDRP_DEVICEDESC = 0x00000000;
    private const uint SPDRP_FRIENDLYNAME = 0x0000000C;
    private const uint SPDRP_HARDWAREID = 0x00000001;
    private static readonly Guid GUID_DEVCLASS_MONITOR = new(0x4D36E96E, 0xE325, 0x11CE, 0xBF, 0xC1, 0x08, 0x00, 0x2B, 0xE1, 0x03, 0x18);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint SetupDiGetClassDevs(ref Guid classGuid, nint enumerator, nint hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList(nint deviceInfoSet);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInfo(nint deviceInfoSet, uint memberIndex, ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceRegistryProperty(nint deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property, out uint propertyRegDataType, nint propertyBuffer, uint propertyBufferSize, out uint requiredSize);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInstanceId(nint deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, nint deviceInstanceId, uint deviceInstanceIdSize, out uint requiredSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid classGuid;
        public uint devInst;
        public nint reserved;
    }

    public static List<string> GetMonitorNamesViaSetupApi()
    {
        var names = new List<string>();
        var guid = GUID_DEVCLASS_MONITOR;
        var devInfoSet = SetupDiGetClassDevs(ref guid, nint.Zero, nint.Zero, DIGCF_PRESENT);
        if (devInfoSet == (nint)(-1))
        {
            return names;
        }

        try
        {
            uint index = 0;
            var devInfoData = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
            while (SetupDiEnumDeviceInfo(devInfoSet, index, ref devInfoData))
            {
                var name = GetDevicePropertyString(devInfoSet, ref devInfoData, SPDRP_FRIENDLYNAME);
                if (string.IsNullOrEmpty(name))
                {
                    name = GetDevicePropertyString(devInfoSet, ref devInfoData, SPDRP_DEVICEDESC);
                }

                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
                }

                index++;
                devInfoData = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(devInfoSet);
        }

        return names;
    }

    private static string GetDevicePropertyString(nint devInfoSet, ref SP_DEVINFO_DATA devInfoData, uint property)
    {
        var result = SetupDiGetDeviceRegistryProperty(devInfoSet, ref devInfoData, property, out _, nint.Zero, 0, out var requiredSize);
        if (!result && requiredSize == 0)
        {
            return string.Empty;
        }

        var ptr = Marshal.AllocHGlobal((int)requiredSize);
        try
        {
            result = SetupDiGetDeviceRegistryProperty(devInfoSet, ref devInfoData, property, out _, ptr, requiredSize, out _);
            return result ? Marshal.PtrToStringUni(ptr) ?? string.Empty : string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayDevices(
        string? lpDevice,
        uint iDevNum,
        ref DISPLAY_DEVICE lpDisplayDevice,
        uint dwFlags);

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

    public static string? GetDisplayName(DISPLAYCONFIG_PATH_TARGET_INFO targetInfo)
    {
        var targetId = new DisplayTargetId(targetInfo.adapterId, targetInfo.id);

        var nameFromApi = GetDisplayNameFromConfig(targetInfo);
        if (nameFromApi != null)
        {
            return nameFromApi;
        }

        EnsureDisplayNameCache();
        if (_displayNameCache!.TryGetValue(targetId, out var cached))
        {
            return cached;
        }

        return null;
    }

    private static void EnsureDisplayNameCache()
    {
        if (_displayNameCache != null)
        {
            return;
        }

        _displayNameCache = new Dictionary<DisplayTargetId, string>();
        _cachedTargetOrder = [];

        var setupApiNames = GetMonitorNamesViaSetupApi();
        var allPaths = GetAllPaths();
        var uniqueTargets = new List<(DisplayTargetId, DISPLAYCONFIG_PATH_TARGET_INFO)>();
        var seen = new HashSet<DisplayTargetId>();

        foreach (var path in allPaths)
        {
            var id = new DisplayTargetId(path.targetInfo.adapterId, path.targetInfo.id);
            if (seen.Add(id))
            {
                uniqueTargets.Add((id, path.targetInfo));
            }
        }

        _cachedTargetOrder = uniqueTargets.Select(t => t.Item1).ToArray();

        if (uniqueTargets.Count > 0 && setupApiNames.Count > 0)
        {
            int nameCount = Math.Min(setupApiNames.Count, uniqueTargets.Count);
            for (int i = 0; i < nameCount; i++)
            {
                _displayNameCache[uniqueTargets[i].Item1] = setupApiNames[i];
            }
        }

        for (int i = 0; i < uniqueTargets.Count; i++)
        {
            var id = uniqueTargets[i].Item1;
            if (!_displayNameCache.ContainsKey(id))
            {
                _displayNameCache[id] = string.Format(Properties.Resources.display_label, i + 1);
            }
        }
    }

    public static string GetTargetFriendlyName(DISPLAYCONFIG_PATH_TARGET_INFO targetInfo)
    {
        return GetDisplayName(targetInfo) ?? Properties.Resources.unknown_display;
    }

    public static string GetCurrentTopologySummary()
    {
        var (paths, _) = GetActivePaths();
        if (paths.Length == 0)
        {
            return Properties.Resources.no_active_displays;
        }

        var lines = new List<string>();
        for (int i = 0; i < paths.Length; i++)
        {
            var name = GetTargetFriendlyName(paths[i].targetInfo);
            var tech = GetOutputTechnologyName(paths[i].targetInfo.outputTechnology);
            lines.Add($"{i + 1}. {name} ({tech})");
        }

        return string.Join(", ", lines);
    }

    private static unsafe string? GetDisplayNameFromConfig(DISPLAYCONFIG_PATH_TARGET_INFO targetInfo)
    {
        var sz = Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
        var ptr = Marshal.AllocHGlobal(sz);
        try
        {
            var deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME
            {
                header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    type = DISPLAYCONFIG_DEVICE_INFO_TYPE.GetTargetName,
                    size = (uint)sz,
                    adapterId = targetInfo.adapterId,
                    id = targetInfo.id,
                },
            };
            Marshal.StructureToPtr(deviceName, ptr, false);
            var result = DisplayConfigGetDeviceInfo_IntPtr(ptr);
            if (result == 0)
            {
                var resultName = Marshal.PtrToStructure<DISPLAYCONFIG_TARGET_DEVICE_NAME>(ptr);
                return resultName.GetMonitorFriendlyDeviceName();
            }

            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static string GetOutputTechnologyName(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY tech) => tech switch
    {
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.HDMI => "HDMI",
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DisplayPortExternal => "DisplayPort",
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DisplayPortEmbedded => "eDP",
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DVI => "DVI",
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.VGA => "VGA",
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.LVDS => "LVDS",
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Internal => "Internal",
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.IndirectWired => "IndirectWired",
        DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.IndirectVirtual => "Virtual",
        _ => $"Other({(int)tech})",
    };

    public static unsafe (DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes) GetActivePaths()
    {
        var result = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out var numPathArrayElements, out var numModeInfoArrayElements);
        if (result != 0)
        {
            return ([], []);
        }

        var pathArray = new DISPLAYCONFIG_PATH_INFO[numPathArrayElements];
        var modeInfoArray = new DISPLAYCONFIG_MODE_INFO[numModeInfoArrayElements];

        fixed (DISPLAYCONFIG_PATH_INFO* pathsPtr = pathArray)
        {
            fixed (DISPLAYCONFIG_MODE_INFO* modesPtr = modeInfoArray)
            {
                result = QueryDisplayConfig(
                    QDC_ONLY_ACTIVE_PATHS,
                    ref numPathArrayElements,
                    pathsPtr,
                    ref numModeInfoArrayElements,
                    modesPtr,
                    nint.Zero);
            }
        }

        if (result != 0)
        {
            return ([], []);
        }

        Array.Resize(ref pathArray, (int)numPathArrayElements);
        Array.Resize(ref modeInfoArray, (int)numModeInfoArrayElements);
        return (pathArray, modeInfoArray);
    }

    private static unsafe string SetTopology(DISPLAYCONFIG_TOPOLOGY_ID topology)
    {
        SaveState();
        var flags = SDC_APPLY | SDC_SAVE_TO_DATABASE | SDC_ALLOW_CHANGES | SDC_TOPOLOGY_SUPPLIED;
        var err = SetDisplayConfig(0, null, 0, null, flags | (uint)topology);
        if (err != 0)
        {
            flags = SDC_APPLY | SDC_ALLOW_CHANGES | SDC_TOPOLOGY_SUPPLIED;
            err = SetDisplayConfig(0, null, 0, null, flags | (uint)topology);
        }

        if (err == 0)
        {
            EnableEscRestore();
        }
        else
        {
            _savedState = null;
        }

        return err switch
        {
            0 => Properties.Resources.switched_display_topology,
            -1 or 87 => string.Format(Properties.Resources.error_format, GetWin32ErrorMessage(err)),
            _ => string.Format(Properties.Resources.error_format, GetWin32ErrorMessage(err)),
        };
    }

    public static string SetExternalOnly()
    {
        return SetTopology(DISPLAYCONFIG_TOPOLOGY_ID.External);
    }

    public static string SetInternalOnly()
    {
        return SetTopology(DISPLAYCONFIG_TOPOLOGY_ID.Internal);
    }

    public static string SetClone()
    {
        return SetTopology(DISPLAYCONFIG_TOPOLOGY_ID.Clone);
    }

    public static string SetExtend()
    {
        return SetTopology(DISPLAYCONFIG_TOPOLOGY_ID.Extend);
    }

    public static string GetWin32ErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            0 => "Success",
            -1 => "ERROR_INVALID_PARAMETER: One or more parameters are invalid",
            -2 => "ERROR_NOT_SUPPORTED: The requested operation is not supported",
            -5 => "ERROR_ACCESS_DENIED: Access denied",
            8 => "ERROR_NOT_ENOUGH_MEMORY: Not enough memory",
            87 => "ERROR_INVALID_PARAMETER: The parameter is incorrect",
            123 => "ERROR_INVALID_NAME: The filename or extension is invalid",
            1601 => "ERROR_NO_MORE_ITEMS: No more data is available",
            1460 => "ERROR_TIMEOUT: The operation timed out",
            _ => $"Unknown error code: {errorCode}",
        };
    }

    public static void SaveState()
    {
        var (paths, modes) = GetActivePaths();
        lock (StateLock)
        {
            _savedState = (paths, modes);
        }
    }

    public static bool HasSavedState
    {
        get
        {
            lock (StateLock)
            {
                return _savedState.HasValue;
            }
        }
    }

    public static unsafe string RestoreState()
    {
        (DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes) snapshot;
        lock (StateLock)
        {
            if (_savedState == null)
            {
                return Properties.Resources.no_saved_state;
            }

            snapshot = _savedState.Value;
            _savedState = null;
        }

        if (snapshot.paths.Length == 0)
        {
            return string.Format(Properties.Resources.error_format, Properties.Resources.error_saved_state_no_paths);
        }

        foreach (ref var path in snapshot.paths.AsSpan())
        {
            path.sourceInfo.modeInfoIdx = 0xFFFFFFFF;
            path.targetInfo.modeInfoIdx = 0xFFFFFFFF;
        }

        var flags = SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES;
        int err;

        fixed (DISPLAYCONFIG_PATH_INFO* pathsPtr = snapshot.paths)
        {
            fixed (DISPLAYCONFIG_MODE_INFO* modesPtr = snapshot.modes)
            {
                err = SetDisplayConfig(
                    (uint)snapshot.paths.Length,
                    pathsPtr,
                    (uint)snapshot.modes.Length,
                    modesPtr,
                    flags);
            }
        }

        CancelEscRestore();

        return err switch
        {
            0 => Properties.Resources.display_state_restored,
            -1 or 87 => string.Format(Properties.Resources.restore_failed_format, GetWin32ErrorMessage(err)),
            _ => string.Format(Properties.Resources.restore_failed_format, GetWin32ErrorMessage(err)),
        };
    }

    public static void EnableEscRestore()
    {
        lock (StateLock)
        {
            if (_activeHook != null)
            {
                _activeHook.Register(0x1B, () =>
                {
                    var msg = RestoreState();
                    System.Diagnostics.Debug.WriteLine($"ESC restore: {msg}");
                });
                return;
            }

            _activeHook = KeyboardHook.CreateWithEsc(() =>
            {
                var msg = RestoreState();
                System.Diagnostics.Debug.WriteLine($"ESC restore: {msg}");
            });
        }
    }

    public static void CancelEscRestore()
    {
        lock (StateLock)
        {
            if (_activeHook != null)
            {
                _activeHook.Dispose();
                _activeHook = null;
            }
        }
    }

    public static void EnableXboxGuideViewCombo()
    {
        lock (StateLock)
        {
            if (_xboxPollTimer != null)
            {
                return;
            }

            _guideWasPressed = false;
            _xboxPollTimer = new Timer(
                _ => PollXboxGuide(),
                null,
                0,
                100);
        }
    }

    public static void DisableXboxGuideViewCombo()
    {
        lock (StateLock)
        {
            _xboxPollTimer?.Dispose();
            _xboxPollTimer = null;
        }
    }

    private static void PollXboxGuide()
    {
        // Try all 4 controller indices
        for (int userIndex = 0; userIndex < XinputMaxControllers; userIndex++)
        {
            var state = default(XINPUT_STATE);
            if (XInputGetState(userIndex, ref state) != 0)
            {
                continue;
            }

            bool guideDown = (state.Gamepad.wButtons & XinputGamepadGuide) != 0;
            bool viewDown = (state.Gamepad.wButtons & XinputGamepadBack) != 0;

            if (guideDown && !_guideWasPressed)
            {
                _guideWasPressed = true;

                if (viewDown)
                {
                    CycleSavedProfiles();
                    GuideViewComboPressed?.Invoke();
                }

                return;
            }

            _guideWasPressed = guideDown;
            return;
        }

        _guideWasPressed = false;
    }

    private static int _profileCycleIndex;

    private static void CycleSavedProfiles()
    {
        var profiles = GetSavedProfiles();
        if (profiles.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("Xbox Guide+View: no saved profiles to cycle");
            return;
        }

        _profileCycleIndex %= profiles.Count;
        var (fileName, name) = profiles[_profileCycleIndex];
        _profileCycleIndex++;
        var msg = ApplySavedProfileReference(fileName, referenceIsFileName: true);
        System.Diagnostics.Debug.WriteLine($"Xbox Guide+View applied profile '{name}': {msg}");
    }

    private static bool IsViewButtonPressed()
    {
        for (int i = 0; i < XinputMaxControllers; i++)
        {
            var state = default(XINPUT_STATE);
            if (XInputGetState(i, ref state) == 0 && (state.Gamepad.wButtons & XinputGamepadBack) != 0)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryActivateDisplays(List<DisplayTargetId> targets, out string message)
    {
        message = null!;

        if (targets.Count == 0)
        {
            message = string.Format(Properties.Resources.error_format, Properties.Resources.error_no_displays_selected);
            return false;
        }

        var allPaths = GetAllPaths();
        var allUniqueTargets = allPaths
            .Select(p => new DisplayTargetId(p.targetInfo.adapterId, p.targetInfo.id))
            .Distinct()
            .ToList();

        // Safety check: at least one selected target must exist in hardware
        var validTargets = targets.Where(t =>
            allUniqueTargets.Any(u => u.AdapterId.Equals(t.AdapterId) && u.TargetId == t.TargetId)).ToList();

        if (validTargets.Count == 0)
        {
            message = string.Format(Properties.Resources.error_format, Properties.Resources.error_no_displays_connected);
            return false;
        }

        // Proceed with activation
        message = ActivateDisplays(validTargets);
        return true;
    }

    public static unsafe DISPLAYCONFIG_PATH_INFO[] GetAllPaths()
    {
        uint flags = QDC_ALL_PATHS | QDC_VIRTUAL_MODE_AWARE;
        var result = GetDisplayConfigBufferSizes(flags, out var numPathArrayElements, out var numModeInfoArrayElements);
        if (result != 0)
        {
            return FallbackToActivePaths();
        }

        var pathArray = new DISPLAYCONFIG_PATH_INFO[numPathArrayElements];
        var modeInfoArray = new DISPLAYCONFIG_MODE_INFO[numModeInfoArrayElements];

        fixed (DISPLAYCONFIG_PATH_INFO* pathsPtr = pathArray)
        {
            fixed (DISPLAYCONFIG_MODE_INFO* modesPtr = modeInfoArray)
            {
                result = QueryDisplayConfig(
                    flags,
                    ref numPathArrayElements,
                    pathsPtr,
                    ref numModeInfoArrayElements,
                    modesPtr,
                    nint.Zero);
            }
        }

        if (result != 0)
        {
            return FallbackToActivePaths();
        }

        Array.Resize(ref pathArray, (int)numPathArrayElements);
        return pathArray;
    }

    private static unsafe DISPLAYCONFIG_PATH_INFO[] FallbackToActivePaths()
    {
        var (activePaths, _) = GetActivePaths();
        return activePaths;
    }

    internal static bool IsInternalTechnology(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY tech)
        => tech == DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Internal || ((uint)tech & 0x80000000) != 0;

    private static DISPLAYCONFIG_TOPOLOGY_ID ClassifyTopology(List<DisplayTargetId> targets)
    {
        // Use all paths (not just active) so profiles targeting inactive displays
        // are classified correctly — active-only misses disconnected/off monitors.
        var techByTarget = GetAllPaths()
            .GroupBy(p => new DisplayTargetId(p.targetInfo.adapterId, p.targetInfo.id))
            .ToDictionary(g => g.Key, g => g.First().targetInfo.outputTechnology);
        return ClassifyTopology(targets, techByTarget);
    }

    internal static DISPLAYCONFIG_TOPOLOGY_ID ClassifyTopology(
        List<DisplayTargetId> targets,
        IReadOnlyDictionary<DisplayTargetId, DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY> techByTarget)
    {
        var selectedInternal = 0;
        var selectedExternal = 0;
        foreach (var t in targets)
        {
            if (techByTarget.TryGetValue(t, out var tech))
            {
                if (IsInternalTechnology(tech))
                {
                    selectedInternal++;
                }
                else
                {
                    selectedExternal++;
                }
            }
        }

        // Exactly one external display → External
        if (selectedExternal == 1 && selectedInternal == 0)
        {
            return DISPLAYCONFIG_TOPOLOGY_ID.External;
        }

        // Exactly one internal display → Internal
        if (selectedInternal == 1 && selectedExternal == 0)
        {
            return DISPLAYCONFIG_TOPOLOGY_ID.Internal;
        }

        // Multiple displays → Extend
        return DISPLAYCONFIG_TOPOLOGY_ID.Extend;
    }

    public static string ActivateDisplays(List<DisplayTargetId> targets)
    {
        SaveState();

        var topology = ClassifyTopology(targets);
        var result = SetTopology(topology);
        if (!result.StartsWith(Properties.Resources.error_prefix, StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }

        // Topology fallback failed — try with path array as last resort.
        // QDC_ALL_PATHS returns multiple paths per target (one per possible source),
        // so deduplicate by target and prefer active paths to avoid ERROR_INVALID_PARAMETER.
        var allPaths = GetAllPaths();
        var selectedPaths = allPaths
            .Where(p => targets.Any(t =>
                t.AdapterId.Equals(p.targetInfo.adapterId) &&
                t.TargetId == p.targetInfo.id))
            .GroupBy(p => new DisplayTargetId(p.targetInfo.adapterId, p.targetInfo.id))
            .Select(g => g.OrderByDescending(p => (p.flags & 1u) != 0 ? 1 : 0).First())
            .ToArray();

        if (selectedPaths.Length == 0)
        {
            _savedState = null;
            return string.Format(Properties.Resources.error_format, Properties.Resources.error_no_matching_paths);
        }

        for (int i = 0; i < selectedPaths.Length; i++)
        {
            selectedPaths[i].sourceInfo.modeInfoIdx = 0xFFFFFFFF;
            selectedPaths[i].targetInfo.modeInfoIdx = 0xFFFFFFFF;
        }

        var flags = SDC_APPLY | SDC_SAVE_TO_DATABASE | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES | SDC_VIRTUAL_MODE_AWARE;
        int err;

        unsafe
        {
            fixed (DISPLAYCONFIG_PATH_INFO* selectedPtr = selectedPaths)
            {
                err = SetDisplayConfig((uint)selectedPaths.Length, selectedPtr, 0, null, flags);
            }
        }

        if (err != 0)
        {
            flags = SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES | SDC_VIRTUAL_MODE_AWARE;
            unsafe
            {
                fixed (DISPLAYCONFIG_PATH_INFO* selectedPtr = selectedPaths)
                {
                    err = SetDisplayConfig((uint)selectedPaths.Length, selectedPtr, 0, null, flags);
                }
            }
        }

        if (err == 0)
        {
            EnableEscRestore();
            SaveSnapshot();
            return string.Format(Properties.Resources.activated_displays_format, targets.Count);
        }

        // CDSE fallback for builds where SetDisplayConfig returns ERROR_INVALID_PARAMETER
        int cdseResult = TryActivateViaCDSE(targets);
        if (cdseResult == DISP_CHANGE_SUCCESSFUL)
        {
            EnableEscRestore();
            SaveSnapshot();
            return string.Format(Properties.Resources.activated_displays_format, targets.Count);
        }

        _savedState = null;
        return string.Format(Properties.Resources.error_format, GetWin32ErrorMessage(err));
    }

    private static readonly string ProfilesDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonitorPowerExtension",
        "profiles");

    private static readonly string SnapshotDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonitorPowerExtension");

    private static readonly string SnapshotPath = System.IO.Path.Combine(SnapshotDir, "snapshot.json");

    internal static void SaveSnapshot()
    {
        var (paths, _) = GetActivePaths();
        var deviceNameMap = BuildTargetToDeviceNameMap();
        var targets = new List<SnapshotTarget>();
        foreach (var p in paths)
        {
            var id = new DisplayTargetId(p.targetInfo.adapterId, p.targetInfo.id);
            deviceNameMap.TryGetValue(id, out var deviceName);
            deviceName ??= string.Empty;
            var dm = default(DEVMODE);
            dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            int regOk = string.IsNullOrEmpty(deviceName) ? 0 : EnumDisplaySettings(deviceName, ENUM_REGISTRY_SETTINGS, ref dm);
            targets.Add(new SnapshotTarget
            {
                LowPart = id.AdapterId.LowPart,
                HighPart = id.AdapterId.HighPart,
                TargetId = id.TargetId,
                DeviceName = deviceName,
                Width = regOk != 0 ? dm.dmPelsWidth : 0,
                Height = regOk != 0 ? dm.dmPelsHeight : 0,
                Frequency = regOk != 0 ? dm.dmDisplayFrequency : 0,
                PositionX = regOk != 0 ? dm.dmPositionX : 0,
                PositionY = regOk != 0 ? dm.dmPositionY : 0,
                Orientation = regOk != 0 ? dm.dmDisplayOrientation : 0,
                BitsPerPel = regOk != 0 ? dm.dmBitsPerPel : 0,
            });
        }

        if (!Directory.Exists(SnapshotDir))
        {
            Directory.CreateDirectory(SnapshotDir);
        }

        File.WriteAllText(SnapshotPath, JsonSerializer.Serialize(new { targets }));
    }

    private sealed class SnapshotTarget
    {
        public uint LowPart { get; set; }

        public int HighPart { get; set; }

        public uint TargetId { get; set; }

        public string DeviceName { get; set; } = string.Empty;

        public int Width { get; set; }

        public int Height { get; set; }

        public int Frequency { get; set; }

        public int PositionX { get; set; }

        public int PositionY { get; set; }

        public int Orientation { get; set; }

        public int BitsPerPel { get; set; }
    }

    // Build a map from DisplayTargetId → GDI device name by walking all paths and
    // using the *source* id (not the target id) for GetSourceName.
    private static Dictionary<DisplayTargetId, string> BuildTargetToDeviceNameMap()
    {
        var map = new Dictionary<DisplayTargetId, string>();
        uint qflags = QDC_ALL_PATHS | QDC_VIRTUAL_MODE_AWARE;
        int hr = GetDisplayConfigBufferSizes(qflags, out var np, out var nm);
        if (hr != 0)
        {
            return map;
        }

        var pa = new DISPLAYCONFIG_PATH_INFO[np];
        var ma = new DISPLAYCONFIG_MODE_INFO[nm];
        unsafe
        {
            fixed (DISPLAYCONFIG_PATH_INFO* pp = pa)
            {
                fixed (DISPLAYCONFIG_MODE_INFO* mm = ma)
                {
                    hr = QueryDisplayConfig(qflags, ref np, pp, ref nm, mm, nint.Zero);
                    if (hr != 0)
                    {
                        return map;
                    }
                }
            }
        }

        Array.Resize(ref pa, (int)np);
        foreach (var p in pa)
        {
            var targetId = new DisplayTargetId(p.targetInfo.adapterId, p.targetInfo.id);
            if (map.ContainsKey(targetId))
            {
                continue;
            }

            // GetSourceName requires the *source* id from sourceInfo — not the target id.
            unsafe
            {
                var srcName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_TYPE.GetSourceName,
                        size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME),
                        adapterId = p.sourceInfo.adapterId,
                        id = p.sourceInfo.id,
                    },
                };
                DISPLAYCONFIG_SOURCE_DEVICE_NAME* ptr = &srcName;
                int nameHr = DisplayConfigGetDeviceInfo_IntPtr((nint)ptr);
                if (nameHr == 0)
                {
                    var gdiName = srcName.GetViewGdiDeviceName();
                    if (!string.IsNullOrEmpty(gdiName))
                    {
                        map[targetId] = gdiName;
                        continue;
                    }
                }
            }

            // Fallback: no usable name found for this target
        }

        return map;
    }

    private static unsafe int TryActivateViaCDSE(List<DisplayTargetId>? targetsToActivate = null)
    {
        // Build target → GDI device name map using correct source IDs
        var targetToDevice = BuildTargetToDeviceNameMap();
        if (targetToDevice.Count == 0)
        {
            return -1;
        }

        var activateSet = targetsToActivate != null
            ? new HashSet<DisplayTargetId>(targetsToActivate)
            : null;

        // First, disable monitors NOT in the requested set (pass null DEVMODE)
        if (activateSet != null)
        {
            foreach (var kvp in targetToDevice)
            {
                if (activateSet.Contains(kvp.Key))
                {
                    continue;
                }

                var deviceName = kvp.Value;
                if (!deviceName.StartsWith(@"\\.\DISPLAY", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Disable this monitor by passing null DEVMODE with CDS_UPDATEREGISTRY | CDS_GLOBAL
                _ = ChangeDisplaySettingsEx(deviceName, nint.Zero, nint.Zero, CDS_UPDATEREGISTRY | CDS_GLOBAL | CDS_NORESET, nint.Zero);
            }
        }

        // Then activate each requested target
        foreach (var target in targetToDevice)
        {
            if (activateSet != null && !activateSet.Contains(target.Key))
            {
                continue;
            }

            var deviceName = target.Value;
            if (!deviceName.StartsWith(@"\\.\DISPLAY", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dm = default(DEVMODE);
            dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            int regOk = EnumDisplaySettings(deviceName, ENUM_REGISTRY_SETTINGS, ref dm);
            if (regOk == 0 || dm.dmPelsWidth == 0 || dm.dmPelsHeight == 0)
            {
                // Try CDS_ENABLE as a last resort
                int enableHr = ChangeDisplaySettingsEx(
                    deviceName, nint.Zero, nint.Zero, CDS_ENABLE | CDS_NORESET, nint.Zero);
                if (enableHr != DISP_CHANGE_SUCCESSFUL)
                {
                    _ = ChangeDisplaySettingsEx(
                        deviceName, nint.Zero, nint.Zero, CDS_UPDATEREGISTRY | CDS_GLOBAL | CDS_NORESET, nint.Zero);
                }

                continue;
            }

            // Apply the registry settings with NORESET so we can commit all at once
            int size = Marshal.SizeOf<DEVMODE>();
            nint dmPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(dm, dmPtr, false);
                int result = ChangeDisplaySettingsEx(
                    deviceName, dmPtr, nint.Zero, CDS_UPDATEREGISTRY | CDS_GLOBAL | CDS_NORESET, nint.Zero);
                if (result != DISP_CHANGE_SUCCESSFUL)
                {
                    _ = ChangeDisplaySettingsEx(deviceName, dmPtr, nint.Zero, CDS_GLOBAL | CDS_NORESET, nint.Zero);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(dmPtr);
            }
        }

        // Commit all deferred changes at once
        _ = ChangeDisplaySettingsEx(null!, nint.Zero, nint.Zero, 0, nint.Zero);

        SaveSnapshot();
        return DISP_CHANGE_SUCCESSFUL;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalid.Contains(c)).ToArray());
    }

    public static string? ResolveSavedProfileFileName(string profileReference, bool referenceIsFileName)
    {
        if (referenceIsFileName)
        {
            // Reject any input that contains path separators to prevent directory traversal
            if (profileReference.Contains(System.IO.Path.DirectorySeparatorChar) ||
                profileReference.Contains(System.IO.Path.AltDirectorySeparatorChar))
            {
                return null;
            }

            var name = System.IO.Path.GetFileNameWithoutExtension(profileReference);
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return $"{name}.json";
        }

        var profiles = GetSavedProfiles();
        foreach (var (fileName, pName) in profiles)
        {
            if (string.Equals(pName, profileReference, StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }
        }

        return null;
    }

    public static string ApplySavedProfileReference(string profileReference, bool referenceIsFileName)
    {
        var fileName = ResolveSavedProfileFileName(profileReference, referenceIsFileName);
        if (fileName == null)
        {
            return string.Format(Properties.Resources.error_format, Properties.Resources.error_profile_not_found);
        }

        DisplayProfileApplyStarted?.Invoke();
        try
        {
            return ApplyNamedProfile(fileName);
        }
        finally
        {
            DisplayProfileApplyCompleted?.Invoke();
        }
    }

    public static string SaveNamedProfile(string name, List<DisplayTargetId> targets)
    {
        if (targets.Count == 0)
        {
            return string.Format(Properties.Resources.error_format, Properties.Resources.error_no_displays_selected);
        }

        var safeName = SanitizeFileName(name);
        if (!System.IO.Directory.Exists(ProfilesDir))
        {
            System.IO.Directory.CreateDirectory(ProfilesDir);
        }

        var path = System.IO.Path.Combine(ProfilesDir, $"{safeName}.json");
        var data = new { Name = name, Targets = targets };
        System.IO.File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(data));
        SaveSnapshot();
        return Properties.Resources.profile_saved;
    }

    public static List<(string FileName, string Name)> GetSavedProfiles()
    {
        if (!System.IO.Directory.Exists(ProfilesDir))
        {
            return [];
        }

        var list = new List<(string, string)>();
        foreach (var f in System.IO.Directory.GetFiles(ProfilesDir, "*.json"))
        {
            try
            {
                var json = System.IO.File.ReadAllText(f);
                using var doc = JsonDocument.Parse(json);
                var name = doc.RootElement.GetProperty("Name").GetString();
                if (!string.IsNullOrEmpty(name))
                {
                    list.Add((System.IO.Path.GetFileName(f), name));
                }
            }
            catch
            {
            }
        }

        return list;
    }

    public static string ApplyNamedProfile(string fileName)
    {
        var path = System.IO.Path.Combine(ProfilesDir, fileName);
        if (!System.IO.File.Exists(path))
        {
            return string.Format(Properties.Resources.error_format, Properties.Resources.error_profile_not_found);
        }

        var json = System.IO.File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var targetsElement = doc.RootElement.GetProperty("Targets");
        var targets = System.Text.Json.JsonSerializer.Deserialize<List<DisplayTargetId>>(targetsElement.GetRawText());
        if (targets == null || targets.Count == 0)
        {
            return string.Format(Properties.Resources.error_format, Properties.Resources.error_profile_empty);
        }

        return ActivateDisplays(targets);
    }

    public static void DeleteSavedProfile(string fileName)
    {
        var path = System.IO.Path.Combine(ProfilesDir, fileName);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }

    public static (string Name, List<DisplayTargetId> Targets)? LoadNamedProfile(string fileName)
    {
        var path = System.IO.Path.Combine(ProfilesDir, fileName);
        if (!System.IO.File.Exists(path))
        {
            return null;
        }

        var json = System.IO.File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var name = doc.RootElement.GetProperty("Name").GetString();
        var targetsElement = doc.RootElement.GetProperty("Targets");
        var targets = System.Text.Json.JsonSerializer.Deserialize<List<DisplayTargetId>>(targetsElement.GetRawText());
        if (string.IsNullOrEmpty(name) || targets == null || targets.Count == 0)
        {
            return null;
        }

        return (name, targets);
    }

    public static string OverwriteNamedProfile(string fileName, string name, List<DisplayTargetId> targets)
    {
        if (targets.Count == 0)
        {
            return string.Format(Properties.Resources.error_format, Properties.Resources.error_no_displays_selected);
        }

        var safeName = SanitizeFileName(name);
        if (!System.IO.Directory.Exists(ProfilesDir))
        {
            System.IO.Directory.CreateDirectory(ProfilesDir);
        }

        var path = System.IO.Path.Combine(ProfilesDir, $"{safeName}.json");
        var data = new { Name = name, Targets = targets };
        System.IO.File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(data));
        SaveSnapshot();
        return Properties.Resources.profile_saved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState(int dwUserIndex, ref XINPUT_STATE pState);
}
