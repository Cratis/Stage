// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;

internal static class an_available_screen_model
{
    internal static SemanticCompilation Compile()
    {
        var compilation = a_fleet_model.Compile(an_available_fleet_model.Source + "\n\n" + """
              feature Public
                slice StateView PublicList
                  query PublicVessels => PublicVessel[]

                  readmodel PublicVessel
                    vesselId Uuid key
                    name String

                  projection PublicVessels => PublicVessel
                    from VesselRegistered
                      vesselId = $eventSourceId
                      name = name
            """);
        var application = compilation.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single(feature => feature.Name == "Vessels");
        var publicFeature = module.Features.Single(feature => feature.Name == "Public");
        var view = feature.Slices.Single(slice => slice.Name == "Vessels");
        var catalog = compilation.Documents.IdentityCatalog;
        var viewAddress = SemanticAddress.ForSlice(catalog.Application, module.Name, feature.Name, view.Name);
        var publicViewAddress = SemanticAddress.ForSlice(catalog.Application, module.Name, "Public", "PublicList");
        var gatedView = view with
        {
            Screens =
            [
                Screen(catalog, viewAddress, "VesselList") with { Available = [new SemanticAudienceRequirement("Plus")] },
                Screen(catalog, viewAddress, "InheritedList")
            ]
        };
        var publicView = publicFeature.Slices.Single() with
        {
            Screens = [Screen(catalog, publicViewAddress, "PublicList")]
        };
        module = module with
        {
            Features =
            [
                feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == view.Id ? gatedView : slice)] },
                publicFeature with { Slices = [publicView] }
            ]
        };
        var model = ExecutableSemanticModel.Create(compilation.Model.LanguageVersion, compilation.Model.SemanticVersion, application with { Modules = [module] });
        return SemanticCompilation.Create(model, compilation.Documents, compilation.SourceMap);
    }

    static SemanticScreen Screen(SemanticIdentityCatalog catalog, SemanticAddress slice, string name) =>
        new(catalog.ResolveSemantic(SemanticAddress.ForScreen(slice, name)), name, name, [], [], [], new([], []));
}
#endif
