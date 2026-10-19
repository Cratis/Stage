// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_pinned_action_values;

public class with_a_command_specification_pinning_its_generated_id : given.a_specification_in_a_slice
{
    internal const string Source = """
        concept OfferId : Uuid
          generate uuid
        module Work
          feature Selling
            slice StateChange Offering
              command Offer
                offerId OfferId generated identifier
                sku String
                produces Offered
                  for offerId
                  offerId = offerId
                  sku = sku
              event Offered
                offerId OfferId
                sku String
              specification OfferingAProduct
                given generated Offer.offerId = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                when Offer
                  sku = "mug"
                then Offered
                  for "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                  offerId = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                  sku = "mug"
        """;

    void Establish() => Prepare(Source);
    void Because()
    {
        Validate();
        Render();
    }

    [Fact] void should_render_the_specification() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_pin_the_generated_value() => _source.ShouldContain("_scenario.PinGeneratedValue(\"offerId\"");
}
