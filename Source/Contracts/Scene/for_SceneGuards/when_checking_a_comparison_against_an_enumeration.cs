// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

public class when_checking_a_comparison_against_an_enumeration : Specification
{
    static Dictionary<string, object?> Comparison(string right) => new()
    {
        ["kind"] = "comparison",
        ["left"] = SceneGuards.Subject("item.status"),
        ["operator"] = "Equal",
        ["right"] = right,
        ["enumeration"] = new[] { "open", "closed" },
    };

    [Fact] void should_evaluate_a_member() => SceneGuards.CanEvaluate((object)Comparison("open")).ShouldBeTrue();
    [Fact] void should_evaluate_a_member_in_any_casing() => SceneGuards.CanEvaluate((object)Comparison("Closed")).ShouldBeTrue();
    [Fact] void should_refuse_a_literal_that_is_not_a_member() => SceneGuards.CanEvaluate((object)Comparison("archived")).ShouldBeFalse();
}
