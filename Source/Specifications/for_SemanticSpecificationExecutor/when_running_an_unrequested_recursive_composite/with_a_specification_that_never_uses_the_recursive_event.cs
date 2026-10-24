// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_unrequested_recursive_composite;

public class with_a_specification_that_never_uses_the_recursive_event : given.a_recursive_event_in_an_unrequested_projection
{
    async Task Because() => _result = Assert.Single((await new SemanticSpecificationExecutor().Run(_plan, new([]), new())).Results);

    [Fact] void should_not_be_refused() => Assert.True(_result.Outcome != SemanticSpecificationOutcome.Unsupported, $"{_result.Outcome}: {_result.Unsupported?.Details}");
    [Fact] void should_pass() => Assert.True(_result.Outcome == SemanticSpecificationOutcome.Passed, $"{_result.Outcome}: {_result.Unsupported?.Details} {string.Join(';', _result.Failures)}");
}
