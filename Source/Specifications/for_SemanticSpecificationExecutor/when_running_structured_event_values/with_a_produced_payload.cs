// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_structured_event_values;

public class with_a_produced_payload : given.a_structured_event
{
    void Establish() => Compile(action: "command", facts: "then Recorded\n          for \"one\"\n          count = 7\n          details\n            tags none\n          numbers\n            item 1\n            item 2\n          entries\n            item\n              label = \"first\"\n            item\n              tags none\n            item empty");
    Task Because() => Execute();
    [Fact] void should_compare_the_structured_fact() => AssertOutcome(SemanticSpecificationOutcome.Passed);
}
