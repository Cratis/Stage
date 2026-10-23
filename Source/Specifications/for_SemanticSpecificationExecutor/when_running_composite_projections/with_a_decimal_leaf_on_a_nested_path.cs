// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections;

public class with_a_decimal_leaf_on_a_nested_path : given.a_composite_projection
{
    Task Because() => Run(addressType: "type Address\n  street String\n  city String\n  lat Decimal", address: "{\"street\":\"Main 1\",\"city\":\"Oslo\",\"lat\":12345678901234567.89}", decimalPath: true, expectedLat: "12345678901234567.89");

    [Fact] void should_pass_with_the_exact_decimal() => AssertOutcome(SemanticSpecificationOutcome.Passed);
}
