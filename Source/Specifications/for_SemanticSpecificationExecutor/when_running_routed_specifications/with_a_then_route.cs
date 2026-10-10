// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_a_then_route : a_routed_plan
{
    async Task Because() => await Run(_plan);

    [Fact] void should_pass_like_the_reference() => AssertParity(true);
}
