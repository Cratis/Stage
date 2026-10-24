// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections;

public class with_the_largest_safe_whole_number : given.a_composite_projection
{
    Task Because() => Run(lines: "[{\"sku\":\"A-1\",\"quantity\":9007199254740991}]");

    [Fact] void should_be_passed() => AssertOutcome(SemanticSpecificationOutcome.Passed);
}
