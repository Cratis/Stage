// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_routes;

public class and_a_fixture_stream_id_is_unformattable : Specification
{
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;
    SemanticId _specification;

    void Because()
    {
        var model = routed_specifications.Compile();
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single();
        _specification = specification.Id;
        var given = specification.GivenEvents.Single();
        var changed = slice with
        {
            Specifications = [specification with { GivenEvents = [given with { Route = given.Route! with { StreamId = SemanticValue.Text(string.Empty) } }] }]
        };
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var options = new CratisRenderingOptions("Routed", "Routed");
        var request = new ArtifactRenderRequest(model, plan, CratisRendering.CreateProfile("Routed", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);

        // ESM construction already refuses empty fixture identities. Exercise Stage's defensive admission
        // directly with a selected slice rather than bypassing the upstream contract validator.
        _diagnostics = SemanticCratisAdmission.Evaluate(context, [context.Slice(slice.Id) with { Slice = changed }]);
    }

    [Fact] void should_refuse_the_fixture_route() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-030"]);
    [Fact] void should_identify_the_specification() => _diagnostics.Single().Artifact.ShouldEqual(_specification);
    [Fact] void should_block_rendering() => _diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
}
