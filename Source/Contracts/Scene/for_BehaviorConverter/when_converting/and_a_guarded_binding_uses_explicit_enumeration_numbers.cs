// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
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
        _syntax = new("CloseRow", [], [new InteractionBindingSyntax(new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.Click, at), null, [], at)
        {
            Alternatives = [new InteractionAlternativeSyntax(
                new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("open", at), at),
                [new ExecuteCommandActionSyntax("Close", at)], at)],
            Otherwise = new InteractionOtherwiseSyntax([new ExecuteCommandActionSyntax("Reopen", at)], at)
        }], at);
    }

    void Because()
    {
        var scope = new BehaviorScope(new Dictionary<string, BehaviorSyntax> { ["CloseRow"] = _syntax }, [])
        {
            Subject = "Row",
            Enumerations = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> { ["Row"] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase) { ["status"] = ["open", "closed"] } },
            EnumerationNumbers = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<int>>> { ["Row"] = new Dictionary<string, IReadOnlyList<int>>(StringComparer.OrdinalIgnoreCase) { ["status"] = [1, 0] } }
        };
        _result = scope.Resolve([_syntax], [new UsesBehaviorSyntax("CloseRow", [], SourceLocation.Start)], "Rows");
    }

    [Fact] void should_expand_both_inline_and_named_uses() => _result.Count.ShouldEqual(2);

    [Fact]
    void should_copy_numbered_conditions_to_every_selection()
    {
        foreach (var behavior in _result)
        {
            behavior.Bindings.Count.ShouldEqual(2);
            foreach (var binding in behavior.Bindings)
            {
                var selection = JsonSerializer.SerializeToNode(binding.Condition!.Value)!.AsObject();
                selection["alternatives"]![0]!["enumeration"]!.ToJsonString().ShouldEqual("[\"open\",\"closed\"]");
                selection["alternatives"]![0]!["enumerationNumbers"]!.ToJsonString().ShouldEqual("[1,0]");
                SceneGuards.CanRunBinding(new JsonObject { ["path"] = binding.Condition.Path, ["value"] = selection }).ShouldBeTrue();
            }
        }
    }

    [Fact] void should_keep_the_untyped_entry_point() => JsonSerializer.Serialize(BehaviorConverter.Convert(_syntax)).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_names_only_entry_point() => JsonSerializer.Serialize(BehaviorConverter.Convert(_syntax, _ => ["open", "closed"])).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_diagnostic_entry_point() => JsonSerializer.Serialize(BehaviorConverter.Convert(_syntax, _ => ["open", "closed"], null, "Rows")).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_untyped_attachment_entry_point() => JsonSerializer.Serialize(BehaviorAttachments.Convert([_syntax], [], new Dictionary<string, BehaviorSyntax>(), "Rows", [])).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_names_only_attachment_entry_point() => JsonSerializer.Serialize(BehaviorAttachments.Convert([_syntax], [], new Dictionary<string, BehaviorSyntax>(), "Rows", [], _ => ["open", "closed"])).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_diagnostic_attachment_entry_point() => JsonSerializer.Serialize(BehaviorAttachments.Convert([_syntax], [], new Dictionary<string, BehaviorSyntax>(), "Rows", [], _ => ["open", "closed"], null)).Contains("enumerationNumbers", StringComparison.Ordinal).ShouldBeFalse();
}
