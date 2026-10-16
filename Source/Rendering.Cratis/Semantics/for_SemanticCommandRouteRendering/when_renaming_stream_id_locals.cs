// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandRouteRendering;

public class when_renaming_stream_id_locals : Specification
{
    string? _streamId;
    string _checks = null!;

    void Because()
    {
        // Route-plus-generation admission belongs to PR3; exercise the route renderer directly here.
        var source = when_planning_event_source_routes.Source.Replace("amount Decimal\n", "streamIdPart0 AccountId generated\n        streamIdPart01 AccountId generated\n        amount Decimal\n", StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        var options = new CratisRenderingOptions("Banking", "Banking");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Banking", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var command = context.Commands.Values.Single();
        var routing = new SemanticCommandRouteRendering(command, context, new(command));
        _streamId = routing.StreamId();
        var builder = new CSharpCodeBuilder();
        routing.RenderChecks(builder, "Result");
        _checks = builder.ToString();
    }

    [Fact] void should_skip_both_reserved_parameter_names() => _streamId.ShouldEqual("streamIdPart02");
    [Fact] void should_use_the_same_name_in_the_route_check() => _checks.ShouldContain("out var streamIdPart02");
}
