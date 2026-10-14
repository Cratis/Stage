// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class without_an_outcome : given.a_command_model
{
    void Establish() => _command = _command with { Produces = [], Response = null };
    void Because() => Evaluate();

    [Fact] void should_explain_the_missing_outcome_and_fix() => Messages.ShouldContainOnly(["Command 'IssueInvoice' has no outcome; Stage cannot emit a handler without a produced event or response. Add a produces or returns clause."]);
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
