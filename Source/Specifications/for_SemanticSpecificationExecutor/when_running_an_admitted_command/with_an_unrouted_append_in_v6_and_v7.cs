// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_an_unrouted_append_in_v6_and_v7 : an_append_comparison
{
    SemanticSpecificationOutcome _v6;
    bool _v6Reference;

    async Task Because()
    {
        await RunAppend(SemanticVersion.V6);
        _v6 = _result.Outcome;
        _v6Reference = _reference.Passed;
        await RunAppend(SemanticVersion.V7);
    }

    [Fact] void should_exclude_the_action_from_then_events_in_v6() => _v6.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_match_the_v6_reference_failure() => _v6Reference.ShouldBeFalse();
    [Fact] void should_exclude_the_action_from_then_events_in_v7_like_the_reference() => AssertParity(false);
}
