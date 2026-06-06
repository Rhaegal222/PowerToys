$csharpCode = @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public static class DisplayExtender
{
    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    private static unsafe extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements,
        DISPLAYCONFIG_PATH_INFO* pathArray, ref uint numModeInfoArrayElements,
        DISPLAYCONFIG_MODE_INFO* modeInfoArray, IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    private static unsafe extern int SetDisplayConfig(uint np, DISPLAYCONFIG_PATH_INFO* pathArray,
        uint nm, DISPLAYCONFIG_MODE_INFO* modeInfoArray, uint flags);

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
        Console.WriteLine($"{label}: 0x{flags:X8} -> {hr}");
        return hr;
    }

    public static unsafe int TestAll()
    {
        // Exact flag combination the old SetTopology code used (without SDC_APPLY):
        // SDC_TOPOLOGY_SUPPLIED | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_FORCE_MODE_ENUMERATION | SDC_VIRTUAL_MODE_AWARE | SDC_ALLOW_CHANGES | Extend
        uint oldStyle = SDC_TOPOLOGY | SDC_USE_SUPPLIED | SDC_FORCE | SDC_VM | SDC_ALLOW | 4;
        Call("OldStyle", 0, null, 0, null, oldStyle);

        // OldStyle minus SDC_USE_SUPPLIED (the fallback style from New Step A)
        uint stepAFallback = SDC_APPLY | SDC_TOPOLOGY | SDC_ALLOW | SDC_FORCE | 4;
        Call("StepAFallback", 0, null, 0, null, stepAFallback);

        // Minimal: just SDC_TOPOLOGY | Extend
        Call("TopoOnly", 0, null, 0, null, SDC_TOPOLOGY | 4);
        // Apply + Topo
        Call("Apply+Topo", 0, null, 0, null, SDC_APPLY | SDC_TOPOLOGY | 4);
        // Apply + Topo + Supplied
        Call("All3", 0, null, 0, null, SDC_APPLY | SDC_TOPOLOGY | SDC_USE_SUPPLIED | 4);
        // Topo + Supplied
        Call("Topo+Supplied", 0, null, 0, null, SDC_TOPOLOGY | SDC_USE_SUPPLIED | 4);
        // Apply only
        Call("ApplyOnly", 0, null, 0, null, SDC_APPLY);
        // Supplied only
        Call("SuppliedOnly", 0, null, 0, null, SDC_USE_SUPPLIED);
        // Save
        Call("SaveOnly", 0, null, 0, null, SDC_SAVE);

        // Now try with actual active paths
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
            Console.WriteLine($"Re-applying {activePaths.Count} active paths...");
            var ap = activePaths.ToArray();
            var usedModeIndices = new HashSet<uint>();
            foreach (var p in ap) usedModeIndices.Add(p.si.modeInfoIdx);
            var am = ma.Where(m => usedModeIndices.Contains(m.id)).ToArray();
            // Build a proper modeInfoArray (all entries that are target modes too)
            var allUsed = new HashSet<uint>();
            foreach (var p in ap) { allUsed.Add(p.si.modeInfoIdx); allUsed.Add(p.ti.modeInfoIdx); }
            var am2 = ma.Where((m, i) => allUsed.Contains((uint)i)).ToArray();

            fixed (DISPLAYCONFIG_PATH_INFO* ppp = ap) fixed (DISPLAYCONFIG_MODE_INFO* mmm = am2)
            {
                Call("Reapply", (uint)ap.Length, ppp, (uint)am2.Length, mmm, SDC_APPLY | SDC_USE_SUPPLIED | SDC_ALLOW);
                Call("Reapply+Force", (uint)ap.Length, ppp, (uint)am2.Length, mmm, SDC_APPLY | SDC_USE_SUPPLIED | SDC_ALLOW | SDC_FORCE | SDC_VM);
            }
        }
        return 0;
    }
}
'@

Add-Type -TypeDefinition $csharpCode -Language CSharp -CompilerOptions "/unsafe" -ErrorAction Stop
[DisplayExtender]::TestAll()
