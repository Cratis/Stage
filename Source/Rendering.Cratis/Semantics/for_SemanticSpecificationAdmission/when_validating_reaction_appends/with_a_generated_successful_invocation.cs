// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_generated_successful_invocation : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare("""
        concept PurchaseOrderId : Uuid
        module Buying
          feature Replenishment
            slice Automation AutoReplenish
              event StockBelowReorderPointAtPurchasing from "Warehouse"
                sku String
              capture ReorderFeed
                source events
                  from StockBelowReorderPointAtPurchasing
                key sku
                append ReplenishmentRequested
                  when added
                    sku = $.sku
              event ReplenishmentRequested
                sku String
              event PurchaseOrderCreated
              command CreateReplenishmentPurchaseOrder
                purchaseOrderId PurchaseOrderId generated identifier
                sku String
                produces PurchaseOrderCreated
                  for purchaseOrderId
              reaction Replenisher
                when ReplenishmentRequested
                  invokes CreateReplenishmentPurchaseOrder
                    sku = sku
                    on refused
                      acknowledge
              specification AutoReplenishDraftsAnOrderForThePreferredSupplier
                given generated CreateReplenishmentPurchaseOrder.purchaseOrderId = "c0ffee00-0000-4000-8000-000000000001"
                when append ReplenishmentRequested
                  for "ACM-1000"
                  sku = "ACM-1000"
                then PurchaseOrderCreated
        """);
    void Because() => Validate();
    [Fact] void should_locate_the_refusal_on_the_specification() => _diagnostics.Single().Artifact.ShouldEqual(_specification.Id);
    [Fact] void should_name_the_generated_invocation() => _diagnostics.Single().Message.ShouldContain("Invoked command 'CreateReplenishmentPurchaseOrder' generates property 'purchaseOrderId'");
    [Fact] void should_explain_the_supported_fixture_alternative() => _diagnostics.Single().Message.ShouldContain("given generated CreateReplenishmentPurchaseOrder.purchaseOrderId");
}
