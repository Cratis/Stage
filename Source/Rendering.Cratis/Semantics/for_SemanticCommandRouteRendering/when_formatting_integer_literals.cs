// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandRouteRendering;

public class when_formatting_integer_literals : Specification
{
    readonly List<UnsupportedSemanticRendering> _refusals = [];
    readonly List<string?> _ids = [];

    void Because()
    {
        var model = invoice_model.Compile(when_planning_event_source_routes.Source);
        var options = new CratisRenderingOptions("InvoiceApp", "InvoiceApp");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("InvoiceApp", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var command = context.Commands.Values.Single();
        foreach (var value in new[] { -9007199254740991m, 9007199254740991m, -9007199254740992m, 9007199254740992m })
        {
            var routed = command with { Route = command.Route! with { StreamId = SemanticExpression.FromValue(SemanticValue.Number(value)) } };
            var exception = Catch.Exception(() => _ids.Add(new SemanticCommandRouteRendering(routed, context).StreamId()));
            if (exception is not null) _refusals.Add(Assert.IsType<UnsupportedSemanticRendering>(exception));
        }
    }

    [Fact] void should_preserve_canonical_endpoint_bytes() => _ids.ShouldContainOnly(["\"-9007199254740991\"", "\"9007199254740991\""]);
    [Fact] void should_refuse_both_out_of_range_literals() => _refusals.Count.ShouldEqual(2);
    [Fact] void should_keep_the_typed_rendering_refusal() => _refusals.TrueForAll(refusal => refusal.Code == "STAGE-ESM-012").ShouldBeTrue();
}
