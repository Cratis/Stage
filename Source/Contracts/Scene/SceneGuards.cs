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
    /// <summary>The legacy guarded interaction binding path.</summary>
    [Obsolete("Use typed Scene guard selections instead of the legacy guard path.")]
    public const string GuardPath = "$guard";

    /// <summary>The legacy subject reference key.</summary>
    [Obsolete("Use the typed SubjectReferenceFor API instead.")]
    public const string SubjectKey = "subject";

    /// <summary>The prefix of a reference to the interaction's subject.</summary>
    public const string SubjectPrefix = "item.";

    /// <summary>The Stage validation diagnostic for a literal outside its declared enumeration.</summary>
    public const string UnknownEnumerationLiteralDiagnosticCode = "STAGE-SCENE-GUARD-001";
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter<GuardOperator>(), new JsonStringEnumConverter<LogicalGuardOperator>() }
    };

    /// <summary>Creates a subject reference, not a render-time binding.</summary>
    /// <param name="path">The authored subject path.</param>
    /// <returns>The reference.</returns>
    public static SubjectReference SubjectReferenceFor(string path) => new(path);

    /// <summary>Creates a legacy dictionary-shaped subject reference.</summary>
    /// <param name="path">The authored subject path.</param>
    /// <returns>The legacy reference.</returns>
    [Obsolete("Use the typed SubjectReferenceFor API instead.")]
    public static IDictionary<string, object?> Subject(string path) => SceneGuardConversion.LegacySubject(SubjectReferenceFor(path));

    /// <summary>Converts a predicate to its legacy dictionary shape.</summary>
    /// <param name="condition">The authored condition.</param>
    /// <returns>The legacy predicate.</returns>
    [Obsolete("Use the typed ConvertToCondition API instead.")]
    public static IDictionary<string, object?> Convert(ScreenplaySyntax.ConditionSyntax condition) => SceneGuardConversion.From(condition, null).ToLegacy();

    /// <summary>Converts a predicate to its legacy dictionary shape, including enumeration members.</summary>
    /// <param name="condition">The authored condition.</param>
    /// <param name="enumerationOf">Resolves enumeration members for a subject path.</param>
    /// <returns>The legacy predicate.</returns>
    [Obsolete("Use the typed ConvertToCondition API instead.")]
    public static IDictionary<string, object?> Convert(ScreenplaySyntax.ConditionSyntax condition, Func<string, IReadOnlyList<string>?>? enumerationOf) =>
        SceneGuardConversion.From(condition, enumerationOf).ToLegacy();

    /// <summary>Converts a predicate without evaluating it.</summary>
    /// <param name="condition">The authored condition.</param>
    /// <param name="enumerationOf">Resolves enumeration members for a subject path.</param>
    /// <returns>The typed predicate; unsupported syntax remains unevaluable.</returns>
    public static GuardCondition ConvertToCondition(ScreenplaySyntax.ConditionSyntax condition, Func<string, IReadOnlyList<string>?>? enumerationOf = null) =>
        SceneGuardConversion.From(condition, enumerationOf).ToCondition();

    /// <summary>Whether Scene understands an authored predicate.</summary>
    /// <param name="condition">The authored predicate.</param>
    /// <returns>Whether it is evaluable.</returns>
    public static bool CanEvaluate(ScreenplaySyntax.ConditionSyntax condition) => CanEvaluate(ConvertToCondition(condition));

    /// <summary>Whether Scene understands a converted predicate and it passes Stage's authored enumeration validation.</summary>
    /// <param name="condition">The predicate or its JSON representation.</param>
    /// <returns>Whether it is evaluable.</returns>
    public static bool CanEvaluate(object? condition)
    {
        var serialized = Serialize(condition);
        return GuardConditions.CanEvaluate(serialized) && EnumerationMembershipDiagnostic(serialized) is null;
    }

    /// <summary>Validates Stage's stricter authored enumeration membership contract, separately from Scene capability.</summary>
    /// <param name="condition">The predicate or its JSON representation.</param>
    /// <returns>The Stage diagnostic code for an unknown literal, or null when no membership violation is present.</returns>
    public static string? EnumerationMembershipDiagnostic(object? condition) => EnumerationMembershipDiagnostic(Serialize(condition));

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
        if (!alternatives.All(CanRunBranch)) return false;
        return CanRunFallback(properties["otherwise"]);
    }

    /// <summary>Checks a serialized Scene for unsupported guards.</summary>
    /// <param name="json">The Scene document.</param>
    /// <returns>Whether any guard is unevaluable.</returns>
    public static bool HasUnevaluableGuard(string json) => HasUnevaluableGuard(JsonNode.Parse(json));

    /// <summary>Recognizes a guarded action component.</summary>
    /// <param name="element">The component.</param>
    /// <returns>Whether it carries guarded branches.</returns>
    public static bool IsGuardedAction(JsonObject element) =>
        (Text(element["componentName"]) == "core:guarded-action" || Text(element["componentName"]) == "core:action") &&
        element["properties"] is JsonObject properties && (properties.ContainsKey("alternatives") || properties.ContainsKey("otherwise"));

    /// <summary>Refuses legacy guard envelopes.</summary>
    /// <param name="condition">The binding.</param>
    /// <returns>Whether it is not a legacy guard envelope.</returns>
    public static bool CanRunBinding(JsonNode? condition) => condition is not JsonObject binding || Text(binding["path"]) != "$guard";

    static bool CanRunBranch(JsonNode? node) =>
        node is JsonObject branch && Text(branch["command"]) is { Length: > 0 } && CanEvaluate(branch["condition"]);

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    static bool CanRunFallback(JsonNode? node) => node switch
    {
        null => true,
        JsonObject fallback => Text(fallback["outcome"]) switch
        {
            "Hidden" => true,
            "Execute" => Text(fallback["command"]) is { Length: > 0 },
            _ => false
        },
        _ => false
    };

    static JsonElement Serialize(object? condition) => condition is GuardCondition guard
        ? JsonSerializer.SerializeToElement(guard, _options)
        : JsonSerializer.SerializeToElement(condition, _options);

    static string? EnumerationMembershipDiagnostic(JsonElement condition)
    {
        if (condition.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        if (IsLogical(condition))
        {
            return LogicalSideDiagnostic(condition, "left") ?? LogicalSideDiagnostic(condition, "right");
        }
        if (!TryGetEnumerationComparison(condition, out var enumeration, out var literal))
        {
            return null;
        }
        return IsEnumerationMember(literal, enumeration) ? null : UnknownEnumerationLiteralDiagnosticCode;
    }

    static bool IsLogical(JsonElement condition) => condition.TryGetProperty("kind", out var kind) &&
        kind.ValueKind == JsonValueKind.String && kind.GetString() == "logical";

    static string? LogicalSideDiagnostic(JsonElement condition, string side) =>
        condition.TryGetProperty(side, out var value) ? EnumerationMembershipDiagnostic(value) : null;

    static bool TryGetEnumerationComparison(JsonElement condition, out JsonElement enumeration, out JsonElement literal)
    {
        literal = default;
        if (!condition.TryGetProperty("enumeration", out enumeration)) return false;
        if (!IsStringEnumeration(enumeration)) return false;
        return condition.TryGetProperty("right", out literal) && literal.ValueKind != JsonValueKind.Null;
    }

    static bool IsStringEnumeration(JsonElement enumeration) => enumeration.ValueKind == JsonValueKind.Array &&
        enumeration.GetArrayLength() > 0 && !enumeration.EnumerateArray().Any(member => member.ValueKind != JsonValueKind.String);

    static bool IsEnumerationMember(JsonElement literal, JsonElement enumeration) => literal.ValueKind switch
    {
        JsonValueKind.String => enumeration.EnumerateArray().Any(member => string.Equals(member.GetString(), literal.GetString(), StringComparison.OrdinalIgnoreCase)),
        JsonValueKind.Number => literal.TryGetDouble(out var ordinal) && double.IsFinite(ordinal) && ordinal == Math.Truncate(ordinal) && ordinal >= 0 && ordinal < enumeration.GetArrayLength(),
        _ => false
    };

    static bool HasUnevaluableGuard(JsonNode? node) => node switch
    {
        JsonObject element when IsGuardedAction(element) && !CanRunAction(element["properties"] as JsonObject) => true,
        JsonObject binding when HasUnevaluableInteraction(binding) => true,
        JsonObject element => element.Any(property => HasUnevaluableGuard(property.Value)),
        JsonArray array => array.Any(HasUnevaluableGuard),
        _ => false
    };

    static bool HasUnevaluableInteraction(JsonObject binding) => binding.ContainsKey("trigger") &&
        (!CanRunBinding(binding["condition"]) || HasUnevaluableAlternatives(binding["alternatives"]));

    static bool HasUnevaluableAlternatives(JsonNode? node) => node is JsonArray alternatives &&
        alternatives.Any(branch => !CanEvaluate(branch?["condition"]));
}
