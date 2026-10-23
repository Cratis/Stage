// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections;

public class with_a_recursive_composite_holding_a_decimal : given.a_composite_projection
{
    Task Because() => Run(addressType: "type Address\n  street String\n  city String\n  lat Decimal\n  next Address optional", address: "{\"street\":\"Main 1\",\"city\":\"Oslo\",\"lat\":1.5,\"next\":{\"street\":\"Side 2\",\"city\":\"Bergen\",\"lat\":2.5}}", decimalPath: true);

    [Fact] void should_be_unsupported() => AssertOutcome(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_say_the_value_cannot_round_trip_exactly() => _result.Unsupported!.Details.ShouldContain("cannot round-trip exactly");
}
