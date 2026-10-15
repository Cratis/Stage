// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_a_nested_optional_named_validated_concept : given.a_command_model
{
    void Establish() => SetModel(("concept Status : Enum\n  open\n  closed\n  validate\n    != closed\ntype Details\n  status Status optional\n" + Source).Replace("        produces InvoiceIssued", "        details Details\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_keep_refusing_optional_named_validated_concepts() => Messages.ShouldContainOnly(["Command 'IssueInvoice' composite property 'details' contains optional validated concept properties 'details.status (Status)'; Arc graph validation skips null concepts and cannot preserve the reference evaluator's nested concept rules. Make the nested validated concept non-optional only if absence is not part of the contract, or keep this shape unsupported (Cratis/Stage#409)."]);
    [Fact] void should_keep_the_specific_command_diagnostic() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
