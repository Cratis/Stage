// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// A slice-scoped render of a protected-reads model, including a slice that declares no command of its own.
/// </summary>
/// <remarks>
/// The protected read mode belongs to the application, so a scoped render must refuse it even when the selected
/// slice only invokes a protected command elsewhere; otherwise the invoked command would run without its reads.
/// </remarks>
public class when_refusing_protected_command_reads_in_a_scoped_render : Specification
{
    static readonly ExecutableSemanticModel _model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());

    public static TheoryData<int> Slices => [.. Enumerable.Range(0, SemanticVersionFeatures.Slices(_model.Application).Count())];

    [Fact] void should_examine_a_slice_without_commands() => SemanticVersionFeatures.Slices(_model.Application).ShouldContain(slice => slice.Commands.IsDefaultOrEmpty);

    [Theory]
    [MemberData(nameof(Slices))]
    void should_refuse_the_application_boundary_without_emitting_artifacts(int index)
    {
        var slice = SemanticVersionFeatures.Slices(_model.Application).ElementAt(index).Id;
        var plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(ArtifactRenderScopeKind.Slice, slice), new("ProtectedReads", "ProtectedReads"));
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-044" && diagnostic.Artifact == _model.Application.Id);
        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
        plan.Artifacts.ShouldBeEmpty();
    }
}
