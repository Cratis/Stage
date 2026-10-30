// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

/// <summary>Stage#891 S5: an admitted C# handler body runs once in the shared worker and returns its event payloads in order.</summary>
public class when_executing_command_handlers : Specification
{
    [Fact]
    async Task should_run_the_body_once_and_return_the_produced_event_payload()
    {
        using var fixture = new a_real_worker();
        var (loaded, entry) = Compile(fixture, handler_model.Body);
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        await using var worker = Create(fixture, loaded, entry);
        var result = await worker.HandleAsync(Request(loaded, entry, "ada"));
        Assert.True(result.Outcome == "produced", result.Diagnostic);
        var events = Assert.IsType<SemanticArrayValue>(SemanticValueWire.Read(result.Value!.Value));
        var payload = Assert.IsType<SemanticCompositeValue>(Assert.Single(events.Values));
        payload.Properties.Single().Value.ShouldEqual(SemanticValue.Text("ADA"));

        // A reduce request cannot invoke a handler unit.
        (await worker.ReduceAsync(Request(loaded, entry, "ada"))).Outcome.ShouldEqual("faulted");
    }

    [Fact]
    async Task should_refuse_a_request_without_exactly_one_command_member()
    {
        using var fixture = new a_real_worker();
        var (loaded, entry) = Compile(fixture, handler_model.Body);
        await using var worker = Create(fixture, loaded, entry);
        var request = Request(loaded, entry, "ada");
        var result = await worker.HandleAsync(request with { Members = request.Members.Add("Extra", request.Members["Command"]) });
        result.Outcome.ShouldNotEqual("produced");
    }

    internal static (LoadedSemanticModel Loaded, BodyUnitEntry Entry) Compile(a_real_worker fixture, string body, string? source = null)
    {
        var request = handler_model.Request(body, source);
        var loaded = new LoadedSemanticModel(request.Model, request.ExecutionPlan)
        {
            ImplementationRequirements = request.ImplementationRequirements,
            TypedContextDescriptors = request.TypedContextDescriptors,
            ImplementationContents = request.ImplementationContents
        };
        var entry = BodyUnitCompiler.Compile(loaded, fixture.CacheDirectory).Entries.Single();
        return (loaded, entry);
    }

    internal static BodyWorkerSupervisor Create(a_real_worker fixture, LoadedSemanticModel loaded, BodyUnitEntry entry) =>
        new(new(fixture.WorkerPath, fixture.CacheDirectory) { TimeoutMs = 10000, HeapLimitMb = 96 }, new BodyUnitManifest(loaded.Model.Revision.ToString(), [entry]));

    internal static BodyEvaluationRequest Request(LoadedSemanticModel loaded, BodyUnitEntry entry, string name)
    {
        var command = loaded.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Commands).Single(command => command.Handler is not null);
        var composite = SemanticValue.Composite([
            new(command.Properties.Single(property => property.Name == "orderId").Id, SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6")),
            new(command.Properties.Single(property => property.Name == "name").Id, SemanticValue.Text(name))]);
        return new(entry.RequirementId, new Dictionary<string, SemanticValue> { ["Command"] = composite }.ToImmutableDictionary(pair => pair.Key, pair => SemanticValueWire.Write(pair.Value), StringComparer.Ordinal));
    }
}
#endif
