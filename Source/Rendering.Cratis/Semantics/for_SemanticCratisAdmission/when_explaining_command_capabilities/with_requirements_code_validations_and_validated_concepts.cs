// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_requirements_code_validations_and_validated_concepts : given.a_command_model
{
    void Establish()
    {
        SetModel(("concept Display : String\n  validate\n    min 2\n" + Source).Replace("        produces InvoiceIssued", "        display Display\n        validate\n          require description != \"draft\"\n        produces InvoiceIssued", StringComparison.Ordinal));
        _command = _command with { CodeValidations = [new("validate-description")] };
    }

    void Because() => Evaluate();

    [Fact] void should_keep_the_code_validation_failure_order_refusal() => _diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-005").Select(diagnostic => diagnostic.Message).ShouldContainOnly(["Command 'IssueInvoice' requirement #1 (description) is combined with validated concept property 'display'; the command's code validations have no portable order relative to concept rules. Model equivalent rules at one validation level, or keep this combination unsupported (Cratis/Stage#840)."]);
    [Fact] void should_keep_the_independent_implementation_refusal() => _diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-020").ShouldEqual(1);
}
