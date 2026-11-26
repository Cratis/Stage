// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// The bounded promotion in Screenplay v6.38.1 SemanticModelBinder.PromoteV2Destinations, including nested features.
internal static class round_trip_command_destinations
{
    public static SemanticApplication Promote(SemanticApplication application) => application with
    {
        Modules = [.. application.Modules.Select(module => module with { Features = [.. module.Features.Select(Promote)] })]
    };

    static SemanticFeature Promote(SemanticFeature feature) => feature with
    {
        Features = [.. feature.Features.Select(Promote)],
        Slices = [.. feature.Slices.Select(slice => slice with { Commands = [.. slice.Commands.Select(Promote)] })]
    };

    static SemanticCommand Promote(SemanticCommand command)
    {
        if (command.Destination is not null || HasImplicitGeneratedDestination(command)) return command;

        var source = command.Produces.Where(produced => IsDefaultDestination(command, produced)).Select(produced => produced.Destination)
            .OfType<SemanticResolvedExpression>().FirstOrDefault();
        if (source is null) return command;

        var property = command.Properties.Single(value => value.Id == source.Target);
        return command with { Destination = new(property.Type, source) };
    }

    static bool IsDefaultDestination(SemanticCommand command, SemanticProducedEvent produced) =>
        produced.Route is null || (produced.Destination is SemanticResolvedExpression expression &&
            command.Properties.Any(property => property.Id == expression.Target && property.IsIdentifier));

    static bool HasImplicitGeneratedDestination(SemanticCommand command) =>
        command.Properties.Any(property => property.IsGenerated && property.IsIdentifier) && command.Produces.Any(produced => produced.Destination is null);
}
#endif
