// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Describes the guards of a Screenplay <c language="shell">when ... otherwise</c> as the Stage runtime evaluates them,
/// and decides which guards it can evaluate.
/// </summary>
/// <remarks>
/// <para>
/// A guard compares a field of the subject - <c language="shell">item.status</c> - with a literal using one of the
/// Screenplay comparison operators, and combines comparisons with <c language="shell">and</c> and <c language="shell">or</c>.
/// That is the shape the Stage frontend runtime evaluates against bound data (<c language="shell">stageGuards.ts</c>).
/// Any other guard is not evaluable; an action or interaction carrying one is refused with
/// <see cref="UnsupportedGuardedScreenAction.DiagnosticCode"/> or <see cref="UnsupportedGuardedInteraction.DiagnosticCode"/>
/// and never reaches the runtime, and the runtime refuses one again if it does.
/// </para>
/// <para>
/// A guarded interaction is carried as one Scene interaction binding per branch, all with the same trigger and in
/// authored order. Each binding's condition is a <see cref="Cratis.Scene.Model.Common.BindingExpression"/> with the path
/// <see cref="GuardPath"/> and a value saying which branch it is and which alternatives decide it, so the Scene
/// interaction engine runs exactly the first branch that holds for the subject, or the <c language="shell">otherwise</c>.
/// </para>
/// </remarks>
public static class SceneGuards
{
    /// <summary>
    /// The binding path of a guarded interaction branch's condition.
    /// </summary>
    public const string GuardPath = "$guard";

    /// <summary>
    /// The prefix every guard field has: the subject the guard is about.
    /// </summary>
    public const string SubjectPrefix = "item.";

    static readonly HashSet<string> _comparisonOperators = new(Enum.GetNames<ScreenplaySyntax.ComparisonOperator>(), StringComparer.Ordinal);
    static readonly HashSet<string> _logicalOperators = new(Enum.GetNames<ScreenplaySyntax.LogicalOperator>(), StringComparer.Ordinal);
    static readonly HashSet<string> _selectionKinds = new(["firstMatch", "otherwise"], StringComparer.Ordinal);

    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Converts an authored condition to the form the Scene carries.
    /// </summary>
    /// <param name="condition">The authored condition.</param>
    /// <returns>The Scene form; an unevaluable condition keeps only its kind, which no runtime evaluates.</returns>
    public static IDictionary<string, object?> Convert(ScreenplaySyntax.ConditionSyntax condition) => condition switch
    {
        ScreenplaySyntax.ComparisonConditionSyntax comparison => new Dictionary<string, object?>
        {
            ["kind"] = "comparison",
            ["left"] = new Cratis.Scene.Model.Common.BindingExpression(comparison.Left),
            ["operator"] = comparison.Operator.ToString(),
            ["right"] = comparison.Right is ScreenplaySyntax.LiteralExpressionSyntax literal ? literal.Value : new Dictionary<string, object?> { ["kind"] = comparison.Right.GetType().Name },
        },
        ScreenplaySyntax.LogicalConditionSyntax logical => new Dictionary<string, object?>
        {
            ["kind"] = "logical",
            ["left"] = Convert(logical.Left),
            ["operator"] = logical.Operator.ToString(),
            ["right"] = Convert(logical.Right),
        },
        _ => new Dictionary<string, object?> { ["kind"] = condition.GetType().Name },
    };

    /// <summary>
    /// Whether the Stage runtime can evaluate an authored condition.
    /// </summary>
    /// <param name="condition">The authored condition.</param>
    /// <returns>True when it can.</returns>
    public static bool CanEvaluate(ScreenplaySyntax.ConditionSyntax condition) => condition switch
    {
        ScreenplaySyntax.ComparisonConditionSyntax comparison =>
            comparison.Left.StartsWith(SubjectPrefix, StringComparison.Ordinal) && comparison.Left.Length > SubjectPrefix.Length &&
            Enum.IsDefined(comparison.Operator) &&
            comparison.Right is ScreenplaySyntax.LiteralExpressionSyntax { Value: null or string or bool or int or long or double or decimal or float },
        ScreenplaySyntax.LogicalConditionSyntax logical => Enum.IsDefined(logical.Operator) && CanEvaluate(logical.Left) && CanEvaluate(logical.Right),
        _ => false,
    };

    /// <summary>
    /// Whether the Stage runtime can evaluate a condition in the form the Scene carries it.
    /// </summary>
    /// <param name="condition">The condition: the converted dictionary, or the same shape as JSON.</param>
    /// <returns>True when it can.</returns>
    public static bool CanEvaluate(object? condition) => CanEvaluate(ToNode(condition));

    /// <summary>
    /// Whether a Scene guarded action's alternatives and fallback are all evaluable by the Stage runtime.
    /// </summary>
    /// <param name="properties">The action's properties.</param>
    /// <returns>True when the action can run.</returns>
    public static bool CanRunAction(IReadOnlyDictionary<string, object?> properties) =>
        CanRunAction(ToNode(properties) as JsonObject);

    /// <summary>
    /// Whether a Scene guarded action, as JSON, is evaluable by the Stage runtime.
    /// </summary>
    /// <param name="properties">The action's properties.</param>
    /// <returns>True when the action can run.</returns>
    public static bool CanRunAction(JsonObject? properties)
    {
        if (properties?["alternatives"] is not JsonArray alternatives || alternatives.Count == 0)
        {
            return false;
        }

        var branches = alternatives.All(alternative =>
            alternative is JsonObject branch && Text(branch["command"]) is { Length: > 0 } && CanEvaluate(branch["condition"]));
        var otherwise = properties["otherwise"] switch
        {
            null => true,
            JsonObject fallback => Text(fallback["outcome"]) switch
            {
                "Hidden" => true,
                "Execute" => Text(fallback["command"]) is { Length: > 0 },
                _ => false,
            },
            _ => false,
        };
        return branches && otherwise;
    }

    /// <summary>
    /// Whether a Scene interaction binding's guard, as JSON, is evaluable by the Stage runtime.
    /// </summary>
    /// <param name="condition">The binding's condition.</param>
    /// <returns>True when there is no guard or it can be evaluated.</returns>
    public static bool CanRunBinding(JsonNode? condition)
    {
        if (condition is not JsonObject binding || Text(binding["path"]) != GuardPath)
        {
            return true;
        }

        return binding["value"] is JsonObject selection &&
            _selectionKinds.Contains(Text(selection["kind"]) ?? string.Empty) &&
            selection["alternatives"] is JsonArray alternatives && alternatives.Count > 0 &&
            alternatives.All(CanEvaluate);
    }

    /// <summary>
    /// Whether a JSON Scene document carries a guarded action or interaction the Stage runtime cannot evaluate.
    /// </summary>
    /// <param name="json">The Scene document.</param>
    /// <returns>True when it does.</returns>
    public static bool HasUnevaluableGuard(string json) => HasUnevaluableGuard(JsonNode.Parse(json));

    /// <summary>
    /// Whether a JSON element is a guarded action.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>True when it is an action with alternatives or a fallback.</returns>
    public static bool IsGuardedAction(JsonObject element) =>
        Text(element["componentName"]) == "core:action" && element["properties"] is JsonObject properties &&
        (properties.ContainsKey("alternatives") || properties.ContainsKey("otherwise"));

    static bool HasUnevaluableGuard(JsonNode? node) => node switch
    {
        JsonObject element when IsGuardedAction(element) && !CanRunAction(element["properties"] as JsonObject) => true,
        JsonObject binding when binding.ContainsKey("trigger") && !CanRunBinding(binding["condition"]) => true,
        JsonObject element => element.Any(property => HasUnevaluableGuard(property.Value)),
        JsonArray array => array.Any(HasUnevaluableGuard),
        _ => false,
    };

    static bool CanEvaluate(JsonNode? condition)
    {
        if (condition is not JsonObject node)
        {
            return false;
        }

        return Text(node["kind"]) switch
        {
            "comparison" =>
                node["left"] is JsonObject left && Text(left["path"]) is { } path && path.StartsWith(SubjectPrefix, StringComparison.Ordinal) && path.Length > SubjectPrefix.Length &&
                Text(node["operator"]) is { } comparison && _comparisonOperators.Contains(comparison) &&
                (node["right"] is null || node["right"] is JsonValue),
            "logical" =>
                Text(node["operator"]) is { } logical && _logicalOperators.Contains(logical) && CanEvaluate(node["left"]) && CanEvaluate(node["right"]),
            _ => false,
        };
    }

    static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        JsonNode node => node,
        JsonElement element => JsonNode.Parse(element.GetRawText()),
        _ => JsonSerializer.SerializeToNode(value, _options),
    };

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
