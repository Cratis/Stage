// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_the_canonical_v4_corpus : a_register_project_render_request
{
    ExecutableSemanticModel _v4 = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _v4 = SemanticModelSerializer.Deserialize(RegisterProjectCorpus.V2.EsmBytes.AsSpan());

    void Because() => _plan = CratisRendering.Plan(
        _v4,
        SemanticExecutionPlan.Compile(_v4).Plan!,
        new(ArtifactRenderScopeKind.Application, _v4.Application.Id),
        _options);

    [Fact] void should_load_an_esm_v4_model() => _v4.SemanticVersion.ShouldEqual(SemanticVersion.V4);
    [Fact] void should_render_the_current_generation() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_emit_the_current_event_without_the_historical_identifier() => Assert.Contains("public record ProjectRegistered(global::Projects.Common.ProjectName Name);", EventSource());
    [Fact] void should_not_emit_historical_records() => _plan.Artifacts.Select(Text).Sum(text => text.Split("public record ProjectRegistered(", StringSplitOptions.None).Length - 1).ShouldEqual(1);
    [Fact] void should_preserve_the_carried_lineage() => _v4.Application.Modules.Single().Features.Single().Slices.Single(slice => !slice.Events.IsEmpty).Events.Single().PriorRevisions.Length.ShouldEqual(1);

    [Fact] void should_emit_the_modeled_generation() => Assert.Contains("[global::Cratis.Chronicle.Events.EventTypeAttribute(generation: 2)]", EventSource());
    [Fact] void should_compile_with_the_current_native_event_identity()
    {
        var files = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Text(artifact)));
        var assembly = RenderedOutput.Load(files);
        assembly.GetType("Projects.Projects.Registration.RegisterProject.ProjectRegistered", throwOnError: true)!.GetEventType()
            .ShouldEqual(new EventType("ProjectRegistered", 2));
    }

    string EventSource() => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Projects/Registration/RegisterProject/RegisterProject.cs"));
}
