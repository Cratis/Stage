// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// A projection keyed by an event property still reads the event's source identity: the receipt is per purchase order and names the SKU
/// whose stream the goods arrived on. Chronicle converts the identity text into the SKU concept, which is sound because the only command
/// that produces the event addresses it by a SKU.
/// </summary>
public class when_executing_event_source_in_payload_keyed_projections : a_generated_application
{
    internal const string Source = """
        concept Sku : String
          validate
            min 3 message "A SKU has at least three characters"
        concept PurchaseOrderId : Uuid
        module Stock
          feature Receiving
            slice StateChange ReceiveStock
              command ReceiveStock
                sku Sku identifier
                purchaseOrderId PurchaseOrderId
                quantity Int
                produces StockReceived
                  for sku
                  sku = sku
                  purchaseOrderId = purchaseOrderId
                  quantity = quantity
              event StockReceived
                sku Sku
                purchaseOrderId PurchaseOrderId
                quantity Int
            slice StateView ReceivingLog
              readmodel Receipt
                purchaseOrderId PurchaseOrderId key
                sku Sku
                received Int
              query AllReceipts => observable Receipt[]
              projection ReceiptProjection => Receipt
                no automap
                from StockReceived key purchaseOrderId
                  purchaseOrderId = purchaseOrderId
                  sku = $eventSourceId
                  received = quantity
              specification AReceiptAppearsOnTheLog
                given StockReceived
                  for "ACM-1000"
                  sku = "ACM-1000"
                  purchaseOrderId = "7b2f4a3e-0c0d-4c6a-9a41-3b6a1a0e4001"
                  quantity = 12
                then readmodel Receipt
                  purchaseOrderId = "7b2f4a3e-0c0d-4c6a-9a41-3b6a1a0e4001"
                  sku = "ACM-1000"
                  received = 12
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_name_the_source_of_the_event_in_the_projected_row()
    {
        try
        {
            var build = await Run("source-in-key-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("source-in-key-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", results, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
