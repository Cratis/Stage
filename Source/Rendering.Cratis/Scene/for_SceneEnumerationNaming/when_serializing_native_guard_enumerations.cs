// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Guards;
using Cratis.Scene.Model.Interactions;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_SceneEnumerationNaming;

public class when_serializing_native_guard_enumerations : Specification
{
    JsonNode _wire = null!;

    void Because()
    {
        var comparison = new ComparisonGuardCondition(new("item.status"), GuardOperator.Equal, "open");
        _wire = CanonicalSceneJson.ToNode(new
        {
            comparison,
            and = new LogicalGuardCondition(comparison, LogicalGuardOperator.And, comparison),
            or = new LogicalGuardCondition(comparison, LogicalGuardOperator.Or, comparison),
            hidden = new GuardedActionFallback(GuardedActionFallbackOutcome.Hidden),
            execute = new GuardedActionFallback(GuardedActionFallbackOutcome.Execute, "CloseWorkItem"),
            trigger = new BuiltInInteractionTrigger(InteractionTriggerKind.DoubleClick),
            destination = new DestinationReference(Kind: DestinationKind.Dialog, Dialog: "Edit")
        })!;
    }

    [Fact] void should_preserve_the_comparison_operator_spelling() => _wire["comparison"]!["operator"]!.GetValue<string>().ShouldEqual("Equal");
    [Fact] void should_preserve_the_and_operator_spelling() => _wire["and"]!["operator"]!.GetValue<string>().ShouldEqual("And");
    [Fact] void should_preserve_the_or_operator_spelling() => _wire["or"]!["operator"]!.GetValue<string>().ShouldEqual("Or");
    [Fact] void should_preserve_the_hidden_fallback_spelling() => _wire["hidden"]!["outcome"]!.GetValue<string>().ShouldEqual("Hidden");
    [Fact] void should_preserve_the_execute_fallback_spelling() => _wire["execute"]!["outcome"]!.GetValue<string>().ShouldEqual("Execute");
    [Fact] void should_keep_existing_double_click_spelling() => _wire["trigger"]!["kind"]!.GetValue<string>().ShouldEqual("DoubleClick");
    [Fact] void should_keep_existing_destination_spelling() => _wire["destination"]!["kind"]!.GetValue<string>().ShouldEqual("dialog");
}
