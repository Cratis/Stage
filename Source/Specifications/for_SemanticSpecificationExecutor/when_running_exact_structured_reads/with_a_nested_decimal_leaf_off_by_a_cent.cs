// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_exact_structured_reads;

public class with_a_nested_decimal_leaf_off_by_a_cent : given.an_isolated_structured_read
{
    Task Because() => Run("Decimal", "12345678901234567.89", false, "12345678901234567.88");

    [Fact] void should_report_the_outcome() => AssertOutcome(SemanticSpecificationOutcome.Failed);
}
