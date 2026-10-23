// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_exact_structured_reads;

public class with_only_a_nested_whole_number_leaf_beyond_two_to_the_53 : given.an_isolated_structured_read
{
    Task Because() => Run("Int", "9007199254740993", false);

    [Fact] void should_report_the_outcome() => AssertOutcome(SemanticSpecificationOutcome.Passed);
}
