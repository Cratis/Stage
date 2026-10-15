// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Validation.for_StageRuleEvaluator.when_evaluating_not_empty;

public class with_a_collection_element_path : given.a_modeled_rule
{
    bool[] _accepted = null!;

    void Because() => _accepted = [.. new (string Property, string Json)[]
    {
        ("lines.description", """{"lines":[{"description":"Valid"}]}"""),
        ("lines.description", """{"lines":[{"description":""}]}"""),
        ("lines.description", """{"lines":[]}"""),
        ("order.lines.description", """{"order":{"lines":[{"description":"Valid"}]}}"""),
        ("order.lines.description", """{"order":{"lines":[{"description":""}]}}"""),
        ("lines", """{"lines":[]}"""),
        ("lines", """{"lines":[{"description":"Valid"}]}""")
    }.Select(example => StageRuleEvaluator.Evaluate(Rules(example.Property, ValidationRuleKind.NotEmpty), example.Json).Count == 0)];

    [Fact] void should_skip_unsupported_element_paths_but_still_check_collection_presence() => _accepted.ShouldEqual([true, true, true, true, true, false, true]);
}
