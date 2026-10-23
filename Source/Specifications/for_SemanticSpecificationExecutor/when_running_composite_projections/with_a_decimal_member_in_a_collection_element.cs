// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections;

public class with_a_decimal_member_in_a_collection_element : given.a_composite_projection
{
    Task Because() => Run(lineType: "type Line\n  sku String\n  quantity Int\n  price Decimal", lines: "[{\"sku\":\"A-1\",\"quantity\":2,\"price\":12345678901234567.89}]", expectedLines: "[{\"sku\":\"A-1\",\"quantity\":2,\"price\":12345678901234567.89}]");

    [Fact] void should_pass_with_the_exact_decimal() => AssertOutcome(SemanticSpecificationOutcome.Passed);
}
