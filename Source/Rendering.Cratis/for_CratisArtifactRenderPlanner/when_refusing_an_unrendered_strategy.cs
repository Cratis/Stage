// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_an_unrendered_strategy : Specification
{
    [Theory]
    [InlineData("Id")]
    [InlineData("Receipt")]
    public void should_refuse_without_falling_back_to_uuid(string name)
    {
        var model = a_generation_model.Model();
        var execution = SemanticExecutionPlan.Compile(model).Plan!;

        // Simulate a future compiler admitting a strategy this target has not audited.
        var generation = model.Application.Concepts.Single(concept => concept.Name == name).Generation!;
        typeof(SemanticGeneration).GetField("<Kind>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(generation, "future");
        var plan = CratisRendering.Plan(model, execution, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GenerationApp", "GenerationApp"));
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-028");
        plan.Artifacts.ShouldBeEmpty();
    }
}
