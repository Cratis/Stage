// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_optional_numeric_concept_rule : given.a_command_model
{
    void Establish() => SetModel(("concept Count : Int\n" + Source).Replace("        produces InvoiceIssued", "        count Count?\n        validate\n          count > 0\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_name_the_rule_property_and_nullable_predicate_limitation() => Messages.ShouldContainOnly(["Command 'IssueInvoice' validation 'GreaterThan' on optional property 'count' requires nullable WholeNumber concept 'Count' comparisons; Stage does not yet support faithful underlying-value predicates for this shape. Make the property non-optional only if absence is not part of the contract, or keep this shape unsupported (Cratis/Stage#409)."]);
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
