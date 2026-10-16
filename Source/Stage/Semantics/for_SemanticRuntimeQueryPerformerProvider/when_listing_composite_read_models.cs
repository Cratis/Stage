// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider;

public class when_listing_composite_read_models : given.a_composite_key_runtime
{
    async Task Because() => await Perform($"All{ModelNaming.Pluralize("PaymentTotals")}", QueryArguments.Empty);

    [Fact] void should_offer_no_conventional_lookup_by_id() => _provider.Performers.ShouldNotContain(performer => performer.Name == "GetPaymentTotalsById");
    [Fact] void should_identify_each_instance_by_its_parts() => ((IEnumerable<object>)_result!).Cast<DynamicReadModel>().Select(model => model.Id)
        .ShouldContainOnly("""{"invoiceId":"INV-1","method":"card"}""", """{"invoiceId":"INV-1","method":"cash"}""");
}
