// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_unsupported_named_value_rules : given.a_command_model
{
    [Theory]
    [InlineData(SemanticValidationRuleKind.NotEmpty)]
    [InlineData(SemanticValidationRuleKind.GreaterThan)]
    [InlineData(SemanticValidationRuleKind.LessThanOrEqual)]
    [InlineData(SemanticValidationRuleKind.Length)]
    [InlineData(SemanticValidationRuleKind.Matches)]
    public void should_keep_non_equality_rules_refused(SemanticValidationRuleKind kind)
    {
        SetModel(("concept Status : Enum\n  draft\n  issued\n" + Source).Replace("        produces InvoiceIssued", "        status Status\n        validate\n          status != draft\n        produces InvoiceIssued", StringComparison.Ordinal));

        // Exercise the defensive predicate, including rules the reference binder does not admit.
        _command = _command with { Validations = [_command.Validations.Single() with { Kind = kind }] };
        Evaluate();
        _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
        Messages.Single().ShouldContain("only scalar equality and inequality against a declared member are supported");
    }

    [Fact]
    public void should_keep_named_value_collection_rules_refused()
    {
        SetModel(("concept Status : Enum\n  draft\n  issued\n" + Source).Replace("        produces InvoiceIssued", "        status Status[] optional\n        validate\n          status not empty\n        produces InvoiceIssued", StringComparison.Ordinal));
        Evaluate();
        _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
        Messages.Single().ShouldContain("Collection rules, 'not empty', ordering, and other predicates remain unsupported");
    }
}
