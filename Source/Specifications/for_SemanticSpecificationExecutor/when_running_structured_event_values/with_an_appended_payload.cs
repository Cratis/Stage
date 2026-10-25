// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_structured_event_values;

public class with_an_appended_payload : given.a_structured_event
{
    void Establish() => Compile(action: "when append");
    Task Because() => Execute();
    [Fact] void should_evaluate_the_assertion() => AssertOutcome(SemanticSpecificationOutcome.Passed);
}
