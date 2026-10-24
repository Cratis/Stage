// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections;

public class with_a_wrong_high_precision_decimal : given.a_composite_projection
{
    Task Because() => Run(lineType: "type Line\n  sku String\n  quantity Int\n  price Decimal", lines: "[{\"sku\":\"A-1\",\"quantity\":2,\"price\":12345678901234567.89}]", expectedLines: "[{\"sku\":\"A-1\",\"quantity\":2,\"price\":12345678901234567.88}]", exactGiven: "12345678901234567.89", exactExpected: "12345678901234567.88");

    [Fact] void should_fail_because_the_decimal_differs_in_the_last_digit() => AssertOutcome(SemanticSpecificationOutcome.Failed);
}
