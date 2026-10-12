// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_an_unauthorized_invocation : given.a_reaction_plan
{
    void Establish() => Compile("reaction Worker\n  when Started\n    item\n    invokes Complete\n      item = item", "then denied", new() { Policy = "policy Access\n  require role \"Automation\"", Authorization = "authorize Access", Caller = "given caller\n  authenticated\n  role \"Automation\"" });
    Task Because() => Execute();
    [Fact] void should_deny_without_forwarding_the_given_caller() => AssertMatchesReference();
    [Fact] void should_report_the_authorization_rejection() => _result.Trace!.Rejection.ShouldEqual("Caller is not authorized.");
}
