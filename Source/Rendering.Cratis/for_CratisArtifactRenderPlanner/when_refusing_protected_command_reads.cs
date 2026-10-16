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

public class when_refusing_protected_command_reads : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());

    void Because() => _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("ProtectedReads", "ProtectedReads"));

    [Fact] void should_refuse_the_application_boundary() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-044" && diagnostic.Artifact == _model.Application.Id);
    [Fact] void should_examine_read_commands() => ReadCommands().ShouldNotBeEmpty();
    [Fact] void should_refuse_each_read_command() => ReadCommands().ShouldEachConformTo(command => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-044" && diagnostic.Artifact == command.Id));
    [Fact] void should_admit_the_version() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();

    IEnumerable<SemanticCommand> ReadCommands() => SemanticVersionFeatures.Slices(_model.Application).SelectMany(slice => slice.Commands).Where(command => !command.Reads.IsDefaultOrEmpty);
}
