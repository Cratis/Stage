// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_root_joins_with_explicit_keys;

public class and_the_key_shape_is_unsupported : Specification
{
    [Fact]
    public void should_reject_an_event_context_key_before_planning()
    {
        var model = a_root_join_model.Create(_ => new SemanticProjectionValueKey(new SemanticProjectionEventContextValue("correlationId")));
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.Null(execution.Plan);
        Assert.NotEmpty(execution.Issues);
    }

    [Theory]
    [InlineData("literal")]
    [InlineData("source")]
    [InlineData("child")]
    public void should_refuse_without_emitting_artifacts(string shape)
    {
        SemanticProjectionKey Change(SemanticProjectionKey key) => shape switch
        {
            "literal" => new SemanticProjectionValueKey(SemanticProjectionValue.Literal(SemanticValue.Text("5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"))),
            _ => new SemanticProjectionValueKey(SemanticProjectionValue.EventSourceIdentity)
        };
        var model = a_root_join_model.Create(Change, child: shape == "child");
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.NotNull(execution.Plan);
        var plan = CratisRendering.Plan(model, execution.Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017");
    }
}
