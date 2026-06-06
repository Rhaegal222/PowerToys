// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CmdPal.Common.Services;
using Microsoft.CmdPal.Ext.Run;
using Microsoft.CmdPal.UI.ViewModels;
using Microsoft.CmdPal.UI.ViewModels.Services;

namespace Microsoft.CmdPal.UI;

internal sealed class RunHistoryService : IRunHistoryService
{
    private readonly IAppStateService _appStateService;

    public RunHistoryService(IAppStateService appStateService)
    {
        _appStateService = appStateService;
    }

    public IReadOnlyList<string> GetRunHistory()
    {
        if (_appStateService.State.RunHistory.IsEmpty)
        {
            var history = CreateRunHistory();
            _appStateService.UpdateState(state => state with
            {
                RunHistory = history.ToImmutableList(),
            });
        }

        return _appStateService.State.RunHistory;
    }

    public void ClearRunHistory()
    {
        _appStateService.UpdateState(state => state with
        {
            RunHistory = ImmutableList<string>.Empty,
        });
    }

    public void AddRunHistoryItem(string item)
    {
        if (string.IsNullOrWhiteSpace(item))
        {
            return;
        }

        _appStateService.UpdateState(state => state with
        {
            RunHistory = state.RunHistory
                .Remove(item)
                .Insert(0, item),
        });
    }

    public long RunCommand(string commandLine, string workingDir, bool asAdmin, ulong hwnd)
    {
        return RunHistory.ExecuteCommandline(commandLine, workingDir, hwnd, asAdmin);
    }

    public ParseCommandlineResult ParseCommandline(string commandLine, string workingDirectory)
    {
        return RunHistory.ParseCommandline(commandLine, workingDirectory);
    }

    public string QualifyCommandLineDirectory(string commandLine, string fullFilePath, string defaultDirectory)
    {
        return RunHistory.QualifyCommandLineDirectory(commandLine, fullFilePath, defaultDirectory);
    }

    /// <summary>
    /// Reads the Windows Run dialog MRU list from the registry via comctl32.dll.
    /// This is a C# port of the C++ RunHistory::CreateRunHistory() from Microsoft.Terminal.UI.
    /// </summary>
    private static IReadOnlyList<string> CreateRunHistory()
    {
        const string RunMruKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU";
        const int MruCacheWrite = 0x0001;

        var history = new List<string>();

        try
        {
            var hLib = NativeMethods.LoadLibraryExW("comctl32.dll", nint.Zero, NativeMethods.LoadLibrarySearchSystemDirs);
            if (hLib == nint.Zero)
            {
                return history;
            }

            try
            {
                var createMruListAddr = NativeMethods.GetProcAddress(hLib, "CreateMRUListW");
                var enumMruListAddr = NativeMethods.GetProcAddress(hLib, "EnumMRUListW");
                var freeMruListAddr = NativeMethods.GetProcAddress(hLib, "FreeMRUList");
                if (createMruListAddr == nint.Zero || enumMruListAddr == nint.Zero || freeMruListAddr == nint.Zero)
                {
                    return history;
                }

                var createMruList = Marshal.GetDelegateForFunctionPointer<NativeMethods.CreateMRUListWDelegate>(createMruListAddr);
                var enumMruListCount = Marshal.GetDelegateForFunctionPointer<NativeMethods.EnumMRUListWCountDelegate>(enumMruListAddr);
                var enumMruListData = Marshal.GetDelegateForFunctionPointer<NativeMethods.EnumMRUListWDataDelegate>(enumMruListAddr);
                var freeMruList = Marshal.GetDelegateForFunctionPointer<NativeMethods.FreeMRUListDelegate>(freeMruListAddr);

                var mi = new NativeMethods.MRUINFOW
                {
                    cbSize = (uint)Marshal.SizeOf<NativeMethods.MRUINFOW>(),
                    uMax = 26,
                    fFlags = MruCacheWrite,
                    hKey = NativeMethods.HkeyCurrentUser,
                    lpszSubKey = RunMruKey,
                    lpfnCompare = nint.Zero,
                };

                var hMruList = createMruList(ref mi);
                if (hMruList == nint.Zero)
                {
                    return history;
                }

                try
                {
                    var count = enumMruListCount(hMruList, -1, nint.Zero, 0);
                    var buffer = new char[262]; // MAX_PATH + 2

                    for (var i = 0; i < count; i++)
                    {
                        var length = enumMruListData(hMruList, i, buffer, buffer.Length);
                        if (length > 1)
                        {
                            // length includes the null terminator; trim it
                            var text = new string(buffer, 0, length - 1);

                            // Old MRU format appends a backslash + show-cmd letter; strip it.
                            if (text.Length > 0 && text[^1] == '\\')
                            {
                                text = text[..^1];
                            }

                            if (!string.IsNullOrEmpty(text))
                            {
                                history.Add(text);
                            }
                        }
                    }
                }
                finally
                {
                    freeMruList(hMruList);
                }
            }
            finally
            {
                NativeMethods.FreeLibrary(hLib);
            }
        }
        catch
        {
            // Swallow all errors; an empty history is a safe fallback.
        }

        return history;
    }

    private static class NativeMethods
    {
        internal static readonly nint HkeyCurrentUser = unchecked((nint)0x80000001);
        internal const uint LoadLibrarySearchSystemDirs = 0x00000800;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MRUINFOW
        {
            public uint cbSize;
            public uint uMax;
            public uint fFlags;
            public nint hKey;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpszSubKey;
            public nint lpfnCompare;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate nint CreateMRUListWDelegate(ref MRUINFOW lpmi);

        // Used to query the count: pass nItem=-1, lpData=null, uLen=0
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int EnumMRUListWCountDelegate(nint hMRU, int nItem, nint lpData, int uLen);

        // Used to read an entry: pass a char[] buffer
        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        internal delegate int EnumMRUListWDataDelegate(nint hMRU, int nItem, [Out] char[] lpData, int uLen);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate void FreeMRUListDelegate(nint hMRU);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern nint LoadLibraryExW(string lpLibFileName, nint hFile, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern nint GetProcAddress(nint hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(nint hModule);
    }
}
