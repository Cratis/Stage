---
title: Feedback in a play session
description: Let people using a staged application give feedback with the Showrunner Notes backend, aimed at a screen, module, feature, slice or component.
---

A published application can host the Showrunner Notes backend, which adds the feedback button to the application. A play
session (sandbox mode) can host it too, so reviewers of staged software give feedback the same way. It is **off** unless the
host asks for it: a sandbox without the setting below behaves exactly as before, with no Notes routes, no
`stage/model-references` route and no feedback button.

## Turn it on

| Setting | Meaning |
| --- | --- |
| `Stage:Sandbox:Notes:Enabled` | `true` hosts the Notes backend in the sandbox. Anything else, or leaving it out, keeps it off. |
| `Notes:ApplicationName`, `Notes:PublicationId` | Required. What the receiving side sees on every note. Use an identifier that names the staged session, not a published release. |
| `Notes:Transport` | `SameKernel` (default) or `Http`. With `Http`, also `Notes:CollectorUrl`, `Notes:CollectorTokenFile` (or `Notes:CollectorToken`) and `Notes:CollectorTenantId`. |
| `Stage:Sandbox:Notes:ModelReferencesFile` | Optional. A JSON file that maps `kind:Name` to the identifier the source model gave the element, for example `slice:Shop.Orders.PlaceOrder`. The keys are the same as for `--references` of the exporter, and a key the model does not have is refused. Without it notes still name the element, but carry no identifier. |

The `Notes` section is the `NotesOptions` of the Notes backend, shared with published mode, so every option it documents
works here. The host refuses to start, naming what is missing, when the options are incomplete.

When enabled, the host serves `stage/model-references` with `notesEnabled: true`, the Notes identity and the references. The
frontend renders the feedback button and the markers on screens, modules, features, slices and components from that answer.

## Who may give feedback

Access control is the Notes backend's `NotesOptions.AccessMode`, and in a sandbox the host decides it.

- `Everyone` lets every signed-in user give feedback. Use it with the `Http` transport and a collector token that only the
  invited reviewers' session receives: the collector token, not Stage, then bounds who can reach the collecting side.
- `GrantedUsers` lets only users a desk granted the right to. The Notes backend needs the `SameKernel` transport for it, because
  the desk's decisions arrive through Chronicle's outbox and inbox; it refuses `GrantedUsers` over `Http`. Stage passes the option
  straight through the `Notes` section, so a sandbox that shares a kernel with the desk can use it as soon as the pinned Notes
  version supports the option.

Published mode is unchanged and is configured with `Stage:Published:Notes:Enabled` as before.
