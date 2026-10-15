// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_optional_validated_concept : given.a_command_model
{
    void Establish() => SetModel(("concept Display : String\n  validate\n    not empty\n" + Source).Replace("        produces InvoiceIssued", "        display Display?\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_name_the_property_concept_and_null_validation_fix() => Messages.ShouldContainOnly(["Command 'IssueInvoice' optional property 'display' uses validated concept 'Display'; Arc graph validation skips null concepts and cannot preserve absence-rejecting concept rules. Only absence-rejecting rules block optional scalar concepts without named values. Make the property non-optional only if absence is not part of the contract, or keep this shape unsupported (Cratis/Stage#409)."]);
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
