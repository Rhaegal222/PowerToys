// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using MonitorPowerExtension.Properties;

#pragma warning disable CA1305, CA1863

namespace MonitorPowerExtension.Pages;

internal sealed partial class CreateProfilePage : DynamicListPage
{
    private readonly List<TargetState> _targets;

    public CreateProfilePage()
    {
        Name = Resources.create_profile_page_title;
        Title = Resources.create_profile_page_title;
        PlaceholderText = Resources.profile_name_placeholder;
        _targets = LoadTargets();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) => RaiseItemsChanged(0);

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>
        {
            new ListItem(new SaveProfileCommand(this))
            {
                Title = Resources.save_profile_button,
                Subtitle = string.IsNullOrWhiteSpace(SearchText) ? Resources.profile_name_required : SearchText,
                Icon = new IconInfo("\uE74E"),
            },
        };

        if (_targets.Count == 0)
        {
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = Resources.no_active_displays,
                Subtitle = Resources.error_no_active_to_save,
                Icon = new IconInfo("\uE783"),
            });
            return [.. items];
        }

        items.AddRange(_targets.Select((target, index) => new ListItem(new ToggleTargetCommand(this, index))
        {
            Title = target.Name,
            Subtitle = target.KeepOn ? Resources.will_stay_on : Resources.will_turn_off,
            Icon = new IconInfo(target.KeepOn ? "\uE7F4" : "\uE783"),
        }));

        return [.. items];
    }

    private static List<TargetState> LoadTargets()
    {
        try
        {
            var activeTargets = DisplayHelpers.GetActivePaths().paths
                .Select(p => new DisplayHelpers.DisplayTargetId(p.targetInfo.adapterId, p.targetInfo.id))
                .ToHashSet();

            return DisplayHelpers.GetAllPaths()
                .GroupBy(p => new DisplayHelpers.DisplayTargetId(p.targetInfo.adapterId, p.targetInfo.id))
                .Select(g =>
                {
                    var path = g.First();
                    var id = new DisplayHelpers.DisplayTargetId(path.targetInfo.adapterId, path.targetInfo.id);
                    return new TargetState(id, GetTargetName(path.targetInfo), activeTargets.Contains(id));
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private void ToggleTarget(int index)
    {
        if ((uint)index >= (uint)_targets.Count)
        {
            return;
        }

        _targets[index].KeepOn = !_targets[index].KeepOn;
        RaiseItemsChanged(0);
    }

    private ICommandResult SaveProfile()
    {
        var name = SearchText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            ExtensionHost.ShowStatus(
                new StatusMessage() { Message = Resources.profile_name_required, State = MessageState.Error },
                StatusContext.Extension);
            return CommandResult.KeepOpen();
        }

        var selectedTargets = _targets
            .Where(t => t.KeepOn)
            .Select(t => t.Id)
            .ToList();

        if (selectedTargets.Count == 0)
        {
            ExtensionHost.ShowStatus(
                new StatusMessage() { Message = Resources.profile_no_monitors, State = MessageState.Error },
                StatusContext.Extension);
            return CommandResult.KeepOpen();
        }

        try
        {
            var msg = DisplayHelpers.SaveNamedProfile(name, selectedTargets);
            var isError = msg.StartsWith(Resources.error_prefix, StringComparison.OrdinalIgnoreCase);
            ExtensionHost.ShowStatus(
                new StatusMessage() { Message = msg, State = isError ? MessageState.Error : MessageState.Success },
                StatusContext.Extension);
            return isError ? CommandResult.KeepOpen() : CommandResult.GoHome();
        }
        catch (Exception ex)
        {
            ExtensionHost.ShowStatus(
                new StatusMessage() { Message = string.Format(Resources.error_format, ex.Message), State = MessageState.Error },
                StatusContext.Extension);
            return CommandResult.KeepOpen();
        }
    }

    private static string GetTargetName(DISPLAYCONFIG_PATH_TARGET_INFO targetInfo)
    {
        try
        {
            return DisplayHelpers.GetTargetFriendlyName(targetInfo);
        }
        catch
        {
            return Resources.unknown_display;
        }
    }

    private sealed class TargetState(DisplayHelpers.DisplayTargetId id, string name, bool keepOn)
    {
        public DisplayHelpers.DisplayTargetId Id { get; } = id;

        public string Name { get; } = name;

        public bool KeepOn { get; set; } = keepOn;
    }

    private sealed partial class ToggleTargetCommand(CreateProfilePage page, int index) : InvokableCommand
    {
        public override ICommandResult Invoke()
        {
            page.ToggleTarget(index);
            return CommandResult.KeepOpen();
        }
    }

    private sealed partial class SaveProfileCommand(CreateProfilePage page) : InvokableCommand
    {
        public override ICommandResult Invoke() => page.SaveProfile();
    }
}
