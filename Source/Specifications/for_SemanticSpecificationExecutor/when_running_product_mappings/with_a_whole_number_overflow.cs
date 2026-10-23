// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_product_mappings;

public class with_a_whole_number_overflow : given.a_product_run
{
    Task Because() => Run("Int", "Int", "4611686018427387904", "4", "1");

    [Fact] void should_refuse_by_name() => AssertRefused("A product of two event values overflows its number type.");
}
