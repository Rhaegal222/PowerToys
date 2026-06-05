// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CmdPal.Ext.MonitorPower.UnitTests;

[TestClass]
public class MonitorPowerCommandsProviderTests
{
    [TestMethod]
    public void Provider_HasDisplayName()
    {
        var provider = new MonitorPowerExtension.MonitorPowerCommandsProvider();

        Assert.IsNotNull(provider.DisplayName);
        Assert.IsTrue(provider.DisplayName.Length > 0);
    }

    [TestMethod]
    public void Provider_HasIcon()
    {
        var provider = new MonitorPowerExtension.MonitorPowerCommandsProvider();

        Assert.IsNotNull(provider.Icon);
    }

    [TestMethod]
    public void Provider_TopLevelCommandsNotEmpty()
    {
        var provider = new MonitorPowerExtension.MonitorPowerCommandsProvider();

        var commands = provider.TopLevelCommands();

        Assert.IsNotNull(commands);
        Assert.IsTrue(commands.Length > 0);
    }
}
