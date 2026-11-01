// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_builderless_state_views;

public class and_planning_is_not_strict : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderDiagnostic _warning = null!;

    void Because()
    {
        _plan = builderless_state_view_model.Plan(builderless_state_view_model.Builderless);
        _warning = _plan.Diagnostics.Single(_ => _.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct);
    }

    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_warn_without_failing() => _warning.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_name_the_read_model() => _warning.Message.ShouldContain("WelcomeRow");
    [Fact] void should_record_the_degradation_with_a_remedy() => _plan.Degradations.Single().Remedy.ShouldContain("WelcomeRow");
    [Fact] void should_record_the_warning_as_the_degradation_evidence() => _plan.Degradations.Single().Diagnostic.ShouldEqual(_warning);
    [Fact] void should_mark_the_read_model_degraded_in_the_inventory() => _plan.Inventory!.Constructs.Single(_ => _.Identity == _warning.Artifact).Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_retain_the_read_model_type_in_the_inventory() => _plan.Inventory!.Constructs.Single(_ => _.Identity == _warning.Artifact).Degradations.Single().PlaceholderKind.ShouldEqual(ArtifactRenderPlaceholderKind.RetainedSignatureOrType);
    [Fact] void should_emit_the_read_model_record() => Source(_plan).ShouldContain("record WelcomeRow");
    [Fact] void should_comment_the_degradation_on_the_read_model() => Source(_plan).ShouldContain("// STAGE-DEGRADE-002");

    [Fact]
    void should_compile_the_generated_backend()
    {
        var files = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var errors = RenderedOutput.Errors(files);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    static string Source(ArtifactRenderPlan plan) => string.Join('\n', plan.Artifacts.Select(_ => System.Text.Encoding.UTF8.GetString(_.Bytes.AsSpan())));
}
