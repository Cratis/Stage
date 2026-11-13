// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;

/// <summary>
/// Compiles a fleet whose module provides a literal and a host value and whose view slice provides the vessel being read.
/// </summary>
internal static class a_fleet_model
{
    internal const string Source = """
        module Fleet
          provide region String from "Nordics"
          provide operator String optional from host

          feature Vessels
            slice StateChange RegisterVessel
              command RegisterVessel
                vesselId Uuid identifier
                name String
                produces VesselRegistered
                  for vesselId
                  name = name
              event VesselRegistered
                name String

            slice StateView Vessels
              provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId

              query VesselByKey => Vessel optional
                by vesselId Uuid

              readmodel Vessel
                vesselId Uuid key
                name String

              projection Vessels => Vessel
                from VesselRegistered
                  vesselId = $eventSourceId
                  name = name
        """;

    internal static SemanticCompilation Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("fleet"), "fleet", "Fleet.play", source);
        var result = new SemanticModelCompiler().Compile("Fleet", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        return result.Value!;
    }

    internal static ArtifactRenderPlan Plan(string source) => Plan(Compile(source));

    internal static ArtifactRenderPlan Plan(SemanticCompilation compilation) =>
        new CratisArtifactRenderPlanner().Plan(new(
            compilation.Model,
            SemanticExecutionPlan.Compile(compilation.Model, []).Plan!,
            CratisRendering.WithRealization(CratisRendering.CreateProfile(compilation.Model.Application.Name, new("Fleet", "Fleet")), compilation),
            new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id)));

    internal static string Text(ArtifactRenderPlan plan, string name) => a_model_with_named_sinks.Text(plan, name);
}
#endif
