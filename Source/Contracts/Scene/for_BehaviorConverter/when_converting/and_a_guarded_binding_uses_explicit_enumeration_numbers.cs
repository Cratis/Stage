// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Guards;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorConverter.when_converting;

public class and_a_guarded_binding_uses_explicit_enumeration_numbers : Specification
{
    IReadOnlyList<SceneModel.Behavior> _result = null!;
    BehaviorSyntax _syntax = null!;

    void Establish()
    {
        var at = SourceLocation.Start;
        var trigger = new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.Click, at);
        var open = new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("open", at), at);
        var alternative = new InteractionAlternativeSyntax(open, [new ExecuteCommandActionSyntax("Close", at)], at);
        var otherwise = new InteractionOtherwiseSyntax([new ExecuteCommandActionSyntax("Reopen", at)], at);
        var binding = new InteractionBindingSyntax(trigger, null, [], at) { Alternatives = [alternative], Otherwise = otherwise };
        _syntax = new("CloseRow", [], [binding], at);
    }

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
        var scope = new BehaviorScope(new Dictionary<string, BehaviorSyntax> { ["CloseRow"] = _syntax }, [])
        {
            Subject = new(new(parsed.Value!), [], [new("Row", false, false, SourceLocation.Start)])
        };
        _result = scope.Resolve([_syntax], [new UsesBehaviorSyntax("CloseRow", [], SourceLocation.Start)], "Rows");
    }

    [Fact] void should_expand_both_inline_and_named_uses() => _result.Count.ShouldEqual(2);

    [Fact]
    void should_copy_numbered_conditions_to_every_selection()
    {
        foreach (var behavior in _result)
        {
            behavior.Bindings.Count.ShouldEqual(1);
            var binding = behavior.Bindings[0];
            binding.Condition.ShouldBeNull();
            binding.Otherwise.Count.ShouldEqual(1);
            var condition = Assert.IsType<ComparisonGuardCondition>(binding.Alternatives[0].Condition);
            JsonSerializer.Serialize(condition.Enumeration).ShouldEqual("[\"open\",\"closed\"]");
            JsonSerializer.Serialize(condition.EnumerationNumbers).ShouldEqual("[1,0]");
            SceneGuards.CanEvaluate(condition).ShouldBeTrue();
        }
    }

    [Fact] void should_keep_the_untyped_entry_point() => JsonSerializer.Serialize(BehaviorConverter.Convert(_syntax)).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_names_only_entry_point() => JsonSerializer.Serialize(BehaviorConverter.Convert(_syntax, _ => ["open", "closed"])).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_diagnostic_entry_point() => JsonSerializer.Serialize(BehaviorConverter.Convert(_syntax, _ => ["open", "closed"], null, "Rows")).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_untyped_attachment_entry_point() => JsonSerializer.Serialize(BehaviorAttachments.Convert([_syntax], [], new Dictionary<string, BehaviorSyntax>(), "Rows", [])).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_names_only_attachment_entry_point() => JsonSerializer.Serialize(BehaviorAttachments.Convert([_syntax], [], new Dictionary<string, BehaviorSyntax>(), "Rows", [], _ => ["open", "closed"])).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_diagnostic_attachment_entry_point() => JsonSerializer.Serialize(BehaviorAttachments.Convert([_syntax], [], new Dictionary<string, BehaviorSyntax>(), "Rows", [], _ => ["open", "closed"], null)).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
}
