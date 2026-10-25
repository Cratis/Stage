// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Running totals that may be absent (optional whole numbers) under a constant key: a never-touched total stays absent and the first
/// operation starts from 0, in Chronicle as in the reference.
/// </summary>
public class when_executing_optional_running_totals : a_generated_application
{
    internal const string Source = """
        concept Sku : String
        concept Quantity : Int
          validate
            > 0 message "At least one"
        concept StockQuantity : Int
          validate
            >= 0 message "Not negative"
        module Warehouse
          feature Overview
            slice StateChange Stock
              command RegisterSku
                sku Sku identifier
                produces SkuRegistered
                  for sku
                  sku = sku
              event SkuRegistered
                sku Sku
              command ReceiveStock
                sku Sku identifier
                quantity Quantity
                produces StockReceived
                  for sku
                  sku = sku
                  quantity = quantity
              event StockReceived
                sku Sku
                quantity Quantity
              command DispatchStock
                sku Sku identifier
                quantity Quantity
                produces StockDispatched
                  for sku
                  sku = sku
                  quantity = quantity
              event StockDispatched
                sku Sku
                quantity Quantity
            slice StateView Figures
              readmodel StockFigures
                summaryId String key
                skuCount Int optional
                unitsOnHand StockQuantity optional
                parcelsOpen Int optional
              query CurrentStockFigures => observable StockFigures[]
              projection StockFiguresProjection => StockFigures
                no automap
                from SkuRegistered key "global"
                  summaryId = "global"
                  increment skuCount
                from StockReceived key "global"
                  summaryId = "global"
                  add unitsOnHand by quantity
                from StockDispatched key "global"
                  summaryId = "global"
                  subtract unitsOnHand by quantity
                  decrement parcelsOpen
              specification TheFiguresFollowTheShelves
                given SkuRegistered
                  for "ACM-1000"
                  sku = "ACM-1000"
                given SkuRegistered
                  for "ACM-2000"
                  sku = "ACM-2000"
                given StockReceived
                  for "ACM-1000"
                  sku = "ACM-1000"
                  quantity = 40
                given StockDispatched
                  for "ACM-1000"
                  sku = "ACM-1000"
                  quantity = 5
                then readmodel StockFigures
                  summaryId = "global"
                  skuCount = 2
                  unitsOnHand = 35
                  parcelsOpen = -1
              specification AnUntouchedTotalStaysAbsent
                given SkuRegistered
                  for "ACM-3000"
                  sku = "ACM-3000"
                then readmodel StockFigures
                  summaryId = "global"
                  skuCount = 1
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_run_the_projected_specifications()
    {
        try
        {
            var build = await Run("optional-total-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("optional-total-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            results.ShouldHavePassedSpecifications();
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
