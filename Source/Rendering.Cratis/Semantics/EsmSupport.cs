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
    internal EsmVersionStanding Classify(LanguageVersion language, SemanticVersion semantic)
    {
        if (Audited.Contains((language, semantic))) return EsmVersionStanding.Audited;
        if (language.Major != semantic.Major || language.Minor != semantic.Minor) return EsmVersionStanding.Malformed;
        if (semantic.Major >= Accepts.ExclusiveMajor) return EsmVersionStanding.FutureMajor;
        return Accepts.Contains(semantic) ? EsmVersionStanding.NewerWithinRange : EsmVersionStanding.Unadmitted;
    }
}
