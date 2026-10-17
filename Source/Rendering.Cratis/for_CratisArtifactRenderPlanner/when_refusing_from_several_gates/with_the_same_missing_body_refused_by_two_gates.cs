// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_from_several_gates;

public class with_the_same_missing_body_refused_by_two_gates : Specification
{
    ArtifactRenderRequest _request = null!;
    ArtifactRenderDiagnostic _envelope = null!;
    ArtifactRenderDiagnostic _semantic = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var loaded = given.generation_rule_model.Load();
        _request = new(loaded.Model, loaded.Plan, CratisRendering.CreateProfile(loaded.Model.Application.Name, new("Orders", "Orders")), new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = ImmutableDictionary<string, string>.Empty,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var concept = loaded.Model.Application.Concepts.Single();
        _envelope = SemanticImplementationAdmission.Verify(_request).Single(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Artifact == concept.Id);
        var context = new SemanticApplicationContext(_request, new("Orders", "Orders"));
        _semantic = SemanticCratisAdmission.Evaluate(context, context.SelectedSlices()).Single(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Artifact == concept.Id);
    }

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_exercise_identical_refusals_from_independent_gates() => _semantic.ShouldEqual(_envelope);
    [Fact] void should_report_the_shared_refusal_once() => _plan.Diagnostics.Where(diagnostic => diagnostic.Code == _envelope.Code && diagnostic.Artifact == _envelope.Artifact && diagnostic.Message == _envelope.Message).ShouldContainOnly([_envelope]);
    [Fact] void should_report_failure() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
#endif
