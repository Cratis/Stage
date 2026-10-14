// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_a_rule_implementation_and_an_optional_event_property : given.a_command_model
{
    void Establish()
    {
        var @event = _slice.Events.Single();
        var property = @event.Properties.Single();
        _slice = _slice with { Events = [@event with { Properties = [property with { Type = property.Type with { IsOptional = true } }] }] };
        _command = _command with { Validations = [new(_command.Properties.Single(property => property.Name == "description").Id, SemanticValidationRuleKind.RulePredicate, null, null) { Name = "DescriptionIsValid", RequirementId = "description-rule" }] };
    }

    void Because() => Evaluate();

    [Fact] void should_report_the_event_and_rule_body_in_one_pass() => Messages.ShouldEqual([
        "Command 'IssueInvoice' event 'InvoiceIssued' property 'description' is optional; Cratis events have no optional properties. Make it required (in Studio's model board, tick 'Required' on the property) or remove it.",
        "Command 'IssueInvoice' validation 'DescriptionIsValid' on property 'description' has implementation body 'description-rule' that cannot be rendered: Arc does not supply RuleContext.Occurred (received-at) to a generated validator, and Stage does not yet enforce the pure capability. Use equivalent declarative validation; implementation bodies are not supported yet (Cratis/Stage#409)."]);
    [Fact] void should_keep_both_codes() => _diagnostics.ShouldEachConformTo(diagnostic => diagnostic.Code == "STAGE-ESM-005");
}
