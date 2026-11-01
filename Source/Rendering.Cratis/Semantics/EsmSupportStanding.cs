// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal sealed record EsmSupportStanding(EsmVersionStanding Version, ImmutableArray<SemanticUnknownMember> UnknownMembers)
{
    internal static EsmSupportStanding Evaluate(
        EsmSupport support,
        ExecutableSemanticModel model,
        IReadOnlyDictionary<string, SemanticSurfaceDisposition> ledger,
        SemanticUnknownMemberScope scope)
    {
        var standing = support.Classify(model.LanguageVersion, model.SemanticVersion);
        return new(standing, standing == EsmVersionStanding.NewerWithinRange ? SemanticUnknownMembers.Find(model, ledger, scope) : []);
    }
}
