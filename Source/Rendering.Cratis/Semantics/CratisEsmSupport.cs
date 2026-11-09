// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Provides the internal source of truth for audited Cratis ESM support and its prospective range.
/// </summary>
internal static class CratisEsmSupport
{
    internal static EsmSupport Descriptor { get; } = new(
        [.. EsmSchemaV30Support.LanguageVersions.Zip(EsmSchemaV30Support.SemanticVersions), (LanguageVersion.V31, SemanticVersion.V31), (LanguageVersion.V32, SemanticVersion.V32), (LanguageVersion.V33, SemanticVersion.V33), (LanguageVersion.V34, SemanticVersion.V34), (LanguageVersion.V36, SemanticVersion.V36), (LanguageVersion.V37, SemanticVersion.V37), (LanguageVersion.V38, SemanticVersion.V38), (LanguageVersion.V40, SemanticVersion.V40), (LanguageVersion.V41, SemanticVersion.V41)],
        new(SemanticVersion.V32, 33))
    {
        // ESM v37 is the command clock and v38 graphs, v40 graph queries, which the sandbox runs and the renderer does not render yet (STAGE-ESM-068). The clock members stay refused by the ledger (STAGE-ESM-016), per owning command or specification at run time, so these pairs scan their members.
        MemberScanned = [(LanguageVersion.V37, SemanticVersion.V37), (LanguageVersion.V38, SemanticVersion.V38), (LanguageVersion.V40, SemanticVersion.V40), (LanguageVersion.V41, SemanticVersion.V41)]
    };
}
