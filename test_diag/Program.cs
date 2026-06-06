#nullable disable

using System;
using System.Runtime.InteropServices;

public class Program
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LUID {
        public uint LowPart;
        public int HighPart;
        public override string ToString() => $"{HighPart:X8}{LowPart:X8}";
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_SOURCE_INFO {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_TARGET_INFO {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public uint num;
        public uint den;
        public uint scanlineOrdering;
        public int targetAvailable;
        public uint statusFlags;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_INFO {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_MODE_INFO {
        public uint infoType;
        public uint id;
        public LUID adapterId;
        public uint dummy1;
        public uint dummy2;
        public uint dummy3;
        public uint dummy4;
        public uint dummy5;
        public uint dummy6;
        public uint dummy7;
        public uint dummy8;
        public uint dummy9;
        public uint dummy10;
    }
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_DEVICE_INFO_HEADER {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_TARGET_DEVICE_NAME {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint flags;
        public uint outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string monitorDevicePath;
    }

    [DllImport("user32.dll")]
    public static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    public static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements, [In, Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint numModeInfoArrayElements, [In, Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, IntPtr currentTopologyId);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);

    static void Main() {
        uint QDC_ALL_PATHS = 1;
        
        uint numPaths, numModes;
        int res = GetDisplayConfigBufferSizes(QDC_ALL_PATHS, out numPaths, out numModes);
        if (res != 0) {
            Console.WriteLine("GetDisplayConfigBufferSizes failed: " + res);
            return;
        }
        
        var paths = new DISPLAYCONFIG_PATH_INFO[numPaths];
        var modes = new DISPLAYCONFIG_MODE_INFO[numModes];
        res = QueryDisplayConfig(QDC_ALL_PATHS, ref numPaths, paths, ref numModes, modes, IntPtr.Zero);
        if (res != 0) {
            Console.WriteLine("QueryDisplayConfig failed: " + res);
            return;
        }
        
        Console.WriteLine(string.Format("Found {0} paths:", numPaths));
        for (int i = 0; i < numPaths; i++) {
            var path = paths[i];
            var target = path.targetInfo;
            
            var deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            deviceName.header.type = 2; // GetTargetName
            deviceName.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_TARGET_DEVICE_NAME));
            deviceName.header.adapterId = target.adapterId;
            deviceName.header.id = target.id;
            
            string friendlyName = "Unknown";
            int infoRes = DisplayConfigGetDeviceInfo(ref deviceName);
            if (infoRes == 0) {
                friendlyName = deviceName.monitorFriendlyDeviceName;
            }
            
            // Print only paths where the target is available or name is known to keep output readable, 
            // but we can check if they look correct.
            Console.WriteLine(string.Format("Path {0}: Adapter LUID={1}, TargetID={2}, Available={3}, Active={4}, Name={5}", 
                i, target.adapterId.ToString(), target.id, target.targetAvailable, (path.flags & 1) != 0 ? 1 : 0, friendlyName));
        }
    }
}
