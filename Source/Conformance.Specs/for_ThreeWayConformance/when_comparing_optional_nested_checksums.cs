// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_optional_nested_checksums : Specification
{
    SemanticSpecificationRun[] _reference = [];
    SemanticSpecificationRunReport _stage = null!;
    ArtifactRenderPlan _rendered = null!;

    async Task Because()
    {
        var model = nested_checksum_cases.Compile(true);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        _reference = [.. plan.Specifications.Keys.Select(id => new SemanticSpecificationRunner().Run(plan, id))];
        _stage = await new SemanticSpecificationExecutor().Run(plan, new([]), new());
        _rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("NestedChecksums", "NestedChecksums"));
    }

    [Fact] void should_cover_valid_and_invalid_checksums() => _stage.Results.Count.ShouldEqual(2);
    [Fact] void should_pass_the_reference() => Assert.All(_reference, result => Assert.True(result.Passed, string.Join(';', result.Failures)));
    [Fact] void should_pass_stage() => Assert.All(_stage.Results, result => Assert.Equal(SemanticSpecificationOutcome.Passed, result.Outcome));
    [Fact] void should_refuse_optional_nested_checksum_rendering() => Assert.Contains(_rendered.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-005");
    [Fact] void should_emit_nothing() => _rendered.Artifacts.ShouldBeEmpty();
}
#endif
