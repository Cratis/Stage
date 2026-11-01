// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_the_mapping_is_a_plain_property : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderDiagnostic _warning = null!;

    void Because()
    {
        _plan = event_context_projection_model.Plan(event_context_projection_model.Plain);
        _warning = _plan.Diagnostics.Single(_ => _.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct);
    }

    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_have_no_errors() => _plan.Diagnostics.Any(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_warn_without_failing() => _warning.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_name_both_omitted_values() => _warning.Message.ShouldContain("$eventContext.occurred");
    [Fact] void should_name_the_correlation_value() => _warning.Message.ShouldContain("$eventContext.correlationId");
    [Fact] void should_record_the_degradation_with_a_remedy_citing_the_issue() => _plan.Degradations.Single().Remedy.ShouldContain("Stage#876");
    [Fact] void should_mark_the_read_model_degraded_in_the_inventory() => _plan.Inventory!.Constructs.Single(_ => _.Identity == _warning.Artifact).Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_comment_the_omitted_occurrence() => Source(_plan).ShouldContain("// STAGE-DEGRADE-002: $eventContext.occurred mapping omitted (Stage#876)");
    [Fact] void should_comment_the_omitted_correlation() => Source(_plan).ShouldContain("// STAGE-DEGRADE-002: $eventContext.correlationId mapping omitted (Stage#876)");
    [Fact] void should_still_render_the_literal_mapping() => Source(_plan).ShouldContain("\"global\"");
    [Fact] void should_still_render_the_nested_literal() => Source(_plan).ShouldContain("Note");
    [Fact] void should_not_render_an_event_context_mapping() => Source(_plan).ShouldNotContain("ToEventContextProperty");

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
