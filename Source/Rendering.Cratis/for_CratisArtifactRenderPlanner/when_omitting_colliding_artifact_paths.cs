// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_omitting_colliding_artifact_paths : given.a_register_project_render_request
{
    [Theory]
    [InlineData("Billing/Issue.cs")]
    [InlineData("billing/ISSUE.cs")]
    public void should_omit_both_owners_before_the_strict_contract_is_created(string conflictingPath)
    {
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        var artifacts = ArtifactPathAdmission.Admit(
            [
                PlannedArtifact.CreateText("Billing/Issue.cs", "public class First {}", [_registerProject.Id]),
                PlannedArtifact.CreateText(conflictingPath, "public class Second {}", [_projectLookup.Id]),
                PlannedArtifact.CreateText("Other.cs", "public class Other {}", [_model.Application.Id])
            ],
            _model.Application.Id,
            diagnostics);
        var gaps = RenderingGapReports.FromPathDiagnostics(diagnostics);
        RenderingGapReports.AddMarker(artifacts, gaps, _model.Application.Id);
        var plan = ArtifactRenderPlan.Create(_request, [.. artifacts], [.. diagnostics], gaps);

        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
        plan.Artifacts.Any(artifact => artifact.RelativePath.Equals("Billing/Issue.cs", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
        plan.Artifacts.Any(artifact => artifact.RelativePath == "Other.cs").ShouldBeTrue();
        plan.Artifacts.Any(artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md").ShouldBeTrue();
        gaps.Length.ShouldEqual(2);
        Assert.Contains(gaps, gap => gap.Owner == _registerProject.Id.ToString());
        Assert.Contains(gaps, gap => gap.Owner == _projectLookup.Id.ToString());
        diagnostics.Count.ShouldEqual(2);
        diagnostics.TrueForAll(diagnostic => diagnostic.Code == "STAGE-CRATIS-004" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldBeTrue();
#if DEBUG
        given.opaque_policy_model.Compile(plan).GetTypes().Any(type => type.Name == "Other").ShouldBeTrue();
#endif
    }
}
