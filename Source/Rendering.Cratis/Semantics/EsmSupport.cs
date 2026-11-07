// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Describes exact audited version pairs and the target's prospective compatibility range.
/// </summary>
/// <param name="Audited">The exact pairs whose semantics the target has audited.</param>
/// <param name="Accepts">The prospective compatibility range.</param>
internal sealed record EsmSupport(
    ImmutableArray<(LanguageVersion Language, SemanticVersion Semantics)> Audited,
    EsmVersionRange Accepts)
{
    /// <summary>
    /// Gets the audited pairs whose newer constructs the ledger still checks one by one: a model of such a pair is admitted, but a populated
    /// member the ledger gates (for instance the ESM v37 clock constructs inside a v38 model) is refused like a member of an unaudited pair.
    /// </summary>
    internal ImmutableArray<(LanguageVersion Language, SemanticVersion Semantics)> MemberScanned { get; init; } = [];

    /// <summary>
    /// Gets whether the populated members of a model of this pair are checked against the ledger.
    /// </summary>
    /// <param name="language">The language version.</param>
    /// <param name="semantic">The semantic version.</param>
    /// <returns>True when the pair is audited and member scanned.</returns>
    internal bool ScansMembers(LanguageVersion language, SemanticVersion semantic) => MemberScanned.Contains((language, semantic));

    internal EsmVersionStanding Classify(LanguageVersion language, SemanticVersion semantic)
    {
        if (Audited.Contains((language, semantic))) return EsmVersionStanding.Audited;
        if (language.Major != semantic.Major || language.Minor != semantic.Minor) return EsmVersionStanding.Malformed;
        if (semantic.Major >= Accepts.ExclusiveMajor) return EsmVersionStanding.FutureMajor;
        return Accepts.Contains(semantic) ? EsmVersionStanding.NewerWithinRange : EsmVersionStanding.Unadmitted;
    }
}
