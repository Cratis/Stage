// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_unrouted : a_routed_plan
{
    bool _unroutedPassed;

    async Task Because()
    {
        var expected = _specification.ThenEvents[0] with { Route = null, Unrouted = true };
        var specification = _specification with { ThenEvents = [expected] };
        await Run(With(specification, _command with { Route = null }));
        AssertParity(true);
        _unroutedPassed = _reference.Passed;
        await Run(With(specification));
    }

    [Fact] void should_pass_for_an_unrouted_command() => _unroutedPassed.ShouldBeTrue();
    [Fact] void should_fail_for_a_routed_command_like_the_reference() => AssertParity(false);
}
