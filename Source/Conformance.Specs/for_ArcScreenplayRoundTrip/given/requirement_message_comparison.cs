// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// Only the round-trip comparison sees effective messages; raw recovery and the general comparer stay strict.
internal static class requirement_message_comparison
{
    public static ExecutableSemanticModel Project(ExecutableSemanticModel model) => ExecutableSemanticModel.Create(
        model.LanguageVersion,
        model.SemanticVersion,
        model.Application with { Modules = [.. model.Application.Modules.Select(Project)] });

    static SemanticModule Project(SemanticModule module) => module with { Features = [.. module.Features.Select(Project)] };

    static SemanticFeature Project(SemanticFeature feature) => feature with
    {
        Features = [.. feature.Features.Select(Project)],
        Slices = [.. feature.Slices.Select(Project)]
    };

    static SemanticSlice Project(SemanticSlice slice) => slice with { Commands = [.. slice.Commands.Select(Project)] };

    static SemanticCommand Project(SemanticCommand command) => command with
    {
        Requirements = [.. command.Requirements.Select(requirement => requirement with { Message = requirement.Message ?? SemanticRequirement.DefaultMessage })]
    };
}
#endif
