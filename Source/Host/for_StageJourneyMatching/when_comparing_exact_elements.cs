// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageJourneyMatching.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageJourneyMatching;

public class when_comparing_exact_elements : a_comparison
{
    string _lengthReason = string.Empty;

    void Because()
    {
        Compare("""{ "items": [1, 2] }""", """{ "items": [1, 2, 3] }""", StageJourneyListMatch.Exact);
        _lengthReason = _mismatches.Single().Reason;
        Compare("""{ "items": [{ "name": "a" }, { "name": "b" }] }""", """{ "items": [{ "name": "a" }, { "name": "c" }] }""", StageJourneyListMatch.Exact);
    }

    [Fact] void should_refuse_another_length() => _lengthReason.ShouldEqual("expected 2 elements, found 3");
    [Fact] void should_report_the_nested_path() => _mismatches.Single().Path.ShouldEqual("$.items[1].name");
    [Fact] void should_report_the_expected_value() => _mismatches.Single().Expected!.Value.GetString().ShouldEqual("b");
    [Fact] void should_report_the_actual_value() => _mismatches.Single().Actual!.Value.GetString().ShouldEqual("c");
}
