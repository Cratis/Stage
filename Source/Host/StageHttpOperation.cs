// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

/// <summary>
/// One modeled operation before any CLR types or Arc providers have been created.
/// </summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="CanonicalPath">The normalized slice-qualified route.</param>
/// <param name="LegacyPath">The normalized historical route.</param>
/// <param name="Kind">The operation kind.</param>
/// <param name="SliceId">The owning slice identity.</param>
/// <param name="QualifiedArtifact">The qualified command or query identity.</param>
internal sealed record StageHttpOperation(
    string Method,
    string CanonicalPath,
    string LegacyPath,
    string Kind,
    Guid SliceId,
    string QualifiedArtifact)
{
    internal string Description => $"{Kind} slice '{SliceId:D}' artifact '{QualifiedArtifact}'";
}
