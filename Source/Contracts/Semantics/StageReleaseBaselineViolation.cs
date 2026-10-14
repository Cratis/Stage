// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// Describes a changed or missing admitted revision, matched only by contract identity and revision.
/// </summary>
/// <param name="Kind">Screenplay's BaselinedRevisionChanged or BaselinedRevisionMissing.</param>
/// <param name="ContractId">The admitted event contract identity.</param>
/// <param name="Revision">The admitted revision number.</param>
/// <param name="StoredName">The effective stored event type name at admission.</param>
/// <param name="DisplayHint">The last authored address, for display only.</param>
/// <param name="AdmittedIn">The original admitted release label.</param>
/// <param name="Path">The first differing resolved-schema path, if changed.</param>
public sealed record StageReleaseBaselineViolation(
    string Kind,
    EventContractId ContractId,
    uint Revision,
    string StoredName,
    ModelNotesAddress DisplayHint,
    string AdmittedIn,
    string? Path);
