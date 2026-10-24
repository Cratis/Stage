// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_PortableMatches.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_PortableMatches;

public class when_admitting_large_patterns : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_execute_the_reference_and_stage_and_render_counted_repeat_overflow(bool concept)
    {
        const string pattern = "(){999999999999999999999999999}";
        PortableMatchPattern.Check(pattern).Status.ShouldEqual(PortableMatchStatus.Admitted);
        var model = portable_match_cases.Compile(pattern, concept);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        foreach (var specification in plan.Specifications.Values)
        {
            new SemanticSpecificationRunner().Run(plan, specification.Id).Passed.ShouldBeTrue();
            var stage = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
            stage.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        }

        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("PortableMatches", "PortableMatches"));
        rendered.Success.ShouldBeTrue();
        rendered.Diagnostics.ShouldBeEmpty();
        var validator = rendered.Artifacts.Select(artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))
            .Single(text => text.Contains("PortableMatcher.Compile(", StringComparison.Ordinal) && text.Contains(pattern, StringComparison.Ordinal));
        validator.ShouldContain(pattern);
        validator.ShouldNotContain("RegularExpressions");
    }
}
#endif
