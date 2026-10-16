// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_protected_command_reads : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());

    void Because() => _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("ProtectedReads", "ProtectedReads"));

    [Fact] void should_report_no_diagnostics() => string.Join("\n", _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")).ShouldEqual(string.Empty);
    [Fact] void should_plan_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_refuse_the_decision_boundary() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-044");
    [Fact] void should_admit_the_version() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
    [Fact] void should_examine_read_commands() => ReadCommands().Count().ShouldEqual(3);
    [Fact] void should_render_the_use_slice() => Source("Use").ShouldNotBeEmpty();
    [Fact] void should_protect_every_command_that_reads() => Source("Use").Split("ReadProtectionAttribute(", StringSplitOptions.None).Length.ShouldEqual(4);
    [Fact] void should_emit_the_admission_specification() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("GeneratedCommands/when_admitting_protected_reads.cs", StringComparison.Ordinal));

    string Source(string slice) => string.Concat(_plan.Artifacts.Where(artifact => artifact.RelativePath.Contains($"/{slice}.cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString([.. artifact.Bytes])));

    IEnumerable<SemanticCommand> ReadCommands() => SemanticVersionFeatures.Slices(_model.Application).SelectMany(slice => slice.Commands).Where(command => !command.Reads.IsDefaultOrEmpty);
}
