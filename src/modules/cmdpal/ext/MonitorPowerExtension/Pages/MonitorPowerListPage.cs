// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using MonitorPowerExtension.Properties;

#pragma warning disable CA1305, CA1863

namespace MonitorPowerExtension.Pages;

internal sealed partial class MonitorPowerListPage : ListPage
{
    private static readonly IconInfo AppIcon = new("\uE7F4");

    public MonitorPowerListPage()
    {
        Icon = AppIcon;
        Title = Resources.page_title;
        Name = Resources.page_title;
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>
        {
            new ListItem(new CommandItem(new CreateProfilePage()))
            {
                Title = Resources.save_profile_title,
                Subtitle = Resources.save_profile_subtitle,
                Icon = new IconInfo("\uE81C"),
            },
        };

        foreach (var (fileName, profileName) in DisplayHelpers.GetSavedProfiles())
        {
            items.Add(new ListItem(new ApplySavedProfileCommand(fileName))
            {
                Title = profileName,
                Subtitle = Resources.apply_profile_subtitle,
                Icon = new IconInfo("\uE81C"),
                MoreCommands =
                [
                    new CommandContextItem(new DeleteSavedProfileCommand(fileName))
                    {
                        Title = Resources.delete_profile_title,
                        Icon = new IconInfo("\uE74D"),
                        IsCritical = true,
                    },
                ],
            });
        }

        return [.. items];
    }

    private sealed partial class ApplySavedProfileCommand : InvokableCommand
    {
        private readonly string _fileName;

        public ApplySavedProfileCommand(string fileName)
        {
            _fileName = fileName;
            Name = Resources.apply_profile_title;
        }

        public override CommandResult Invoke()
        {
            var msg = DisplayHelpers.ApplyNamedProfile(_fileName);
            var isError = msg.StartsWith(Resources.error_prefix, StringComparison.OrdinalIgnoreCase);
            ExtensionHost.ShowStatus(
                new StatusMessage() { Message = msg, State = isError ? MessageState.Error : MessageState.Success },
                StatusContext.Extension);
            return CommandResult.KeepOpen();
        }
    }

    private sealed partial class DeleteSavedProfileCommand : InvokableCommand
    {
        private readonly string _fileName;

        public DeleteSavedProfileCommand(string fileName)
        {
            _fileName = fileName;
            Name = Resources.delete_profile_title;
        }

        public override CommandResult Invoke()
        {
            DisplayHelpers.DeleteSavedProfile(_fileName);
            ExtensionHost.ShowStatus(
                new StatusMessage() { Message = Resources.profile_deleted, State = MessageState.Success },
                StatusContext.Extension);
            return CommandResult.GoHome();
        }
    }
}
