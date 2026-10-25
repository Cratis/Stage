// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// ESM v30: one command appends to the purchase order's event source and to the SKU's, and the specification asserts each event on
/// its own source; the projection that follows the SKU's stream renders beside it.
/// </summary>
public class when_executing_commands_for_several_event_sources : a_generated_application
{
    internal const string Source = """
        concept Sku : String
        concept PurchaseOrderId : Uuid
        module Buying
          feature Replenishment
            slice StateChange CreateOrder
              command CreateOrder
                purchaseOrderId PurchaseOrderId identifier
                sku Sku
                quantity Int
                produces PurchaseOrderCreated
                  for purchaseOrderId
                  purchaseOrderId = purchaseOrderId
                  quantity = quantity
                produces SkuReplenishmentStarted
                  for sku
                  purchaseOrderId = purchaseOrderId
              event PurchaseOrderCreated
                purchaseOrderId PurchaseOrderId
                quantity Int
              event SkuReplenishmentStarted
                purchaseOrderId PurchaseOrderId
              specification AnOrderIsCreatedAndTheSkuRemembersIt
                when CreateOrder
                  purchaseOrderId = "7b2f4a3e-0c0d-4c6a-9a41-3b6a1a0e4001"
                  sku = "ACM-1000"
                  quantity = 5
                then PurchaseOrderCreated
                  for "7b2f4a3e-0c0d-4c6a-9a41-3b6a1a0e4001"
                  purchaseOrderId = "7b2f4a3e-0c0d-4c6a-9a41-3b6a1a0e4001"
                  quantity = 5
                then SkuReplenishmentStarted
                  for "ACM-1000"
                  purchaseOrderId = "7b2f4a3e-0c0d-4c6a-9a41-3b6a1a0e4001"
            slice StateView ReplenishmentStatus
              readmodel ReplenishmentRow
                sku Sku key
                purchaseOrderId PurchaseOrderId
              query AllReplenishments => observable ReplenishmentRow[]
              projection ReplenishmentRowProjection => ReplenishmentRow
                no automap
                from SkuReplenishmentStarted
                  sku = $eventSourceId
                  purchaseOrderId = purchaseOrderId
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_append_each_event_to_its_own_event_source()
    {
        try
        {
            var specification = string.Concat(GeneratedFiles("*.cs").Select(ReadGeneratedFile));
            Assert.Contains("should_have_appended_purchase_order_created_at_position_1_from_source", specification, StringComparison.Ordinal);
            Assert.Contains("should_have_appended_sku_replenishment_started_at_position_2_from_source", specification, StringComparison.Ordinal);
            var build = await Run("several-sources-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("several-sources-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", results, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
