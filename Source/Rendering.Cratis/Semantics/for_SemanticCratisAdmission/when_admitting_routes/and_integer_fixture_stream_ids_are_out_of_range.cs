// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_routes;

public class and_integer_fixture_stream_ids_are_out_of_range : Specification
{
    readonly List<ArtifactRenderDiagnostic> _diagnostics = [];
    readonly List<bool> _appendRefusals = [];
    readonly List<bool> _appendCodecRefusals = [];
    int _evaluations;

    void Because()
    {
        Exercise(routed_specifications.Integer(), false);
        Exercise(routed_specifications.Composite(), true);
    }

    void Exercise(ExecutableSemanticModel model, bool composite)
    {
        var options = new CratisRenderingOptions("Routed", "Routed");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Routed", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var located = context.DeclaringSlice(context.Specifications.Values.Single().Id);
        var slice = located.Slice;
        var specification = slice.Specifications.Single();
        foreach (var value in new[] { -9007199254740992m, 9007199254740992m })
        {
            var route = specification.ThenEvents[0].Route!;
            var invalid = composite
                ? route with { StreamIdParts = [.. route.StreamIdParts.Select(part => part.Part == "month" ? part with { Value = SemanticValue.Number(value) } : part)] }
                : route with { StreamId = SemanticValue.Number(value) };
            var given = specification with { GivenEvents = [specification.GivenEvents[0] with { Route = invalid }] };
            var then = specification with { ThenEvents = [specification.ThenEvents[0] with { Route = invalid }] };
            var append = specification with
            {
                When = null,
                WhenAppended = new(specification.ThenEvents[0].EventContract, specification.ThenEvents[0].Values)
                {
                    EventSource = new(slice.Commands[0].Properties[0].Type, SemanticValue.Text("acc-1")),
                    Route = invalid
                }
            };
            foreach (var changed in new[] { given, then })
            {
                _evaluations++;
                _diagnostics.AddRange(SemanticCratisAdmission.Evaluate(context, [located with { Slice = slice with { Specifications = [changed] } }]).Where(diagnostic => diagnostic.Code == "STAGE-ESM-030"));
            }

            // Direct-append specifications are refused before rendering, independently of their route.
            _appendRefusals.Add(SemanticCratisAdmission.Evaluate(context, [located with { Slice = slice with { Specifications = [append] } }]).Any(diagnostic => diagnostic.Code == "STAGE-ESM-011"));
            _appendCodecRefusals.Add(!SemanticSpecificationRouteRendering.CanFormat(append.WhenAppended!.Route!, context));
        }
    }

    [Fact] void should_refuse_every_given_and_then_fixture() => _diagnostics.Count.ShouldEqual(_evaluations);
    [Fact] void should_keep_the_typed_route_refusal() => _diagnostics.TrueForAll(diagnostic => diagnostic.Code == "STAGE-ESM-030").ShouldBeTrue();
    [Fact] void should_block_rendering() => _diagnostics.TrueForAll(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_the_independent_direct_append_refusal() => _appendRefusals.TrueForAll(refused => refused).ShouldBeTrue();
    [Fact] void should_refuse_out_of_range_direct_append_fixture_ids_in_the_codec() => _appendCodecRefusals.TrueForAll(refused => refused).ShouldBeTrue();
}
