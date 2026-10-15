// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_mixed_optional_validated_concepts : given.a_command_model
{
    void Establish() => SetModel(("concept Display : String\n  validate\n    length == 3\nconcept Required : String\n  validate\n    not empty\ntype Details\n  display Display optional\n  required Required optional\ntype Pair\n  left Details optional\n  right Details optional\n" + Source).Replace("        produces InvoiceIssued", "        details Pair optional\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_name_only_blocking_paths_in_both_branches() => Messages.ShouldContainOnly(["Command 'IssueInvoice' composite property 'details' contains optional validated concept properties 'details.left.required (Required), details.right.required (Required)'; Arc graph validation skips null concepts and cannot preserve the reference evaluator's nested concept rules. Make the nested validated concept non-optional only if absence is not part of the contract, or keep this shape unsupported (Cratis/Stage#409)."]);
    [Fact] void should_keep_the_specific_command_diagnostic() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
