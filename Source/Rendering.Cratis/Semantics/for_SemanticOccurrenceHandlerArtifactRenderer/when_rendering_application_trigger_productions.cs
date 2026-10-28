// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceHandlerArtifactRenderer;

public class when_rendering_application_trigger_productions : given.an_application_trigger_reaction
{
    string _handler = string.Empty;
    IReadOnlyList<string> _errors = [];
    IReadOnlyList<string> _warnings = [];

    void Because()
    {
        Plan();
        _handler = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => Path.GetFileName(artifact.RelativePath) == "ProcessBatch.cs").Bytes.AsSpan());
        var files = Files(_plan);
        _errors = for_CratisRenderer.RenderedOutput.Errors(files);
        _warnings = for_CratisRenderer.RenderedOutput.Warnings(files);
    }

    [Fact] void should_complete_the_render_plan() => _plan.Success.ShouldBeTrue();
    [Fact] void should_implement_a_discovered_handler() => _handler.ShouldContain("IOccurrenceHandler<BatchRequested>");
    [Fact] void should_bind_the_recording_namespace() => _handler.ShouldContain("await client.GetEventStore(eventStore.Name, @namespace)");
    [Fact] void should_not_use_ambient_service_provider_state() => _handler.ShouldNotContain("IServiceProvider");
    [Fact] void should_enroll_one_atomic_batch() => _handler.Split("unitOfWork.AddEvents(EventSequenceId.Log,", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_use_short_type_names() => _handler.ShouldNotContain("global::");
    [Fact] void should_not_use_an_immediate_append() => _handler.ShouldNotContain("AppendMany");
    [Fact] void should_enroll_without_additional_concurrency_scopes() => _handler.ShouldContain(", []);");
    [Fact] void should_name_the_payload_mapping() => _handler.ShouldContain("new BatchStarted(occurrence.BatchId)");
    [Fact] void should_set_the_explicit_destination() => _handler.ShouldContain("new(occurrence.BatchId, new BatchStarted");
    [Fact] void should_not_use_an_ambient_event_source() => _handler.ShouldNotContain("context.EventSourceId");
    [Fact] void should_not_render_a_reactor() => _handler.ShouldNotContain("IReactor");

    [Fact]
    void should_use_a_stable_reaction_effect_key()
    {
        _handler.ShouldContain($"stage:reaction:{_reaction.Id}:productions");
        _handler.ShouldContain("GetEffectReceipt(effect)");
        _handler.ShouldContain("UseEffectReceipt(effect, null)");
        _handler.IndexOf("UseEffectReceipt", StringComparison.Ordinal).ShouldBeLessThan(_handler.IndexOf("AddEvents", StringComparison.Ordinal));
        _handler.IndexOf("AddEvents", StringComparison.Ordinal).ShouldBeLessThan(_handler.IndexOf("unitOfWork.Commit", StringComparison.Ordinal));
        _handler.ShouldNotContain("RecordEffectReceipt");
    }

    [Fact] void should_compile_against_the_pinned_client() => _errors.ShouldBeEmpty();
    [Fact] void should_compile_without_warnings() => _warnings.ShouldBeEmpty();

    internal static IReadOnlyList<RenderedFile> Files(ArtifactRenderPlan plan) =>
        [.. plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs").Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}
