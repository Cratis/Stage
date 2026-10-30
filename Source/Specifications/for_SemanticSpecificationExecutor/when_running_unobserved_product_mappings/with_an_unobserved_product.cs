// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_unobserved_product_mappings;

public class with_an_unobserved_product : given.a_product_projection
{
    Task Because() => Run("""
        Named
                      for "one"
                      name = "n"
        """);

    [Fact] void should_be_passed() => AssertOutcome(SemanticSpecificationOutcome.Passed);
}
