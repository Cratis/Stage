// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_release_baseline;

public class with_strict_acceptance : given.a_release_baseline_request
{
    ArtifactRenderPlan _original = null!;
    ArtifactRenderPlan _rebuilt = null!;
    ImmutableArray<ArtifactRenderDiagnostic> _warnings;

    void Because()
    {
        var identity = _request.Model.Application.Id;
        var locator = SemanticAddress.ForApplication(ApplicationIdentity.Create(_request.Model.Application.Name));
        var warning = new ArtifactRenderDiagnostic("STAGE-DEGRADE-002", ArtifactRenderDiagnosticSeverity.Warning, "Implementation described.", identity);
        var evidence = new ArtifactRenderDegradation(warning, "Implement and verify.");
        var construct = new ArtifactRenderConstruct(identity, ArtifactRenderConstructKind.Command, "model/orders.play", ArtifactRenderOutcome.Degraded)
        {
            SemanticLocator = locator,
            Degradations = [new(evidence, ArtifactRenderPlaceholderKind.ThrowingOperationBody, warning.Message, "model/orders.play") { SemanticLocator = locator }]
        };
        var artifact = PlannedArtifact.CreateText("Orders/Model.cs", "// model", [identity]);
        _original = ArtifactRenderPlan.Create(_request, [artifact], [warning], [], [], [new("Orders/Old.cs", new string('a', 64), identity)])
            .WithInventory(ArtifactRenderInventory.Create([construct])).WithAcceptance([evidence], strict: true);
        _warnings = ReleaseBaselineWarnings.For(WithBaseline(_assessment));
        _rebuilt = CratisPlanDiagnosticReconstruction.Append(_request, _original, _warnings);
    }

    [Fact] void should_reject_strict_acceptance() => Assert.False(_rebuilt.Success);
    [Fact] void should_keep_generation_safe() => Assert.True(_rebuilt.CanPublishArtifacts);
    [Fact] void should_restore_the_original_warning_when_strict_is_disabled() => Assert.Contains(_original.Degradations.Single().Diagnostic, _rebuilt.WithAcceptance(_rebuilt.Degradations).Diagnostics);
    [Fact] void should_preserve_inventory() => Assert.Same(_original.Inventory, _rebuilt.Inventory);
    [Fact] void should_preserve_evidence() => Assert.True(_original.Degradations.SequenceEqual(_rebuilt.Degradations));
    [Fact] void should_preserve_artifact_hashes() => Assert.Equal(_original.Artifacts.Select(artifact => artifact.Sha256), _rebuilt.Artifacts.Select(artifact => artifact.Sha256));
    [Fact] void should_preserve_removal_hashes() => Assert.True(_original.RequiredRemovals.SequenceEqual(_rebuilt.RequiredRemovals));
    [Fact] void should_preserve_the_scope() => Assert.Equal(_original.Scope, _rebuilt.Scope);
    [Fact] void should_preserve_additional_scopes() => Assert.Equal(_original.AdditionalScopes, _rebuilt.AdditionalScopes);
    [Fact] void should_preserve_versions() => Assert.Equal(_original.InputVersions, _rebuilt.InputVersions);
    [Fact] void should_keep_added_warnings() => Assert.All(_warnings, warning => Assert.Contains(warning, _rebuilt.Diagnostics));
    [Fact] void should_match_an_equivalent_reconstructed_digest()
    {
        var original = _original.WithAcceptance(_original.Degradations);
        var expected = ArtifactRenderPlan.Create(_request, original.Artifacts, [.. original.Diagnostics, .. _warnings], original.ImplementationGaps, original.Refusals, original.RequiredRemovals)
            .WithInventory(original.Inventory!).WithAcceptance(original.Degradations, strict: true);
        Assert.Equal(expected.Digest, _rebuilt.Digest);
    }

    [Fact] void should_drop_removals_for_a_genuine_generation_error()
    {
        var error = new ArtifactRenderDiagnostic("STAGE-ESM-021", ArtifactRenderDiagnosticSeverity.Error, "Invalid typed context.", _request.Model.Application.Id);
        var rebuilt = CratisPlanDiagnosticReconstruction.Append(_request, _rebuilt, [error]);
        Assert.False(rebuilt.CanPublishArtifacts);
        Assert.Empty(rebuilt.RequiredRemovals);
        Assert.Contains(error, rebuilt.WithAcceptance(rebuilt.Degradations).Diagnostics);
    }

    [Fact] void should_restore_source_information_and_warnings_without_losing_acceptance()
    {
        var source = new ArtifactRenderDiagnostic("PLAY0270", ArtifactRenderDiagnosticSeverity.Information, "metadata.play(1,1): Model information.", default);
        var warning = source with { Severity = ArtifactRenderDiagnosticSeverity.Warning, Message = "metadata.play(2,1): Model warning." };
        var rebuilt = CratisPlanDiagnosticReconstruction.Append(_request, _original, [source, warning]);
        Assert.True(rebuilt.Strict);
        Assert.True(rebuilt.CanPublishArtifacts);
        Assert.Contains(source, rebuilt.Diagnostics);
        Assert.Contains(warning, rebuilt.Diagnostics);
        Assert.True(_original.Degradations.SequenceEqual(rebuilt.Degradations));
        Assert.True(_original.RequiredRemovals.SequenceEqual(rebuilt.RequiredRemovals));
    }
}
#endif
