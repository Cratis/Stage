// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_omitting_transitive_artifact_dependencies : given.a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        var artifacts = new List<PlannedArtifact>
        {
            PlannedArtifact.CreateText("Contracts/Item.cs", "namespace Lost; public record Item;", [_registerProject.Id]),
            PlannedArtifact.CreateText("contracts/ITEM.cs", "namespace Lost; public record OtherItem;", [_projectLookup.Id]),
            PlannedArtifact.CreateText("Consumer.cs", "using Lost; public record Consumer(Item Value);", [_registerProject.Commands.Single().Id]),
            PlannedArtifact.CreateText("Transitive.cs", "public record Transitive(Consumer Value);", [_projectLookup.ReadModels.Single().Id]),
            PlannedArtifact.CreateText("NamespaceOnly.cs", "using Lost; public record NamespaceOnly;", [_model.Application.Id]),
            PlannedArtifact.CreateText("BItem.cs", "namespace Retained; public record Item;", [_model.Application.Id]),
            PlannedArtifact.CreateText("Independent.cs", "public record Independent(Retained.Item Value);", [_model.Application.Id])
        };
        var admitted = ArtifactPathAdmission.Admit(artifacts, _model.Application.Id, diagnostics);
        admitted = ArtifactDependencyAdmission.Admit(artifacts, admitted, _model.Application.Id, diagnostics);
        var gaps = RenderingGapReports.FromPathDiagnostics(diagnostics);
        RenderingGapReports.AddMarker(admitted, gaps, _model.Application.Id);
        _plan = ArtifactRenderPlan.Create(_request, [.. admitted], [.. diagnostics], gaps);
    }

    [Fact] void should_omit_direct_and_transitive_consumers() => _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => artifact.RelativePath).ShouldContainOnly(["BItem.cs", "Independent.cs"]);
    [Fact] void should_keep_the_original_warning_code_for_dependent_owners() => _plan.Diagnostics.All(diagnostic => diagnostic.Code == "STAGE-CRATIS-004" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_record_every_omitted_owner() => _plan.ImplementationGaps.Length.ShouldEqual(5);
    [Fact] void should_mark_the_result_partial() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
#if DEBUG
    [Fact] void should_compile_the_remaining_artifacts() => given.opaque_policy_model.Compile(_plan).GetTypes().Any(type => type.Name == "Independent").ShouldBeTrue();
#endif
}
