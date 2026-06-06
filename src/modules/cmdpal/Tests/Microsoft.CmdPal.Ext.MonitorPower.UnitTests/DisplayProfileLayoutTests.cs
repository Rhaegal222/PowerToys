// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitorPowerExtension;

namespace Microsoft.CmdPal.Ext.MonitorPower.UnitTests;

[TestClass]
public class DisplayProfileLayoutTests
{
    private static readonly LUID Adapter = new() { LowPart = 1 };

    [TestMethod]
    public void AssignUniqueDeviceNames_UsesDistinctSourcesForInactiveTargets()
    {
        var target1 = new DisplayHelpers.DisplayTargetId(Adapter, 256);
        var target2 = new DisplayHelpers.DisplayTargetId(Adapter, 260);
        var target3 = new DisplayHelpers.DisplayTargetId(Adapter, 264);
        var candidates = new[]
        {
            new DisplayHelpers.DisplayNameCandidate(target1, @"\\.\DISPLAY1", true, 0),
            new DisplayHelpers.DisplayNameCandidate(target2, @"\\.\DISPLAY1", false, 0),
            new DisplayHelpers.DisplayNameCandidate(target2, @"\\.\DISPLAY2", false, 1),
            new DisplayHelpers.DisplayNameCandidate(target3, @"\\.\DISPLAY1", false, 0),
            new DisplayHelpers.DisplayNameCandidate(target3, @"\\.\DISPLAY2", false, 1),
            new DisplayHelpers.DisplayNameCandidate(target3, @"\\.\DISPLAY3", false, 2),
        };

        var result = DisplayHelpers.AssignUniqueDeviceNames(candidates);

        Assert.AreEqual(@"\\.\DISPLAY1", result[target1]);
        Assert.AreEqual(@"\\.\DISPLAY2", result[target2]);
        Assert.AreEqual(@"\\.\DISPLAY3", result[target3]);
    }

    [TestMethod]
    public void TryValidateLayout_RejectsDuplicateDeviceNames()
    {
        var layout = new[]
        {
            CreateTarget(256, @"\\.\DISPLAY1", 0, 0, true),
            CreateTarget(260, @"\\.\DISPLAY1", 1920, 0, false),
        };

        Assert.IsFalse(DisplayHelpers.TryValidateLayout(layout, null, out _));
    }

    [TestMethod]
    public void TryValidateLayout_RejectsOverlappingDisplays()
    {
        var layout = new[]
        {
            CreateTarget(256, @"\\.\DISPLAY1", 0, 0, true),
            CreateTarget(260, @"\\.\DISPLAY2", 1000, 0, false),
        };

        Assert.IsFalse(DisplayHelpers.TryValidateLayout(layout, null, out _));
    }

    [TestMethod]
    public void TryValidateLayout_AcceptsAdjacentDisplays()
    {
        var layout = new[]
        {
            CreateTarget(256, @"\\.\DISPLAY1", 0, 0, true),
            CreateTarget(260, @"\\.\DISPLAY2", 1920, 0, false),
        };

        Assert.IsTrue(DisplayHelpers.TryValidateLayout(layout, null, out _));
    }

    private static DisplayHelpers.SnapshotTarget CreateTarget(
        uint targetId,
        string deviceName,
        int x,
        int y,
        bool isPrimary)
        => new()
        {
            LowPart = Adapter.LowPart,
            HighPart = Adapter.HighPart,
            TargetId = targetId,
            DeviceName = deviceName,
            Width = 1920,
            Height = 1080,
            Frequency = 60,
            PositionX = x,
            PositionY = y,
            BitsPerPel = 32,
            IsPrimary = isPrimary,
        };
}
