// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Validation.for_StageRuleEvaluator.when_evaluating_not_empty;

public class with_a_nested_property : given.a_modeled_rule
{
    IReadOnlyList<StageRuleViolation>[] _violations = null!;

    void Because() => _violations = [.. new[]
    {
        """{"customer":{"address":{"name":"Ada"}}}""",
        """{"customer":{"address":{"name":""}}}""",
        """{"customer":{"address":{"name":"  "}}}""",
        """{"customer":{"address":{"name":null}}}""",
        """{"customer":{"address":{}}}""",
        """{"customer":null}""",
        "{}"
    }.Select(json => StageRuleEvaluator.Evaluate(Rules("customer.address.name", ValidationRuleKind.NotEmpty), json))];

    [Fact] void should_accept_a_present_nested_value() => _violations[0].ShouldBeEmpty();
    [Fact] void should_reject_empty_or_missing_nested_values() => _violations.Skip(1).Select(violations => violations.Count).ShouldEqual([1, 1, 1, 1, 1, 1]);
    [Fact] void should_report_the_modeled_path() => _violations[1].Single().PropertyName.ShouldEqual("customer.address.name");
    [Fact] void should_preserve_the_modeled_message() => _violations[1].Single().Message.ShouldEqual("Modeled rejection");
}
