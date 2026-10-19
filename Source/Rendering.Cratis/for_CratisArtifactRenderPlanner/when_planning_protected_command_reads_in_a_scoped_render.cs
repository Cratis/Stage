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
public class when_planning_protected_command_reads_in_a_scoped_render : Specification
{
    static readonly ExecutableSemanticModel _model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());

    public static TheoryData<int> Slices => [.. Enumerable.Range(0, SemanticVersionFeatures.Slices(_model.Application).Count())];

    [Fact] void should_examine_a_slice_without_commands() => SemanticVersionFeatures.Slices(_model.Application).ShouldContain(slice => slice.Commands.IsDefaultOrEmpty);

    [Theory]
    [MemberData(nameof(Slices))]
    void should_plan_without_refusing_the_decision_boundary(int index)
    {
        var slice = SemanticVersionFeatures.Slices(_model.Application).ElementAt(index).Id;
        var plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(ArtifactRenderScopeKind.Slice, slice), new("ProtectedReads", "ProtectedReads"));
        plan.Diagnostics.ShouldNotContain(diagnostic => string.Equals(diagnostic.Code, "STAGE-ESM-044", StringComparison.Ordinal) || string.Equals(diagnostic.Code, "STAGE-ESM-016", StringComparison.Ordinal));
        plan.Success.ShouldBeTrue();
        plan.Artifacts.ShouldNotBeEmpty();
    }
}
