// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_firing_startup_and_shutdown;

public class with_an_opaque_body_still_refused : when_settling_reactions.given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Startup\n    ```csharp\n    return [];\n    ```", "then no events", new(Action: "when trigger Startup"));
    Task Because() => Execute();
    [Fact] void should_refuse_as_the_reference_does() => AssertMatchesReference();
    [Fact] void should_keep_the_opaque_body_refusal() => _result.Unsupported!.Details.ShouldContain("opaque body");
}
