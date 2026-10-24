// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections;

public class with_a_wrong_nested_scalar : given.a_composite_projection
{
    Task Because() => Run(expectedCity: "Bergen");
    [Fact] void should_be_failed() => AssertOutcome(SemanticSpecificationOutcome.Failed);
}
