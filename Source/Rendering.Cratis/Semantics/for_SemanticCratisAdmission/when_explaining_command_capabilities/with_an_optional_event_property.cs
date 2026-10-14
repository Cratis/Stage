// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_optional_event_property : given.a_command_model
{
    void Establish() => SetModel(Source.Replace("        description String", "        description String?", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_name_the_event_property_and_cratis_rule_and_fix() => Messages.ShouldContainOnly(["Command 'IssueInvoice' event 'InvoiceIssued' property 'description' is optional; Cratis events have no optional properties. Make it required (in Studio's model board, tick 'Required' on the property) or remove it."]);
    [Fact] void should_keep_the_code_and_command_identity() => _diagnostics.Single().ShouldMatch(diagnostic => diagnostic.Code == "STAGE-ESM-005" && diagnostic.Artifact == _command.Id);
}
