// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Model.Elements;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_guarded_action_uses_explicit_enumeration_numbers : Specification
{
    ExternalComponent _result = null!;
    JsonObject _condition = null!;

    void Because()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            concept Status : Enum
              open = 1
              closed = 0
            type Row
              status Status
            """);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        var scope = BehaviorScope.None with
        {
            Subject = new(new(parsed.Value!), [], [new("Row", false, false, SourceLocation.Start)])
        };
        var at = SourceLocation.Start;
        var condition = new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("open", at), at);
        var action = new ScreenGuardedActionSyntax("Close", [new ScreenActionAlternativeSyntax(condition, "Close", at)], at);
        _result = (ExternalComponent)ScreenDirectiveConverter.Convert([action], "Rows", scope)[0];
        var alternatives = System.Text.Json.JsonSerializer.SerializeToNode(_result.Properties["alternatives"])!.AsArray();
        _condition = alternatives[0]!["condition"]!.AsObject();
    }

    [Fact] void should_carry_the_authored_names() => _condition["enumeration"]!.ToJsonString().ShouldEqual("[\"open\",\"closed\"]");
    [Fact] void should_carry_the_authored_numbers_in_matching_order() => _condition["enumerationNumbers"]!.ToJsonString().ShouldEqual("[1,0]");
    [Fact] void should_remain_runnable() => SceneGuards.CanRunAction(System.Text.Json.JsonSerializer.SerializeToNode(_result.Properties)!.AsObject()).ShouldBeTrue();
}
