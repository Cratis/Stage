// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_rejecting_a_from_block;

public class with_several_failing_blocks : given.a_scoped_from_block
{
    void Establish()
    {
        _firstFrom = _firstFrom with
        {
            Mappings =
            [
                _mapping with { Source = SemanticProjectionValue.EventContext("first") },
                _mapping with { Target = [_missing] }
            ]
        };
        _from = _from with { EventContract = SemanticId.Create(SemanticKind.EventContract, "missing-event") };
    }

    void Because() => Evaluate();

    [Fact] void should_report_the_first_failing_block() => ShouldExplain("From block #1 for event 'ProjectRegistered'");
    [Fact] void should_report_the_first_failing_mapping() => ShouldExplain("mapping #1 'name' (Set) cannot render: source '$context.first' has no admitted Chronicle mapping");
    [Fact] void should_not_report_later_failures() => _diagnostic.Message.Contains("missing-event", StringComparison.Ordinal).ShouldBeFalse();
}
