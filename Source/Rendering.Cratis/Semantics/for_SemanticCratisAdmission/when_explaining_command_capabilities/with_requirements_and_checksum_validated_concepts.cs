// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_requirements_and_checksum_validated_concepts : given.a_command_model
{
    void Establish() => SetModel(("concept AccountCode : String\n  validate\n    checksum luhn\n" + Source).Replace("        produces InvoiceIssued", "        accountCode AccountCode\n        validate\n          require description != \"draft\"\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_keep_the_checksum_failure_order_refusal() => Messages.ShouldContainOnly(["Command 'IssueInvoice' requirement #1 (description) is combined with validated concept property 'accountCode'; the concept rules cannot all be rendered at the command validation level. Model equivalent rules at one validation level, or keep this combination unsupported (Cratis/Stage#840)."]);
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
