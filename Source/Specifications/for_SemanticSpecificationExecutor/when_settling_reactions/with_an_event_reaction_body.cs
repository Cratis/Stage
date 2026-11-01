// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_event_reaction_body : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    ```csharp\n    return null;\n    ```", "then no events");
    Task Because() => Execute();
    [Fact] void should_refuse_as_the_reference_does() => AssertMatchesReference();
    [Fact] void should_be_unsupported_not_passed() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_the_opaque_event_body() => _result.Unsupported!.Details.ShouldContain("STAGE-ESM-024: Reaction 'Worker' has an opaque body for trigger kind '");
}
