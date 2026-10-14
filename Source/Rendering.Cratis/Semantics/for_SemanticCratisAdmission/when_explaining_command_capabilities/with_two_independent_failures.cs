// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_two_independent_failures : given.a_command_model
{
    void Establish()
    {
        var @event = _slice.Events.Single();
        var property = @event.Properties.Single();
        _slice = _slice with { Events = [@event with { Properties = [property with { Type = property.Type with { IsOptional = true } }] }] };
        _command = _command with { Produces = [], Response = null };
    }

    void Because() => Evaluate();

    [Fact] void should_report_both_reasons_and_fixes_in_one_pass() => Messages.ShouldEqual([
        "Command 'IssueInvoice' event 'InvoiceIssued' property 'description' is optional; Cratis events have no optional properties. Make it required (in Studio's model board, tick 'Required' on the property) or remove it.",
        "Command 'IssueInvoice' has no outcome; Stage cannot emit a handler without a produced event or response. Add a produces or returns clause."]);
    [Fact] void should_keep_both_codes_and_command_identities() => _diagnostics.ShouldEachConformTo(diagnostic => diagnostic.Code == "STAGE-ESM-005" && diagnostic.Artifact == _command.Id);
}
