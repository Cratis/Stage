// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;

/// <summary>
/// The available screen model with explicit gates on the elements of the gated <c>VesselList</c> screen: an action bound to the
/// register command, gated against an audience the caller has not been given, and a data element bound to the vessel query.
/// </summary>
internal static class an_available_screen_element_model
{
    internal static SemanticCompilation Compile(bool conflicting = false)
    {
        var compilation = an_available_screen_model.Compile();
        var application = compilation.Model.Application;
        var module = application.Modules.Single();
        var registerSlice = module.Features.SelectMany(feature => feature.Slices).Single(slice => slice.Name == "RegisterVessel");
        var viewSlice = module.Features.SelectMany(feature => feature.Slices).Single(slice => slice.Name == "Vessels");
        var command = registerSlice.Commands.Single();
        var query = viewSlice.Queries.Single();
        var screen = viewSlice.Screens.Single(candidate => candidate.Name == "VesselList");
        ImmutableArray<SemanticAvailabilityRequirement> incident = [new SemanticAudienceRequirement("Beta", Negated: true)];
        var actions = conflicting
            ? ImmutableArray.Create(Action(command, incident), Action(command, [new SemanticAudienceRequirement("Plus")]))
            : ImmutableArray.Create(Action(command, incident));
        var gated = screen with
        {
            Actions = actions,
            Data = [new SemanticScreenData(query.Id, []) { Available = [new SemanticAudienceRequirement("Plus")] }]
        };
        var updated = viewSlice with { Screens = [.. viewSlice.Screens.Select(candidate => candidate.Id == screen.Id ? gated : candidate)] };
        module = module with
        {
            Features = [.. module.Features.Select(feature => feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == viewSlice.Id ? updated : slice)] })]
        };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V59, SemanticVersion.V59, application with { Modules = [module] });
        return SemanticCompilation.Create(model, compilation.Documents, compilation.SourceMap);
    }

    static SemanticScreenAction Action(SemanticCommand command, ImmutableArray<SemanticAvailabilityRequirement> available) =>
        new(command.Id, "Register", "screen", []) { Available = available };
}
#endif
