// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_local_publication : Specification
{
    [Fact]
    public async Task should_append_the_outbound_publication_locally()
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan())).Plan!;
        var log = new a_semantic_fact_log();
        using var runtime = log.Runtime(plan, SemanticWorld.Empty);
        var arguments = new Dictionary<string, JsonElement>
        {
            ["orderId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["carrier"] = JsonSerializer.SerializeToElement("DHL")
        };
        var result = await runtime.Execute(plan.Commands.Values.Single(), arguments, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "subject", "name", "user"), false);
        var accepted = Assert.IsType<SemanticAccepted>(result);
        log.Writes.ShouldEqual(1);
        log.Facts.Count.ShouldEqual(2);
        accepted.World.Facts.Length.ShouldEqual(log.Facts.Count);
        var published = log.Facts.Single(fact => plan.Events[fact.EventContract].Visibility == SemanticEventVisibility.Public);
        published.Destination.ShouldEqual(accepted.Facts[0].Destination);
        published.Values.Single(value => plan.Events[published.EventContract].Properties.Single(property => property.Id == value.TargetProperty).Name == "carrier").Value.ShouldEqual(SemanticValue.Text("DHL"));
    }

    [Fact]
    public async Task should_refuse_a_restart_from_a_history_with_published_events()
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan())).Plan!;
        var log = new a_semantic_fact_log();
        using var runtime = log.Runtime(plan, SemanticWorld.Empty);
        var arguments = new Dictionary<string, JsonElement>
        {
            ["orderId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["carrier"] = JsonSerializer.SerializeToElement("DHL")
        };
        await runtime.Execute(plan.Commands.Values.Single(), arguments, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "subject", "name", "user"), false);
        var stored = SemanticSeeds.Stored(plan, log.Facts.Where(fact => plan.Events[fact.EventContract].Visibility == SemanticEventVisibility.Public), 0, DateTimeOffset.UtcNow).ToArray();
        foreach (var @event in stored)
        {
            @event.Context.CausedBy = new() { Subject = "subject", Name = "name", UserName = "user" };
        }

        var error = Catch.Exception(() => SemanticWorldRebuilder.Create(plan, stored, (ulong)stored.Length - 1));
        error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
        error.Message.ShouldContain("local publication");
    }
}
