// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text;
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
            ImplementationContents = [],
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var concept = loaded.Model.Application.Concepts.Single();
        _envelope = SemanticImplementationAdmission.Verify(_request).Single(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Artifact == concept.Id);
        var context = new SemanticApplicationContext(_request, new("Orders", "Orders"));
        _semantic = SemanticCratisAdmission.Evaluate(context, context.SelectedSlices()).Single(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Artifact == concept.Id);
    }

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_exercise_identical_refusals_from_independent_gates() => _semantic.ShouldEqual(_envelope);
    [Fact] void should_report_the_shared_refusal_once() => _plan.Diagnostics.Where(diagnostic => diagnostic.Code == _envelope.Code && diagnostic.Artifact == _envelope.Artifact && diagnostic.Message == _envelope.Message).ShouldContainOnly([_envelope with { Severity = ArtifactRenderDiagnosticSeverity.Warning }]);
    [Fact] void should_report_partial_rendering() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact]
    void should_retain_the_concept_with_a_throwing_factory_and_compile_retained_artifacts()
    {
        global::Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given.rendering_refusals.Warning(_plan, _envelope.Code, _envelope.Artifact, _envelope.Message);
        global::Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given.rendering_refusals.Retained(_plan, _envelope.Artifact);
        var concept = _plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Common/TenantOrderId.cs", StringComparison.Ordinal));
        var content = Encoding.UTF8.GetString(concept.Bytes.AsSpan());
        Assert.Contains("=> throw new global::System.NotImplementedException(", content, StringComparison.Ordinal);
        Assert.DoesNotContain(given.generation_rule_model.PortableBody, content, StringComparison.Ordinal);
        global::Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given.rendering_refusals.Compiles(_plan);
    }
}
#endif
