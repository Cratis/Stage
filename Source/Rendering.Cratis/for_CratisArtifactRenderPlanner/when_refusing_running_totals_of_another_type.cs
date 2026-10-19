// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Chronicle reads the added event value from a direct member access and never runs the lambda, so an event property that needs a
/// double conversion (a concept into an optional number) or a conversion that does not exist (a concept into another concept) cannot render.
/// </summary>
public class when_refusing_running_totals_of_another_type
{
    const string Template = """
        concept Quantity : Int
          validate
            > 0 message "At least one"
        concept StockQuantity : Int
          validate
            >= 0 message "Not negative"
        module Stock
          feature Levels
            slice StateChange Receive
              command ReceiveStock
                sku String identifier
                quantity QUANTITY
                produces StockReceived
                  for sku
                  quantity = quantity
              event StockReceived
                quantity QUANTITY
            slice StateView Levels
              readmodel StockLevel
                sku String key
                onHand TOTAL
              query AllLevels => observable StockLevel[]
              projection StockLevelProjection => StockLevel
                no automap
                from StockReceived
                  add onHand by quantity
        """;

    [Theory]
    [InlineData("Quantity", "Quantity", true)]
    [InlineData("Quantity", "Quantity optional", true)]
    [InlineData("Int", "Int", true)]
    [InlineData("Int", "Int optional", true)]
    [InlineData("Quantity", "StockQuantity", false)]
    [InlineData("Quantity", "Int", true)]
    [InlineData("Quantity", "Int optional", false)]
    [InlineData("Int", "StockQuantity", true)]
    [InlineData("Quantity", "StockQuantity optional", false)]
    public async Task should_admit_only_the_type_the_total_holds(string quantity, string total, bool admitted)
    {
        var source = Template.Replace("QUANTITY", quantity, StringComparison.Ordinal).Replace("TOTAL", total, StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        var diagnostics = string.Join(" | ", plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}"));

        if (admitted)
        {
            Assert.True(plan.Success, diagnostics);
        }
        else
        {
            Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017" && diagnostic.Message.Contains("another type", StringComparison.Ordinal));
        }
    }
}
