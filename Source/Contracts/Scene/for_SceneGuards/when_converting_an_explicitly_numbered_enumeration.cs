// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

public class when_converting_an_explicitly_numbered_enumeration : Specification
{
    readonly ComparisonConditionSyntax _status = new("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("open", SourceLocation.Start), SourceLocation.Start);
    IDictionary<string, object?> _result = null!;

    void Because() => _result = SceneGuards.Convert(
        new LogicalConditionSyntax(_status, LogicalOperator.Or, _status with { Left = "item.phase", Right = new LiteralExpressionSyntax("draft", SourceLocation.Start) }, SourceLocation.Start),
        path => path == "item.status" ? ["open", "closed"] : ["draft", "issued", "cancelled"],
        path => path == "item.status" ? [1, 0] : [-1, 4, 90]);

    [Fact] void should_preserve_reordered_member_identity() => JsonSerializer.Serialize(_result["left"]).ShouldEqual("{\"kind\":\"comparison\",\"left\":{\"subject\":\"item.status\"},\"operator\":\"Equal\",\"right\":\"open\",\"enumeration\":[\"open\",\"closed\"],\"enumerationNumbers\":[1,0]}");
    [Fact] void should_preserve_sparse_negative_numbers_on_the_other_operand() => JsonSerializer.Serialize(((IDictionary<string, object?>)_result["right"]!)["enumerationNumbers"]).ShouldEqual("[-1,4,90]");
    [Fact] void should_preserve_member_order_on_the_other_operand() => JsonSerializer.Serialize(((IDictionary<string, object?>)_result["right"]!)["enumeration"]).ShouldEqual("[\"draft\",\"issued\",\"cancelled\"]");
    [Fact] void should_keep_the_untyped_entry_point_unchanged() => JsonSerializer.Serialize(SceneGuards.Convert(_status)).ShouldEqual("{\"kind\":\"comparison\",\"left\":{\"subject\":\"item.status\"},\"operator\":\"Equal\",\"right\":\"open\"}");
    [Fact] void should_omit_numbers_for_the_names_only_entry_point() => SceneGuards.Convert(_status, _ => ["open", "closed"]).ContainsKey("enumerationNumbers").ShouldBeFalse();
    [Fact] void should_omit_numbers_without_members() => SceneGuards.Convert(_status, _ => null, _ => [1, 0]).ContainsKey("enumerationNumbers").ShouldBeFalse();
    [Fact] void should_omit_absent_numbers() => SceneGuards.Convert(_status, _ => ["open", "closed"], _ => null).ContainsKey("enumerationNumbers").ShouldBeFalse();
}
