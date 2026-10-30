// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_an_event_source_keyed_supplier_fixture : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare("""
        concept OfferId : Uuid
        module Buying
          feature Suppliers
            slice Automation Offering
              event SupplierRegistered
                name String
              command RegisterSupplier
                supplierId String identifier
                name String
                produces SupplierRegistered
                  for supplierId
                  name = name
              readmodel SupplierRow
                supplierId String
                name String
              query SupplierById => SupplierRow optional
                by supplierId String
              projection SupplierRowProjection => SupplierRow
                from SupplierRegistered
                  supplierId = $eventSourceId
                  name = name
              command OfferSupplierProduct
                offerId OfferId generated identifier
                supplierId String
                produces SupplierProductOffered
                  for offerId
                produces SupplierOfferAdded
                  for supplierId
              event SupplierProductOffered
              event SupplierOfferAdded
              specification OfferingAProduct
                given SupplierRegistered
                  for "supplier"
                  name = "Nordic Ceramics"
                given generated OfferSupplierProduct.offerId = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                when OfferSupplierProduct
                  supplierId = "supplier"
                then SupplierProductOffered
                  for "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                then SupplierOfferAdded
                  for "supplier"
        """);
    void Because() => Validate();
    [Fact] void should_name_the_unpreserved_projection_fixture() => _diagnostics.Single().Message.ShouldContain("Given event 'SupplierRegistered' feeds projection 'SupplierRowProjection'");
    [Fact] void should_describe_the_required_key_shape() => _diagnostics.Single().Message.ShouldContain("payload property equal to its explicit 'for' source");
    [Fact] void should_locate_the_refusal_on_the_specification() => _diagnostics.Single().Artifact.ShouldEqual(_specification.Id);
}
