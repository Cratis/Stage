// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Scene.Engine.Guards;
using Cratis.Scene.Model.Guards;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>Translates Screenplay predicates into Scene's closed guard vocabulary.</summary>
public static class SceneGuards
{
    /// <summary>The prefix of a reference to the interaction's subject.</summary>
    public const string SubjectPrefix = "item.";
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter<GuardOperator>(), new JsonStringEnumConverter<LogicalGuardOperator>() }
    };

    /// <summary>Creates a subject reference, not a render-time binding.</summary>
    /// <param name="path">The authored subject path.</param>
    /// <returns>The reference.</returns>
    public static SubjectReference Subject(string path) => new(path);

    /// <summary>Converts a predicate without evaluating it.</summary>
    /// <param name="condition">The authored condition.</param>
    /// <param name="enumerationOf">Resolves enumeration members for a subject path.</param>
    /// <returns>The typed predicate; unsupported syntax remains unevaluable.</returns>
    public static GuardCondition Convert(ScreenplaySyntax.ConditionSyntax condition, Func<string, IReadOnlyList<string>?>? enumerationOf = null) => condition switch
    {
        ScreenplaySyntax.ComparisonConditionSyntax comparison => new ComparisonGuardCondition(
            Subject(comparison.Left),
            Enum.TryParse<GuardOperator>(comparison.Operator.ToString(), out var operation) ? operation : (GuardOperator)(-1),
            comparison.Right is ScreenplaySyntax.LiteralExpressionSyntax literal ? literal.Value : new { unsupported = comparison.Right.GetType().Name },
            enumerationOf?.Invoke(comparison.Left)),
        ScreenplaySyntax.LogicalConditionSyntax logical => new LogicalGuardCondition(
            Convert(logical.Left, enumerationOf),
            Enum.TryParse<LogicalGuardOperator>(logical.Operator.ToString(), out var operation) ? operation : (LogicalGuardOperator)(-1),
            Convert(logical.Right, enumerationOf)),
        _ => new ComparisonGuardCondition(Subject(string.Empty), GuardOperator.Equal, null)
    };

    /// <summary>Whether Scene understands an authored predicate.</summary>
    /// <param name="condition">The authored predicate.</param>
    /// <returns>Whether it is evaluable.</returns>
    public static bool CanEvaluate(ScreenplaySyntax.ConditionSyntax condition) => CanEvaluate(Convert(condition));

    /// <summary>Whether Scene understands a converted predicate.</summary>
    /// <param name="condition">The predicate or its JSON representation.</param>
    /// <returns>Whether it is evaluable.</returns>
    public static bool CanEvaluate(object? condition) => condition is GuardCondition guard
        ? GuardConditions.CanEvaluate(JsonSerializer.SerializeToElement(guard, _options))
        : GuardConditions.CanEvaluate(JsonSerializer.SerializeToElement(condition, _options));

    /// <summary>Whether the component's branches can be run by Scene.</summary>
    /// <param name="properties">The component properties.</param>
    /// <returns>Whether the branches and fallback are understood.</returns>
    public static bool CanRunAction(IReadOnlyDictionary<string, object?> properties) => CanRunAction(JsonSerializer.SerializeToNode(properties, _options) as JsonObject);

    /// <summary>Whether serialized branches can be run by Scene.</summary>
    /// <param name="properties">The component properties.</param>
    /// <returns>Whether the branches and fallback are understood.</returns>
    public static bool CanRunAction(JsonObject? properties)
    {
        if (properties?["alternatives"] is not JsonArray { Count: > 0 } alternatives) return false;
        if (!alternatives.All(branch => branch?["command"]?.GetValue<string>() is { Length: > 0 } && CanEvaluate(branch?["condition"]))) return false;
        return properties["otherwise"] is not JsonObject fallback ||
            fallback["outcome"]?.GetValue<string>() == "Hidden" ||
            (fallback["outcome"]?.GetValue<string>() == "Execute" && fallback["command"]?.GetValue<string>() is { Length: > 0 });
    }

    /// <summary>Checks a serialized Scene for unsupported guards.</summary>
    /// <param name="json">The Scene document.</param>
    /// <returns>Whether any guard is unevaluable.</returns>
    public static bool HasUnevaluableGuard(string json) => HasUnevaluableGuard(JsonNode.Parse(json));

    /// <summary>Recognizes a guarded action component.</summary>
    /// <param name="element">The component.</param>
    /// <returns>Whether it carries guarded branches.</returns>
    public static bool IsGuardedAction(JsonObject element) =>
        (element["componentName"]?.GetValue<string>() == "core:guarded-action" || element["componentName"]?.GetValue<string>() == "core:action") &&
        element["properties"] is JsonObject properties && (properties.ContainsKey("alternatives") || properties.ContainsKey("otherwise"));

    /// <summary>Refuses legacy guard envelopes.</summary>
    /// <param name="condition">The binding.</param>
    /// <returns>Whether it is not a legacy guard envelope.</returns>
    public static bool CanRunBinding(JsonNode? condition) => condition?["path"]?.GetValue<string>() != "$guard";

    static bool HasUnevaluableGuard(JsonNode? node) => node switch
    {
        JsonObject element when IsGuardedAction(element) && !CanRunAction(element["properties"] as JsonObject) => true,
        JsonObject binding when binding.ContainsKey("trigger") && (!CanRunBinding(binding["condition"]) ||
            (binding["alternatives"] is JsonArray alternatives && alternatives.Any(branch => !CanEvaluate(branch?["condition"])))) => true,
        JsonObject element => element.Any(property => HasUnevaluableGuard(property.Value)),
        JsonArray array => array.Any(HasUnevaluableGuard),
        _ => false
    };
}
