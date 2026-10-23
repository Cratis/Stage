// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_product_mappings;

public class with_an_optional_factor : given.a_product_run
{
    Task Because() => Run("Decimal", "Int optional", "3.32", "3", "9.96");

    [Fact] void should_keep_the_product_refused_by_name() => AssertRefused("A projection adds or subtracts a product");
}
