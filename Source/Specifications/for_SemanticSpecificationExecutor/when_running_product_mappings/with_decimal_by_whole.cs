// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_product_mappings;

public class with_decimal_by_whole : given.a_product_run
{
    Task Because() => Run("Decimal", "Int", "3.32", "3", "9.96");

    [Fact] void should_match_the_reference() => AssertParity(true);
}
