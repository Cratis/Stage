// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_a_rule_on_named_concept_values : given.a_command_model
{
    void Establish() => SetModel(("concept Status : Enum\n  draft\n  issued\n" + Source).Replace("        produces InvoiceIssued", "        status Status\n        validate\n          status != \"draft\"\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_name_the_rule_property_and_enum_limitation_and_fix() => Messages.ShouldContainOnly(["Command 'IssueInvoice' validation 'NotEqual' on property 'status' uses concept 'Status' with named values; Stage renders these values as an enum and cannot apply the underlying-value predicate faithfully. Remove the command validation if the named values already express the rule; otherwise keep this combination unsupported (Cratis/Stage#409)."]);
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
