# MonitorPower Command Palette proposal

## Summary

MonitorPower is a proposed display-topology workflow for PowerToys Command Palette. It is intended to complement Power Display, not duplicate it.

Power Display already covers per-monitor DDC/CI and VCP controls such as brightness, contrast, volume, input source, rotation, color temperature, power state, and profiles for those monitor settings.

MonitorPower focuses on Windows display setup workflows:

- "Play on TV"
- "Back to PC"
- choose which displays should stay active
- enable or disable displays
- restore primary monitor and layout
- optionally integrate with Windows display topology APIs or a profile-oriented backend

The initial PowerToys surface should be a small Command Palette entry that exposes accepted profile actions, not a full utility or a second display-control flyout.

This proposal intentionally starts as a spec rather than a feature implementation. Per `CONTRIBUTING.md`, new features should have an issue, conversation, and agreement on product fit and implementation approach before code is added.

## Problem

Users with a desktop monitor setup plus a TV often need to switch between normal desktop use and gaming or media use. Today that workflow commonly involves several manual steps:

- opening Windows display settings
- changing active displays
- changing the primary display
- applying a saved topology through an external utility
- turning unneeded displays off or disabling them

Power Display handles monitor controls, but it does not target higher-level Windows display topology workflows such as active display sets, PC/TV layout restoration, or primary display changes.

## Proposed direction

Use Command Palette as the first integration point. A top-level `MonitorPower` command would list a small set of user-defined or PowerToys-defined display setup profiles.

Example commands:

- `Play on TV`
- `Back to PC`
- `Apply selected display profile`
- `Choose active monitors`

The extension should initially act as an orchestration surface. The underlying implementation can be decided after maintainer feedback:

- Power Display profile extension, if topology actions are considered in scope there.
- Command Palette extension backed by shared PowerToys display-topology services.
- External or sample Command Palette extension, if this workflow should stay outside the main PowerToys binary.

The implementation should not depend on local scripts, local JSON files, or third-party binaries. The local prototype only demonstrates the workflow and planning logic.

## Command Palette shape

The existing Command Palette extension layout under `src/modules/cmdpal/ext/` uses standalone .NET projects. `SamplePagesExtension` shows the relevant shape:

- a project under `src/modules/cmdpal/ext/<ExtensionName>/`
- a `CommandProvider`
- top-level command items returned by `TopLevelCommands()`
- extension registration through manifest and COM-visible extension entry points
- packaging through the extension project manifest and MSIX tooling

`Microsoft.CmdPal.Ext.PowerToys` is also a relevant reference because it already exposes PowerToys module commands through Command Palette and includes module-specific command items, fallback commands, and settings/state refresh behavior.

If accepted for implementation, the likely project location would be:

```text
src/modules/cmdpal/ext/MonitorPowerExtension/
```

The first implementation should remain intentionally small:

- one top-level `MonitorPower` command
- a list page of display setup profiles
- one command per profile
- clear boundaries that avoid brightness, contrast, volume, input source, rotation, color temperature, monitor power-state sliders, and other Power Display responsibilities

## Prototype evidence

A local prototype outside the PowerToys repo validates the basic planning model:

```text
ToolEnabled       : True
KeepOnDdcIndexes  : {3}
TurnOffDdcIndexes : {1, 2, 4}
```

This means the user selects one or more monitors to keep active/on, and the planner derives the monitors that would be turned off or disabled. The prototype is intentionally not copied into PowerToys because it uses local scripts and machine-specific monitor mappings.

## Non-goals

- Reimplementing Power Display.
- Creating a second monitor-control flyout.
- Adding DDC/CI sliders to Command Palette.
- Shipping a dependency on local prototype scripts.
- Bundling third-party binaries such as MultiMonitorTool.

## Open questions

- Should topology profile actions belong in Power Display profiles instead of a Command Palette extension?
- Is there an existing internal PowerToys display abstraction that should own topology changes?
- Should this live under `Microsoft.CmdPal.Ext.PowerToys` as commands for a PowerToys module, or as a separate extension project under `src/modules/cmdpal/ext/`?
- Would maintainers prefer this as a built-in extension, a sample extension, or an external extension?
- What Windows display APIs should be preferred for active display sets, primary monitor, and layout restoration?
- What safety checks are required before disabling displays from PowerToys?

## Upstream process

This should not move directly to a feature PR without maintainer agreement. Per `CONTRIBUTING.md`, the next upstream step is to open or join an issue first, describe the workflow, and agree on the product surface and implementation approach before adding code.
