// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_unobserved_product_mappings;

public class with_an_unsupported_unobserved_product : given.a_product_projection
{
    Task Because() => Run("""
        Named
                      for "one"
                      name = "n"
        """,
        factorSuffix: " optional");

    [Fact] void should_pass_because_the_product_event_never_occurs() => AssertOutcome(SemanticSpecificationOutcome.Passed);
}
