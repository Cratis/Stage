// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The source identity of an event read by a payload-keyed projection becomes the target's type only when every command that produces the
/// event addresses it with a value of that type; a second producer that addresses it differently, or a target of another type, is refused.
/// </summary>
public class when_refusing_event_source_in_payload_keyed_projections
{
    const string Template = """
        concept Sku : String
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
              command ReceiveAgainstOrder
                purchaseOrderId PurchaseOrderId identifier
                sku Sku
                quantity Int
                produces StockReceived
                  for purchaseOrderId
                  sku = sku
                  purchaseOrderId = purchaseOrderId
                  quantity = quantity
            slice StateView ReceivingLog
              readmodel Receipt
                purchaseOrderId PurchaseOrderId key
                sku TARGETTYPE
                received Int
              query AllReceipts => observable Receipt[]
              projection ReceiptProjection => Receipt
                no automap
                from StockReceived key purchaseOrderId
                  purchaseOrderId = purchaseOrderId
                  sku = $eventSourceId
                  received = quantity
        """;

    [Theory]
    [InlineData("a second producer addresses the event by another type", "Sku", true)]
    [InlineData("a target of another type than the producers address", "PurchaseOrderId", false)]
    public async Task should_refuse_the_identity_that_is_not_of_the_target_type(string name, string targetType, bool secondProducer)
    {
        var source = Template.Replace("TARGETTYPE", targetType, StringComparison.Ordinal);
        if (!secondProducer)
        {
            source = source.Replace(
                "              command ReceiveAgainstOrder\n                purchaseOrderId PurchaseOrderId identifier\n                sku Sku\n                quantity Int\n                produces StockReceived\n                  for purchaseOrderId\n                  sku = sku\n                  purchaseOrderId = purchaseOrderId\n                  quantity = quantity\n",
                string.Empty,
                StringComparison.Ordinal);
        }

        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));

        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.False(plan.Success, name);
    }

    const string WithReaction = """
        concept Sku : String
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
              command Announce
                id PurchaseOrderId identifier
                sku Sku
                produces Announced
                  for id
                  id = id
                  sku = sku
              event Announced
                id PurchaseOrderId
                sku Sku
            slice Automation Replay
              reaction ReplayTheReceipt
                when Announced
                  produces StockReceived
                    for DESTINATION
                    sku = sku
                    purchaseOrderId = id
                    quantity = 1
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
        """;

    [Fact]
    public async Task should_not_admit_the_identity_when_a_reaction_may_produce_the_event_with_another_destination_type()
    {
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(WithReaction.Replace("DESTINATION", "id", StringComparison.Ordinal)));

        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    }

    [Fact]
    public async Task should_admit_the_identity_when_the_reaction_addresses_the_event_by_the_target_type()
    {
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(WithReaction.Replace("DESTINATION", "sku", StringComparison.Ordinal)));

        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017");
    }
}
