// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticInlineCode.when_rendering_verified_generation_body;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticInlineCode;

public class when_applying_acceptance_to_verified_generation_body : Specification
{
    const string Remedy = ImplementationLanguage.Remedy;
    SemanticInlineCodeResult _inline = null!;
    ArtifactRenderDiagnostic _warning = null!;
    ArtifactRenderDegradation _evidence = null!;
    ArtifactRenderPlan _original = null!;
    ArtifactRenderPlan _strict = null!;
    ArtifactRenderPlan _restored = null!;

    void Because()
    {
        var context = Context(" \n", language: "java");
        var concept = context.Application.Concepts.Single();
        _inline = SemanticInlineCodeRendering.Generation(concept, context);
        var output = SemanticCommonArtifactRenderer.Render(concept, context);
        _warning = Assert.Single(context.InlineDiagnostics);
        Assert.Equal(Assert.Single(_inline.Diagnostics).Code, _warning.Code);
        Assert.Equal(concept.Id, _warning.Artifact);
        _evidence = new(_warning, Remedy);
        var generated = ArtifactRenderPlan.Create(
            context.Request,
            [PlannedArtifact.CreateText(output.RelativePath, output.Content, output.Sources)],
            [_warning],
            []);
        var address = context.Request.ImplementationRequirements.Single().Owner;
        var inventory = ArtifactRenderInventory.Create([new(concept.Id, ArtifactRenderConstructKind.Concept, null, ArtifactRenderOutcome.Degraded)
        {
            SemanticLocator = address,
            Degradations = [new(_evidence, ArtifactRenderPlaceholderKind.ThrowingOperationBody, concept.Generation!.RequirementId!, null) { SemanticLocator = address }]
        }]);
        _original = generated.WithAcceptance([_evidence]).WithInventory(inventory);
        _strict = _original.WithAcceptance(_original.Degradations, strict: true);
        _restored = _strict.WithAcceptance(_strict.Degradations);
    }

    [Fact] void should_use_a_real_safe_inline_mismatch() => _inline.Refused.ShouldBeFalse();
    [Fact] void should_retain_the_underlying_inline_degradation() => _inline.Degraded.ShouldBeTrue();
    [Fact] void should_accept_the_default_plan() => _original.Success.ShouldBeTrue();
    [Fact] void should_publish_the_default_plan() => _original.CanPublishArtifacts.ShouldBeTrue();
    [Fact] void should_retain_the_original_warning() => Assert.Equal(_warning, Assert.Single(_original.Diagnostics));
    [Fact] void should_keep_warning_severity() => _warning.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_keep_the_warning_code() => _warning.Code.ShouldEqual("STAGE-DEGRADE-003");
    [Fact] void should_keep_the_warning_explanation() => Assert.Contains("language 'java'", _warning.Message, StringComparison.Ordinal);
    [Fact] void should_include_the_remedy_in_the_warning() => Assert.Contains(Remedy, _warning.Message, StringComparison.Ordinal);
    [Fact] void should_keep_the_original_evidence_and_remedy() => Assert.Equal(_evidence, Assert.Single(_original.Degradations));
    [Fact] void should_fail_strict_acceptance() => _strict.Success.ShouldBeFalse();
    [Fact] void should_still_publish_safe_artifacts_in_strict_mode() => _strict.CanPublishArtifacts.ShouldBeTrue();
    [Fact] void should_keep_the_degraded_construct() => Assert.Single(_strict.Inventory!.Constructs).Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_keep_original_warning_evidence_in_strict_mode() => Assert.Equal(_evidence, Assert.Single(_strict.Degradations));
    [Fact] void should_keep_the_inventory_instance() => Assert.Same(_original.Inventory, _strict.Inventory);
    [Fact] void should_keep_the_inventory_digest() => _strict.Inventory!.Digest.ShouldEqual(_original.Inventory!.Digest);
    [Fact] void should_keep_all_artifacts() => _strict.Artifacts.SequenceEqual(_original.Artifacts).ShouldBeTrue();
    [Fact] void should_keep_required_removals() => _strict.RequiredRemovals.SequenceEqual(_original.RequiredRemovals).ShouldBeTrue();
    [Fact] void should_keep_scope() => _strict.Scope.ShouldEqual(_original.Scope);
    [Fact] void should_keep_additional_scopes() => _strict.AdditionalScopes.SequenceEqual(_original.AdditionalScopes).ShouldBeTrue();
    [Fact] void should_restore_original_diagnostics() => _restored.Diagnostics.SequenceEqual(_original.Diagnostics).ShouldBeTrue();
    [Fact] void should_restore_original_evidence() => _restored.Degradations.SequenceEqual(_original.Degradations).ShouldBeTrue();
    [Fact] void should_restore_default_success() => _restored.Success.ShouldBeTrue();
    [Fact] void should_not_mutate_the_original_acceptance() => _original.Strict.ShouldBeFalse();
    [Fact] void should_not_mutate_the_strict_acceptance() => _strict.Strict.ShouldBeTrue();
    [Fact] void should_promote_only_the_strict_diagnostic_severity() => Assert.Equal(_warning with { Severity = ArtifactRenderDiagnosticSeverity.Error }, Assert.Single(_strict.Diagnostics));
    [Fact] void should_restore_the_original_inventory() => Assert.Same(_original.Inventory, _restored.Inventory);
    [Fact] void should_restore_default_acceptance() => _restored.Strict.ShouldBeFalse();

    [Fact]
    void should_retain_identical_nonempty_artifact_bytes_in_both_modes()
    {
        var artifact = Assert.Single(_original.Artifacts);
        Assert.NotEmpty(artifact.Bytes);
        Assert.Contains("throw new NotSupportedException", System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()));
        Assert.Single(_strict.Artifacts).Bytes.SequenceEqual(artifact.Bytes).ShouldBeTrue();
        Assert.Single(_restored.Artifacts).Bytes.SequenceEqual(artifact.Bytes).ShouldBeTrue();
        Assert.Single(_strict.Artifacts).Sha256.ShouldEqual(artifact.Sha256);
    }

    [Theory]
    [InlineData("return @\"unterminated", null)]
    [InlineData("return \"Generated\";", "different verified bytes")]
    void should_refuse_unsafe_bodies_instead_of_degrading(string body, string? hashSource)
    {
        var context = Context(body, hashSource);
        var concept = context.Application.Concepts.Single();
        var result = SemanticInlineCodeRendering.Generation(concept, context);
        result.Refused.ShouldBeTrue();
        result.Degraded.ShouldBeFalse();
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, diagnostic => diagnostic.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error));
        Assert.Throws<InvalidArtifactRenderContract>(() => SemanticCommonArtifactRenderer.Render(concept, context));
        Assert.Empty(context.InlineDiagnostics);
    }
}
