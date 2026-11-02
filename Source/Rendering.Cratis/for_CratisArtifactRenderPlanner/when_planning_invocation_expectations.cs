// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_invocation_expectations : Semantics.for_SemanticReactionSpecificationRenderer.given.an_invocation_expectation
{
    [Fact]
    void should_admit_the_v34_golden_through_the_full_planner()
    {
        var model = _context.Request.Model;
        var plan = new CratisArtifactRenderPlanner().Plan(new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, new("BackendHost", "BackendHost")), new(ArtifactRenderScopeKind.Application, model.Application.Id)));
        Assert.True(plan.Refusals.IsEmpty, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    void should_refuse_a_specification_without_any_outcomes() => AssertRefused(_specification with { ThenInvocations = [] });

    [Fact]
    void should_not_count_invocations_as_a_rejection_outcome() => AssertRefused(_specification with { ThenErrors = [new(null, "rejected")] });

    [Fact]
    void should_still_refuse_mismatched_events_without_invocation_expectations()
    {
        var expected = _specification.ThenInvocations.Single();
        AssertRefused(_specification with
        {
            When = new(expected.Command, expected.Values),
            WhenAppended = null,
            ThenInvocations = [],
            ThenEvents = [new(_specification.WhenAppended!.EventContract, _specification.WhenAppended.Values)]
        });
    }

    void AssertRefused(SemanticSpecification specification)
    {
        List<ArtifactRenderDiagnostic> diagnostics = [];
        SemanticSpecificationAdmission.Validate(_context, _located.Slice with { Specifications = [specification] }, diagnostics);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011");
    }
}
#endif
