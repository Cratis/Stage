// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Arc.Screenplay;
using Cratis.Screenplay.Conformance;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_legacy_graphs_rebind_as_v60 : Specification
{
    ExecutableSemanticModel _known = null!;
    ExecutableSemanticModel _legacy = null!;
    ExecutableSemanticModel _rebound = null!;
    RenderedFile[] _files = [];
    SemanticModelComparison _rebinding = null!;
    SemanticModelComparison _ownership = null!;
    SemanticModelComparison _changedOwnership = null!;
    SemanticModelComparison _meaningLoss = null!;

    void Because()
    {
        // Independently authored explicit ownership, and its historical owner-less contract; comparison must not rewrite either.
        _known = an_organization_graph_model.Request(graph_projection_sources.Split).Model;
        _legacy = ExecutableSemanticModel.Create(LanguageVersion.V38, SemanticVersion.V38, _known.Application with
        {
            GraphProjections = [.. _known.Application.GraphProjections.Select(projection => projection with { DeclaringSlice = null })]
        });
        _rebound = RecoverLegacy();
        _rebinding = round_trip_model_comparison.Compare(_legacy, _rebound);
        _ownership = round_trip_model_comparison.Compare(_legacy, _known);
        _changedOwnership = round_trip_model_comparison.Compare(_known, ChangedOwner());
        _meaningLoss = round_trip_model_comparison.Compare(comparison_profile_models.Legacy, _known);
    }

    ExecutableSemanticModel RecoverLegacy()
    {
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, _legacy.Application.Id);
        var plan = CratisRendering.Plan(_legacy, SemanticExecutionPlan.Compile(_legacy).Plan!, scope, new("Organization", "Organization"));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        _files = [.. plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs" && !artifact.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
        var compilation = RenderedOutput.CreateCompilation(_files);
        compilation = compilation.WithReferences(compilation.References.Select(reference => Path.GetFileName(reference.Display) == "Cratis.Chronicle.Core.dll"
            ? reference.WithAliases(["ChronicleKernel"])
            : reference));
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = new ScreenplayGenerator().Generate(compilation, new() { Domain = "Organization", Module = "Organization", AuthoringOnlyConstructs = false, SegmentsToSkip = 1 });
        Assert.True(generated.IsSuccess, string.Join(Environment.NewLine, generated.Diagnostics));
        var recovered = arc_round_trip.Recover(_legacy.Application.Name, generated.Source);
        Assert.Empty(recovered.Errors);
        return Assert.IsType<ExecutableSemanticModel>(recovered.Model);
    }

    ExecutableSemanticModel ChangedOwner()
    {
        var projection = _known.Application.GraphProjections[0];
        var other = GeneratedTypeNames.AllSlices(_known.Application).First(slice => slice.Slice.Id != projection.DeclaringSlice).Slice.Id;
        return ExecutableSemanticModel.Create(_known.LanguageVersion, _known.SemanticVersion, _known.Application with
        {
            GraphProjections = [projection with { DeclaringSlice = other }, .. _known.Application.GraphProjections.Skip(1)]
        });
    }

    [Fact] void should_emit_no_markers_for_owner_less_projections() => Assert.DoesNotContain(_files, file => file.Content.Contains("GraphProjectionDeclaration", StringComparison.Ordinal));
    [Fact] void should_rebind_legacy_source_as_v60() => _rebound.SemanticVersion.ShouldEqual(SemanticVersion.V60);
    [Fact] void should_validate_recovered_v60_at_its_native_pair() => round_trip_model_profile.ValidateNative(_rebound).LanguageVersion.ShouldEqual(LanguageVersion.V60);
    [Fact] void should_recover_declaring_ownership_from_the_fallback_slice() => _rebound.Application.GraphProjections.ShouldEachConformTo(projection => projection.DeclaringSlice is not null);
    [Fact] void should_report_rebinding_differences_instead_of_a_fixture_error() => _rebinding.IsEquivalent.ShouldBeFalse();
    [Fact] void should_report_the_recovered_version() => Assert.Contains(_rebinding.Differences, difference => difference.Path == "/semanticVersion" && difference.Kind == SemanticDifferenceKind.Changed);
    [Fact] void should_report_extra_ownership_without_fabricating_an_expectation() => Assert.Contains(_ownership.Differences, difference => difference.Path.EndsWith("/declaringSlice", StringComparison.Ordinal) && difference.Kind == SemanticDifferenceKind.Unexpected);
    [Fact] void should_keep_known_ownership_changes_visible() => Assert.Contains(_changedOwnership.Differences, difference => difference.Path.EndsWith("/declaringSlice", StringComparison.Ordinal) && difference.Kind == SemanticDifferenceKind.Changed);
    [Fact] void should_not_equate_a_changed_known_owner() => _changedOwnership.IsEquivalent.ShouldBeFalse();
    [Fact] void should_not_promote_the_legacy_expectation() => _legacy.SemanticVersion.ShouldEqual(SemanticVersion.V38);
    [Fact] void should_not_invent_legacy_ownership() => _legacy.Application.GraphProjections.ShouldEachConformTo(projection => projection.DeclaringSlice is null);
    [Fact] void should_preserve_meaning_gates_on_the_native_path() => Assert.Contains(_meaningLoss.Differences, difference => difference.Path.StartsWith("/version-meaning-unproven[V1 occurrence identity/context]", StringComparison.Ordinal));

    [Fact]
    void should_revalidate_native_pairs_even_when_their_versions_match()
    {
        var invalid = comparison_profile_models.Unchecked(
            _known,
            _known.LanguageVersion,
            _known.SemanticVersion,
            _known.Application with { Modules = [_known.Application.Modules[0], _known.Application.Modules[0]] });
        Assert.Throws<InvalidSemanticContract>(() => round_trip_model_comparison.Compare(_known, invalid));
    }
}
#endif
