// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_dependency_omissions_reach_slice_siblings : given.a_register_project_render_request
{
    List<PlannedArtifact> _retained = [];
    List<ArtifactRenderDiagnostic> _diagnostics = [];
    RenderingPartAdmission _parts = null!;
    ArtifactRenderDiagnostic _original = null!;
    ArtifactRenderDiagnostic _languageWarning = null!;

    void Because()
    {
        var concept = _model.Application.Concepts.Single(item => item.Name == "ProjectId");
        var bridge = _model.Application.Concepts.Single(item => item.Name == "ProjectName");
        _original = new("STAGE-CRATIS-004", ArtifactRenderDiagnosticSeverity.Warning, "The declaration path is reserved.", concept.Id);
        _languageWarning = new("STAGE-ESM-019", ArtifactRenderDiagnosticSeverity.Warning, "Query language is not C#.", _projectLookup.Queries.Single().Id);
        _diagnostics = [_languageWarning, _original];
        _parts = new(new(_request, new("Example", "Projects")), _diagnostics);
        var original = new List<PlannedArtifact>
        {
            PlannedArtifact.CreateText("Missing.cs", "namespace Example; public record Missing;", [concept.Id]),
            PlannedArtifact.CreateText("Bridge.cs", "namespace Example; public record Bridge(Missing Value);", [bridge.Id]),
            PlannedArtifact.CreateText("Consumer.cs", "namespace Example; public record Consumer(Bridge Value);", [_registerProject.Commands.Single().Id]),
            PlannedArtifact.CreateText("Sibling.cs", "namespace Example; public record Sibling;", [_registerProject.Events.Single().Id]),
            PlannedArtifact.CreateText("Cascade.cs", "namespace Example; public record Cascade(Sibling Value);", [_projectLookup.ReadModels.Single().Id]),
            PlannedArtifact.CreateText("Survivor.cs", "namespace Example; public record Survivor;")
        };
        _retained = new ArtifactDependencyAdmission(_model.Application, _diagnostics, _parts).Admit(original, [.. original.Skip(1)]);
    }

    [Fact] void should_keep_the_independent_artifact() => Assert.Equal("Survivor.cs", Assert.Single(_retained).RelativePath);
    [Fact] void should_omit_the_consumer_slice() => Assert.True(_parts.IsOmitted(_registerProject.Id));
    [Fact] void should_omit_the_slice_sibling_without_a_direct_dependency() => Assert.True(_parts.IsOmitted(_registerProject.Events.Single().Id));
    [Fact] void should_recheck_dependencies_after_the_sibling_is_omitted() => Assert.True(_parts.IsOmitted(_projectLookup.Id));
    [Fact] void should_preserve_the_unrelated_language_warning() => Assert.Equal(_languageWarning, _diagnostics[0]);
    [Fact] void should_use_only_the_actual_omission_as_the_dependency_cause() => Assert.All(_diagnostics.Skip(2), diagnostic => Assert.True(diagnostic.Code == _original.Code && diagnostic.Message.EndsWith(_original.Message, StringComparison.Ordinal)));
    [Fact] void should_register_the_dependency_warnings_as_rendering_gaps() => Assert.All(_diagnostics.Skip(2), diagnostic => Assert.True(_parts.IsGap(diagnostic)));
    [Fact] void should_preserve_the_original_omission_diagnostic() => Assert.Equal(_original, _diagnostics[1]);
    [Fact] void should_produce_a_partial_plan() => Assert.Equal(ArtifactRenderOutcome.Partial, Plan().Outcome);
    [Fact] void should_report_the_consumer_as_a_rendering_gap() => Assert.Contains(Plan().ImplementationGaps, gap => gap.Kind == ArtifactImplementationGapKind.Rendering && gap.Owner == _registerProject.Commands.Single().Id.ToString());
#if DEBUG
    [Fact] void should_compile_the_retained_output() => Assert.Contains(given.opaque_policy_model.Compile(Plan()).GetTypes(), type => type.Name == "Survivor");
#endif

    ArtifactRenderPlan Plan()
    {
        var renderingGaps = RenderingGapReports.FromDiagnostics(_diagnostics.Where(diagnostic => _parts.IsGap(diagnostic) || RenderingGapReports.IsPathDiagnostic(diagnostic)));
        return ArtifactRenderPlan.Create(_request, [.. _retained], [.. _diagnostics], renderingGaps);
    }
}
