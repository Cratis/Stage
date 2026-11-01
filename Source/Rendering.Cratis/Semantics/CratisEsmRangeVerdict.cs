// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal sealed record CratisEsmRangeVerdict(
    EsmSupportStanding Standing,
    ImmutableArray<ArtifactRenderDiagnostic> Diagnostics,
    ImmutableArray<ArtifactRenderDegradation> Evidence)
{
    internal bool IsAdmitted => Standing.Version is EsmVersionStanding.Audited or EsmVersionStanding.NewerWithinRange &&
        !Diagnostics.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
}
