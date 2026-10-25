// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_resolving_rendering_diagnostic_owners : a_register_project_render_request
{
    [Fact]
    public void should_omit_the_containing_emission_for_a_nested_property_owner()
    {
        var owner = _registerProject.Commands.Single().Properties[0].Id;
        var original = new ArtifactRenderDiagnostic("STAGE-TEST-001", ArtifactRenderDiagnosticSeverity.Error, "Unsupported nested property.", owner);
        var diagnostics = new List<ArtifactRenderDiagnostic> { original };
        var parts = new RenderingPartAdmission(new(_request, _options), diagnostics);
        parts.AdmitDiagnostics([], []);
        Assert.Equal(original with { Severity = ArtifactRenderDiagnosticSeverity.Warning }, Assert.Single(diagnostics));
        Assert.True(parts.IsOmitted(owner));
        Assert.True(parts.IsOmitted(_registerProject.Id));
        Assert.True(parts.IsOmitted(_registerProject.Commands.Single().Id));
        Assert.True(parts.IsOmitted(_registerProject.Events.Single().Id));
        Assert.False(parts.IsOmitted(_projectLookup.Id));
        var invoked = false;
        Assert.Empty(parts.Render(_registerProject.Id, () =>
        {
            invoked = true;
            return [];
        }));
        Assert.False(invoked);
    }

    [Fact]
    public void should_omit_the_containing_slice_for_an_event_owner()
    {
        var owner = _registerProject.Events.Single().Id;
        var original = new ArtifactRenderDiagnostic("STAGE-TEST-001", ArtifactRenderDiagnosticSeverity.Error, "Unsupported event.", owner);
        var diagnostics = new List<ArtifactRenderDiagnostic> { original };
        var parts = new RenderingPartAdmission(new(_request, _options), diagnostics);
        parts.AdmitDiagnostics([], []);
        Assert.Equal(original with { Severity = ArtifactRenderDiagnosticSeverity.Warning }, Assert.Single(diagnostics));
        Assert.True(parts.IsOmitted(owner));
        Assert.True(parts.IsOmitted(_registerProject.Id));
        Assert.True(parts.IsOmitted(_registerProject.Commands.Single().Id));
        Assert.False(parts.IsOmitted(_projectLookup.Id));
    }

    [Fact]
    public void should_keep_an_unlocated_owner_as_an_error_without_a_gap()
    {
        var owner = SemanticId.Create(SemanticKind.Command, "not-in-the-model");
        var original = new ArtifactRenderDiagnostic("STAGE-TEST-002", ArtifactRenderDiagnosticSeverity.Error, "Unlocated owner.", owner);
        var diagnostics = new List<ArtifactRenderDiagnostic> { original };
        var parts = new RenderingPartAdmission(new(_request, _options), diagnostics);
        parts.AdmitDiagnostics([], []);
        Assert.Equal(original, Assert.Single(diagnostics));
        Assert.False(parts.IsGap(original));
        Assert.Equal(ArtifactRenderOutcome.Refused, ArtifactRenderPlan.Create(_request, [], [.. diagnostics]).Outcome);
    }

    [Fact]
    public void should_locate_nested_features_and_their_slice_owners()
    {
        var index = new SemanticOwnerIndex(_model.Application);
        Assert.True(index.Contains(_feature.Id));
        Assert.Contains(_registerProject.Id, index.Omissions(_feature.Id));
        Assert.Contains(_projectLookup.Id, index.Omissions(_module.Id));
    }
}
