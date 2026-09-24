// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_missing_event_source : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        var changed = _specification with { GivenEvents = [_specification.ThenEvents.Single() with { EventSource = null }] };
        var plan = With(changed);
        _report = await new SemanticSpecificationExecutor().Run(plan, new([changed.Id]), new());
    }

    [Fact] void should_block_identity_allocation() => _report.Results.Single().Unsupported!.Capability.ShouldEqual(StageExecutionCapability.IdentityAllocation);
    [Fact] void should_not_pass() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
}
