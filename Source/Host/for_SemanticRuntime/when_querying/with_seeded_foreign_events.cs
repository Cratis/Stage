// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_querying;

public class with_seeded_foreign_events : Specification
{
    [Fact]
    public async Task should_query_seeded_foreign_facts_without_executing_the_capture()
    {
        const string Source = """
            module Shipping
              feature Tracking
                slice Translate Receive
                  direction inbound
                  event Arrived from "carrier"
                    id String
                    status String
                  event Recorded
                    id String
                  capture Feed
                    source events
                      from Arrived
                    key id
                    append Recorded
                      id = $.id
                  readmodel Shipment
                    shipmentId String
                    status String
                  projection Shipments => Shipment
                    from Arrived key id
                      shipmentId = id
                      status = status
                  query All => Shipment[]
            """;
        var plan = compiled_plan.From(Source);
        var syntax = new ScreenplayCompiler().Compile($"{Source}\nseed\n  for \"parcel\"\n    Arrived\n      id = \"parcel\"\n      status = \"sent\"\n").Value!;
        var seeds = SemanticSeeds.Admit(SemanticSeeds.Create(syntax.Seeds, plan), plan);
        seeds.Diagnostics.ShouldBeEmpty();
        var stored = SemanticSeeds.Stored(plan, seeds.Groups.Single().Facts, 0, DateTimeOffset.UtcNow).ToArray();
        var world = SemanticWorldRebuilder.Create(plan, stored, 0);
        var facts = world.Facts;
        var log = new a_semantic_fact_log();
        log.Facts.AddRange(facts);
        using var runtime = log.Runtime(plan, world);
        new SemanticRuntimeAdmission(plan).Blocking.ShouldBeEmpty();
        var result = Assert.IsType<SemanticAccepted>(await runtime.Query(plan.Queries.Values.Single(), SemanticValue.Null, new ClaimsPrincipal()));
        result.Queries.Single().Results.Length.ShouldEqual(1);
        result.World.Facts.Length.ShouldEqual(1);
        plan.Events[result.World.Facts.Single().EventContract].Name.ShouldEqual("Arrived");
        log.Writes.ShouldEqual(0);
    }
}
