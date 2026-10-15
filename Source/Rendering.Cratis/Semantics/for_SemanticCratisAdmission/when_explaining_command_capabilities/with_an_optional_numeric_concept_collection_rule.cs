// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_optional_numeric_concept_collection_rule : given.a_command_model
{
    void Establish() => SetModel(("concept Count : Int\n" + Source).Replace("        produces InvoiceIssued", "        count Count[] optional\n        validate\n          count all > 0\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_explain_that_collection_rules_remain_unsupported() => Messages.ShouldContainOnly(["Command 'IssueInvoice' validation 'AllGreaterThan' on optional property 'count' uses WholeNumber concept 'Count'; only absence-passing scalar comparisons are supported. Collection rules and 'not empty' remain unsupported. Make the property non-optional only if absence is not part of the contract, or keep this shape unsupported (Cratis/Stage#419)."]);
    [Fact] void should_keep_the_specific_command_diagnostic() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
