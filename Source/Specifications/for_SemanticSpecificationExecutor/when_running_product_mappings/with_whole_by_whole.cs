// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_product_mappings;

public class with_whole_by_whole : given.a_product_run
{
    Task Because() => Run("Int", "Int", "6", "7", "42");

    [Fact] void should_match_the_reference() => AssertParity(true);
}
