// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticEventSourceArtifactRenderer.when_generating_event_sources;

public class and_there_are_no_sources : Specification
{
    ArtifactRenderPlan _legacy = null!;
    ArtifactRenderPlan _v7 = null!;
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        var source = when_planning_event_source_routes.Source;
        source = source.Remove(source.IndexOf("eventsource Account", StringComparison.Ordinal), source.IndexOf("module Banking", StringComparison.Ordinal) - source.IndexOf("eventsource Account", StringComparison.Ordinal))
            .Replace("        stream Account.Transactions\n          streamId = month\n", string.Empty, StringComparison.Ordinal);
        _model = invoice_model.Compile(source);
        _legacy = Plan(_model);
        var application = _model.Application with { Policies = [new("NotGuest", new SemanticNotPolicyCondition(new SemanticRoleCondition("Guest")))] };
        _model = ExecutableSemanticModel.Create(LanguageVersion.V7, SemanticVersion.V7, application);
    }
    void Because() => _v7 = Plan(_model);

    static ArtifactRenderPlan Plan(ExecutableSemanticModel model) => CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Banking", "Banking"));

    [Fact] void should_admit_v7() => _v7.Success.ShouldBeTrue();
    [Fact] void should_not_emit_definitions() => _v7.Artifacts.Any(artifact => artifact.RelativePath.StartsWith("EventSources/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_emit_an_unused_codec() => _v7.Artifacts.Any(artifact => artifact.RelativePath.StartsWith("GeneratedEventSources/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_preserve_every_legacy_artifact_byte() => _v7.Artifacts.All(artifact => _legacy.Artifacts.Single(previous => previous.RelativePath == artifact.RelativePath).Bytes.SequenceEqual(artifact.Bytes)).ShouldBeTrue();
}
