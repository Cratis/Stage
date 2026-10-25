// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections;

public class with_a_whole_number_beyond_two_to_the_53 : given.a_composite_projection
{
    Task Because() => Run(lines: "[{\"sku\":\"A-1\",\"quantity\":9007199254740993}]", expectedLines: "[{\"sku\":\"A-1\",\"quantity\":9007199254740993}]");

    [Fact] void should_pass_with_the_exact_whole_number() => AssertOutcome(SemanticSpecificationOutcome.Passed);
}
