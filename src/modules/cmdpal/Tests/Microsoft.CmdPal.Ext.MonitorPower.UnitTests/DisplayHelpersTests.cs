// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitorPowerExtension;
using static MonitorPowerExtension.DisplayHelpers;

namespace Microsoft.CmdPal.Ext.MonitorPower.UnitTests;

[TestClass]
public class DisplayHelpersTests
{
    private static readonly string ProfilesDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonitorPowerExtension",
        "profiles");

    private static DisplayTargetId MakeTarget(uint adapterLow, uint targetId) =>
        new(new LUID { LowPart = adapterLow }, targetId);

    private static string WriteSyntheticProfile(string profileName)
    {
        Directory.CreateDirectory(ProfilesDir);
        var fileName = profileName + ".json";
        var path = Path.Combine(ProfilesDir, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Name = profileName,
            Targets = new[] { MakeTarget(1, 1) },
            Layout = Array.Empty<object>(),
        }));
        return fileName;
    }

    [TestMethod]
    public void DisplayConfigPathStructs_MatchNativeLayoutSizes()
    {
        Assert.AreEqual(20, Marshal.SizeOf<DISPLAYCONFIG_PATH_SOURCE_INFO>());
        Assert.AreEqual(48, Marshal.SizeOf<DISPLAYCONFIG_PATH_TARGET_INFO>());
        Assert.AreEqual(72, Marshal.SizeOf<DISPLAYCONFIG_PATH_INFO>());
    }

    [TestMethod]
    public void DisplayConfigPathStructs_VirtualModeIndexesShareModeInfoIdxStorage()
    {
        var source = new DISPLAYCONFIG_PATH_SOURCE_INFO { modeInfoIdx = 0x12345678 };
        var target = new DISPLAYCONFIG_PATH_TARGET_INFO { modeInfoIdx = 0x9ABCDEF0 };

        Assert.AreEqual(0x5678, source.cloneGroupId);
        Assert.AreEqual(0x1234, source.sourceModeInfoIdx);
        Assert.AreEqual(0xDEF0, target.desktopModeInfoIdx);
        Assert.AreEqual(0x9ABC, target.targetModeInfoIdx);
    }

    [TestMethod]
    public void ClassifyTopology_SingleExternalDisplay_ReturnsExternal()
    {
        var t1 = MakeTarget(1, 1);
        var targets = new List<DisplayTargetId> { t1 };
        var tech = new Dictionary<DisplayTargetId, DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY>
        {
            [t1] = DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.HDMI,
        };

        var result = ClassifyTopology(targets, tech);

        Assert.AreEqual(DISPLAYCONFIG_TOPOLOGY_ID.External, result);
    }

    [TestMethod]
    public void ClassifyTopology_SingleInternalDisplay_ReturnsInternal()
    {
        var t1 = MakeTarget(1, 1);
        var targets = new List<DisplayTargetId> { t1 };
        var tech = new Dictionary<DisplayTargetId, DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY>
        {
            [t1] = DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Internal,
        };

        var result = ClassifyTopology(targets, tech);

        Assert.AreEqual(DISPLAYCONFIG_TOPOLOGY_ID.Internal, result);
    }

    [TestMethod]
    public void ClassifyTopology_InternalAndExternalDisplay_ReturnsExtend()
    {
        var t1 = MakeTarget(1, 1);
        var t2 = MakeTarget(1, 2);
        var targets = new List<DisplayTargetId> { t1, t2 };
        var tech = new Dictionary<DisplayTargetId, DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY>
        {
            [t1] = DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Internal,
            [t2] = DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.HDMI,
        };

        var result = ClassifyTopology(targets, tech);

        Assert.AreEqual(DISPLAYCONFIG_TOPOLOGY_ID.Extend, result);
    }

    [TestMethod]
    public void ClassifyTopology_TwoExternalDisplays_ReturnsExtend()
    {
        var t1 = MakeTarget(1, 1);
        var t2 = MakeTarget(1, 2);
        var targets = new List<DisplayTargetId> { t1, t2 };
        var tech = new Dictionary<DisplayTargetId, DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY>
        {
            [t1] = DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.HDMI,
            [t2] = DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DisplayPortExternal,
        };

        var result = ClassifyTopology(targets, tech);

        Assert.AreEqual(DISPLAYCONFIG_TOPOLOGY_ID.Extend, result);
    }

    [TestMethod]
    public void GetTopologyFlags_Extend_UsesDatabaseTopologyWithoutSuppliedFlag()
    {
        const uint expected = 0x00000484;

        var result = GetTopologyFlags(DISPLAYCONFIG_TOPOLOGY_ID.Extend);

        Assert.AreEqual(expected, result);
        Assert.AreEqual(0u, result & 0x00000010);
        Assert.AreEqual(0u, result & 0x00000200);
    }

    [TestMethod]
    public void IsInternalTechnology_InternalFlag_ReturnsTrue()
    {
        Assert.IsTrue(IsInternalTechnology(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Internal));
    }

    [TestMethod]
    public void IsInternalTechnology_HdmiFlag_ReturnsFalse()
    {
        Assert.IsFalse(IsInternalTechnology(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.HDMI));
    }

    [TestMethod]
    public void TryActivateDisplays_EmptyList_ReturnsFalseWithMessage()
    {
        var result = TryActivateDisplays(new List<DisplayTargetId>(), out var message);

        Assert.IsFalse(result);
        Assert.IsNotNull(message);
        Assert.IsTrue(message.Length > 0);
    }

    [TestMethod]
    public void TryActivateDisplays_NonExistentTargets_ReturnsFalseWithMessage()
    {
        var fakeTargets = new List<DisplayTargetId>
        {
            MakeTarget(0xDEADBEEF, 0xDEADBEEF),
        };

        var result = TryActivateDisplays(fakeTargets, out var message);

        Assert.IsFalse(result);
        Assert.IsNotNull(message);
        Assert.IsTrue(message.Length > 0);
    }

    [TestMethod]
    public void SaveNamedProfile_EmptyTargets_ReturnsErrorMessage()
    {
        var result = SaveNamedProfile("test-profile-empty", new List<DisplayTargetId>());

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
        Assert.AreNotEqual(MonitorPowerExtension.Properties.Resources.profile_saved, result);
    }

    [TestMethod]
    public void ApplyNamedProfile_NonExistentFile_ReturnsErrorMessage()
    {
        var result = ApplyNamedProfile("nonexistent-profile-xyz-" + Guid.NewGuid().ToString("N") + ".json");

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void ResolveSavedProfileFileName_ProfileName_ReturnsFileName()
    {
        var profileName = "unit-test-reference-" + Guid.NewGuid().ToString("N")[..8];
        string? fileToDelete = null;

        try
        {
            fileToDelete = WriteSyntheticProfile(profileName);

            var result = ResolveSavedProfileFileName(profileName, referenceIsFileName: false);

            Assert.AreEqual(fileToDelete, result);
        }
        finally
        {
            if (fileToDelete is not null)
            {
                DeleteSavedProfile(fileToDelete);
            }
        }
    }

    [TestMethod]
    public void ResolveSavedProfileFileName_FileName_ReturnsFileName()
    {
        var profileName = "unit-test-file-reference-" + Guid.NewGuid().ToString("N")[..8];
        string? fileToDelete = null;

        try
        {
            fileToDelete = WriteSyntheticProfile(profileName);

            var result = ResolveSavedProfileFileName(fileToDelete, referenceIsFileName: true);

            Assert.AreEqual(fileToDelete, result);
        }
        finally
        {
            if (fileToDelete is not null)
            {
                DeleteSavedProfile(fileToDelete);
            }
        }
    }

    [TestMethod]
    public void ResolveSavedProfileFileName_FileNameWithPath_ReturnsNull()
    {
        var result = ResolveSavedProfileFileName(@"folder\profile.json", referenceIsFileName: true);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SaveNamedProfile_UnconnectedTarget_RejectsInvalidLayout()
    {
        var profileName = "unit-test-" + Guid.NewGuid().ToString("N")[..8];
        var targets = new List<DisplayTargetId> { MakeTarget(1, 1) };

        var result = SaveNamedProfile(profileName, targets);

        Assert.AreNotEqual(MonitorPowerExtension.Properties.Resources.profile_saved, result);
        Assert.IsFalse(File.Exists(Path.Combine(ProfilesDir, profileName + ".json")));
    }
}
