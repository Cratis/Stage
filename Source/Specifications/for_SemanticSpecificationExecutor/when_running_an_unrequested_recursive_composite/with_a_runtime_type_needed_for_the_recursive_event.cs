// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Specifications.Comparison;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_unrequested_recursive_composite;

public class with_a_runtime_type_needed_for_the_recursive_event : given.a_recursive_event_in_an_unrequested_projection
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => new SemanticRuntimeTypes(_plan).For(_plan.Events.Single(@event => @event.Value.Name == "Linked").Key))!;

    [Fact] void should_refuse_by_name() => _error.ShouldBeOfExactType<SemanticProjectionLoweringRefused>();
    [Fact] void should_say_the_composite_is_recursive() => _error.Message.ShouldContain("recursive composite");
}
