// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Only a zero literal operand of a required whole or decimal total renders (nothing is emitted for it); Chronicle's Add and
/// Subtract builders take an event property, so every other constant, and a total that may be absent, are refused.
/// </summary>
public class when_planning_literal_running_total_operands
{
    const string Template = """
        concept Money : Decimal
        module Buying
          feature Demand
            slice StateChange RecordDemand
              command RecordDemand
                sku String identifier
                quantity Int
                produces DemandRecorded
                  for sku
                  sku = sku
                  quantity = quantity
              event DemandRecorded
                sku String
                quantity Int
            slice StateView OpenDemand
              readmodel DemandRow
                sku String key
                demanded Int
                delivered DELIVEREDTYPE
              query AllOpenDemand => observable DemandRow[]
              projection DemandRowProjection => DemandRow
                from DemandRecorded key sku
                  add demanded by quantity
                  OPERATION
        """;

    public static TheoryData<string, string, string, bool> Cases => new()
    {
        { "zero add on a required whole number", "add delivered by 0", "Int", true },
        { "zero subtract on a required whole number", "subtract delivered by 0", "Int", true },
        { "zero add on a required decimal", "add delivered by 0", "Decimal", true },
        { "zero add on a required concept (a reference type, null until set)", "add delivered by 0", "Money", false },
        { "constant add", "add delivered by 5", "Int", false },
        { "zero add on a total that may be absent", "add delivered by 0", "Int optional", false },
    };

    const string OptionalTotals = """
        module Warehouse
          feature Overview
            slice StateChange Stock
              command ReceiveStock
                sku String identifier
                quantity Int
                produces StockReceived
                  for sku
                  sku = sku
                  quantity = quantity
              event StockReceived
                sku String
                quantity Int
              command ResetShelf
                sku String identifier
                produces ShelfReset
                  for sku
              event ShelfReset
            slice StateView Figures
              readmodel StockFigures
                summaryId String key
                unitsOnHand Int optional
              query CurrentStockFigures => observable StockFigures[]
              projection StockFiguresProjection => StockFigures
                no automap
                from StockReceived key "global"
                  summaryId = "global"
                  add unitsOnHand by quantity
                from ShelfReset key "global"
                  summaryId = "global"
                  clear unitsOnHand
        """;

    [Fact]
    async Task should_refuse_a_total_that_may_be_absent_when_it_is_also_cleared()
    {
        // Chronicle's Add throws on a property it has set to null (clear), where the reference starts again from 0.
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(OptionalTotals));

        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017" && diagnostic.Message.Contains("cannot also be cleared", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task should_admit_only_the_zero_start_of_a_required_total(string name, string operation, string type, bool admitted)
    {
        var source = Template.Replace("OPERATION", operation, StringComparison.Ordinal).Replace("DELIVEREDTYPE", type, StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        var diagnostics = string.Join(" | ", plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}"));

        if (admitted)
        {
            Assert.True(plan.Success, $"{name}: {diagnostics}");
            var projection = plan.Artifacts.Select(artifact => System.Text.Encoding.UTF8.GetString([.. artifact.Bytes])).Single(text => text.Contains("Add(model => model.Demanded)", StringComparison.Ordinal));
            Assert.DoesNotContain("model.Delivered", projection, StringComparison.Ordinal);
        }
        else
        {
            given.rendering_refusals.Rejected(plan, "STAGE-ESM-017");
        }
    }
}
