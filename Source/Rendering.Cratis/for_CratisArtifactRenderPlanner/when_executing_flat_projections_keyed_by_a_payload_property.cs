// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The Acme warehouse history: rows keyed by a movement id carried in the payload of events that are sourced by the SKU, one row per
/// movement, from two events, with a constant for the direction. Screenplay keeps these as flat projections; they render through the
/// scope that says the same.
/// </summary>
public class when_executing_flat_projections_keyed_by_a_payload_property : a_generated_application
{
    internal const string Source = """
        concept Sku : String
        concept MovementId : Uuid
        concept Quantity : Int
          validate
            > 0 message "At least one"
        module Stock
          feature History
            slice StateChange Moving
              command Increase
                sku Sku identifier
                movementId MovementId generated
                quantity Quantity
                produces StockIncreased
                  for sku
                  sku = sku
                  movementId = movementId
                  quantity = quantity
              event StockIncreased
                sku Sku
                movementId MovementId
                quantity Quantity
              command Decrease
                sku Sku identifier
                movementId MovementId generated
                quantity Quantity
                produces StockDecreased
                  for sku
                  sku = sku
                  movementId = movementId
                  quantity = quantity
              event StockDecreased
                sku Sku
                movementId MovementId
                quantity Quantity
            slice StateView Adjustments
              readmodel AdjustmentRow
                id MovementId key
                sku Sku
                quantity Quantity
                increased Bool
              query AllAdjustments => observable AdjustmentRow[]
              projection AdjustmentRowProjection => AdjustmentRow
                no automap
                from StockIncreased key movementId
                  id = movementId
                  sku = sku
                  quantity = quantity
                  increased = true
                from StockDecreased key movementId
                  id = movementId
                  sku = sku
                  quantity = quantity
                  increased = false
              specification AnIncreaseIsListed
                given StockIncreased
                  for "ACM-1000"
                  sku = "ACM-1000"
                  movementId = "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
                  quantity = 5
                then readmodel AdjustmentRow
                  id = "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
                  sku = "ACM-1000"
                  quantity = 5
                  increased = true
              specification ADecreaseIsListed
                given StockDecreased
                  for "ACM-1000"
                  sku = "ACM-1000"
                  movementId = "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b02"
                  quantity = 2
                then readmodel AdjustmentRow
                  id = "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b02"
                  sku = "ACM-1000"
                  quantity = 2
                  increased = false
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_list_each_movement_under_its_own_id()
    {
        try
        {
            var build = await Run("flat-payload-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("flat-payload-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", results, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
