// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Provider-declared prospective ESM range: matching language and semantic versions from a minimum up to an exclusive major.</summary>
/// <param name="Minimum">The inclusive minimum pair.</param>
/// <param name="ExclusiveMajor">The first major version outside the range.</param>
public sealed record ArtifactRenderDeclaredRange(ArtifactRenderVersionPair Minimum, uint ExclusiveMajor)
{
    internal bool IsValid => Minimum is not null && Minimum.Language.Major == Minimum.Semantics.Major &&
        Minimum.Language.Minor == Minimum.Semantics.Minor && ExclusiveMajor > Minimum.Semantics.Major;

    internal bool Contains(ArtifactRenderVersionPair pair) =>
        pair.Language.Major == pair.Semantics.Major && pair.Language.Minor == pair.Semantics.Minor &&
        pair.Semantics.Major < ExclusiveMajor &&
        (pair.Semantics.Major, pair.Semantics.Minor).CompareTo((Minimum.Semantics.Major, Minimum.Semantics.Minor)) >= 0;
}
