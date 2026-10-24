// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_unobserved_product_mappings;

public class with_an_unsupported_observed_product : given.a_product_projection
{
    Task Because() => Run("""
        Added
                      for "one"
                      unitPrice = 2
                      quantity = 3
        """,
        factorSuffix: " optional");

    [Fact] void should_refuse_the_product() => AssertOutcome(SemanticSpecificationOutcome.Unsupported);
}
