// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeds;

public class when_seeding_dates_and_times : Specification
{
    const string Model = """
        module Warehouse
          feature Stock
            slice StateChange ReceiveStock
              command ReceiveStock
                sku String identifier
                quantity Int
                receivedAt DateTime
                receivedOn Date
                unitCost Decimal
                produces StockReceived
                  for sku
                  quantity = quantity
                  receivedAt = receivedAt
                  receivedOn = receivedOn
                  unitCost = unitCost
              event StockReceived
                quantity Int
                receivedAt DateTime
                receivedOn Date
                unitCost Decimal
            slice StateView Stock
              readmodel StockLevel
                stockId String
                quantity Int
              projection StockLevels => StockLevel
                from StockReceived key $eventSourceId
                  stockId = $eventSourceId
                  quantity = quantity
              query All => StockLevel[]
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticSeedPlan _seeds = null!;

    void Because()
    {
        var plan = _plan = compiled_plan.From(Model);
        var result = new ScreenplayCompiler().Compile($"""
            {Model}
            seed
              for "ACM-1000"
                StockReceived
                  quantity   = 140
                  receivedAt = "2026-10-10T08:00:00Z"
                  receivedOn = "2026-10-10"
                  unitCost   = 12.5
            """);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        _seeds = SemanticSeeds.Admit(SemanticSeeds.Create(result.Value!.Seeds, plan), plan);
    }

    [Fact] void should_not_report_anything() => _seeds.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_date_and_time_in_the_form_of_the_event_log() => SemanticSeeds.Stored(_plan, _seeds.Groups.Single().Facts, 0, DateTimeOffset.UtcNow).Single().Content.ShouldContain("2026-10-10T08:00:00.0000000Z");
    [Fact] void should_seed_the_event() => _seeds.Groups.Single().Facts.Length.ShouldEqual(1);
}
