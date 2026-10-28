// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene.for_SceneRuntimeAdmission.given;

/// <summary>A typed data subject and a guard, built directly so rejected compiler forms reach Stage admission.</summary>
public static class contains_guard_application
{
    /// <summary>Creates a model exercising typed admission on either surface.</summary>
    /// <param name="fieldType">The authored field type, or Unknown for an unresolved type.</param>
    /// <param name="action">Whether to use a guarded screen action.</param>
    /// <param name="literal">The literal compared to the field.</param>
    /// <returns>The authored application syntax.</returns>
    public static ApplicationSyntax Create(string fieldType, bool action, object? literal)
    {
        var at = SourceLocation.Start;
        var type = new TypeRefSyntax(fieldType.TrimEnd('[', ']'), fieldType.EndsWith("[]", StringComparison.Ordinal), false, at);
        var model = new ReadModelSyntax("GuardSubject", [new PropertySyntax("value", type, at)], at);
        var condition = new ComparisonConditionSyntax("item.value", ComparisonOperator.Contains, new LiteralExpressionSyntax(literal, at), at);
        var binding = new InteractionBindingSyntax(new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.DoubleClick, at), null, [], at)
        {
            Alternatives = [new InteractionAlternativeSyntax(condition, [new NavigateActionSyntax("Details", at)], at)],
        };
        ScreenDirectiveSyntax guard = action
            ? new ScreenGuardedActionSyntax("Apply", [new ScreenActionAlternativeSyntax(condition, "Apply", at)], at)
            : new ScreenBehaviorSyntax(new BehaviorSyntax(null, [], [binding], at), at);
        var screen = new ScreenSyntax("Guards", null, [new ScreenDataSyntax(new("GuardSubject", false, false, at), "GuardSubject", null, at), guard], at);
        var slice = new SliceSyntax(Cratis.Screenplay.Syntax.SliceType.StateView, "Guards", [], [], [], [], [], [], [screen], [], [], at, ReadModels: [model]);
        return new([], [], [], [new ModuleSyntax("Guards", [], [new FeatureSyntax("Guards", [], [slice], at)], at)], at);
    }
}
