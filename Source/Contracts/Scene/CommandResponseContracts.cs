// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Describes command responses and their modeled destinations for Scene's response validator.
/// Unsupported types are absent rather than being treated as an untyped successful response.
/// </summary>
/// <param name="application">The executable semantic application.</param>
/// <param name="numericEnumerations">Whether the transport emits numeric enumeration values.</param>
public sealed class CommandResponseContracts(SemanticApplication application, bool numericEnumerations = false)
{
    static readonly Dictionary<SemanticPrimitiveType, string> _primitiveKinds = new()
    {
        [SemanticPrimitiveType.Boolean] = "boolean",
        [SemanticPrimitiveType.WholeNumber] = "number",
        [SemanticPrimitiveType.DecimalNumber] = "number",
        [SemanticPrimitiveType.Uuid] = "string",
        [SemanticPrimitiveType.Text] = "string",
        [SemanticPrimitiveType.Date] = "string",
        [SemanticPrimitiveType.DateTime] = "string"
    };

    /// <summary>
    /// Builds the response contracts used by the native form runtime.
    /// </summary>
    /// <param name="scene">The translated screens whose data queries declare navigation inputs.</param>
    /// <param name="responseMember">The emitted name of a named response field.</param>
    /// <param name="commandMember">The emitted command property name.</param>
    /// <returns>The serializable Scene response and destination contracts.</returns>
    public JsonObject From(JsonObject scene, Func<string, string> responseMember, Func<SemanticProperty, string> commandMember)
    {
        var slices = application.Modules.SelectMany(module => Slices(module.Features)).ToArray();
        var (responses, inputs) = Commands(slices, responseMember, commandMember);
        var queries = slices.SelectMany(slice => slice.Queries).GroupBy(query => query.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        return new() { ["commands"] = responses, ["commandInputs"] = inputs, ["screenInputs"] = Screens(scene, queries) };
    }

    static JsonObject Wrap(SemanticTypeReference reference, string kind)
    {
        JsonObject type = new() { ["kind"] = kind };
        if (reference.IsCollection) type = new() { ["kind"] = "array", ["item"] = type };
        if (reference.IsOptional) type["nullable"] = true;
        return type;
    }

    static IEnumerable<SemanticSlice> Slices(IEnumerable<SemanticFeature> features) => features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));

    static IEnumerable<JsonObject> Objects(JsonNode? node) => node switch
    {
        JsonObject value => new[] { value }.Concat(value.SelectMany(pair => Objects(pair.Value))),
        JsonArray array => array.SelectMany(Objects),
        _ => []
    };

    static string? Text(JsonObject node, string name) => node[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    (JsonObject Responses, JsonObject Inputs) Commands(IEnumerable<SemanticSlice> slices, Func<string, string> responseMember, Func<SemanticProperty, string> commandMember)
    {
        var commands = slices.SelectMany(slice => slice.Commands).GroupBy(command => command.Name, StringComparer.Ordinal).Select(group => group.First()).ToArray();
        var responses = new JsonObject();
        var inputs = new JsonObject();
        foreach (var command in commands)
        {
            responses[command.Name] = Response(command.Response, responseMember);
            inputs[command.Name] = Properties(command.Properties.Where(property => !property.IsGenerated), commandMember);
        }
        return (responses, inputs);
    }

    JsonObject Screens(JsonObject scene, IReadOnlyDictionary<string, SemanticKeyedQuery> queries)
    {
        var screens = new JsonObject();
        foreach (var screen in (scene["screens"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var parameters = ScreenInputs(screen, queries);
            if (Text(screen, "name") is { } screenName) screens[screenName] = parameters;
        }
        return screens;
    }

    JsonObject ScreenInputs(JsonObject screen, IReadOnlyDictionary<string, SemanticKeyedQuery> queries)
    {
        var parameters = new JsonObject();
        foreach (var data in Objects(screen).Where(component => Text(component, "componentName") == "core:data").Select(component => component["properties"]).OfType<JsonObject>())
        {
            AddQueryInput(parameters, data, queries);
        }
        return parameters;
    }

    void AddQueryInput(JsonObject parameters, JsonObject data, IReadOnlyDictionary<string, SemanticKeyedQuery> queries)
    {
        if (Text(data, "query") is not { } name || !queries.TryGetValue(name, out var query)) return;
        if (query.Argument is not { } argument || Text(data, "by") is not { } by) return;
        parameters[by] = Type(argument.Type);
    }

    JsonObject? Response(SemanticCommandResponse? response, Func<string, string> member)
    {
        if (response is null) return new() { ["kind"] = "void" };
        var type = response switch
        {
            SemanticScalarCommandResponse scalar => Type(scalar.Type),
            SemanticRecordCommandResponse record => Record(record.Fields, member),
            _ => null
        };
        return type is null ? null : new() { ["kind"] = "value", ["type"] = type };
    }

    JsonObject? Record(IEnumerable<SemanticCommandResponseField> fields, Func<string, string> member)
    {
        var properties = new JsonObject();
        foreach (var field in fields)
        {
            if (Type(field.Type) is not { } type) return null;
            properties[member(field.Name)] = type;
        }
        return new() { ["kind"] = "object", ["properties"] = properties };
    }

    JsonObject Properties(IEnumerable<SemanticProperty> properties, Func<SemanticProperty, string> member)
    {
        var result = new JsonObject();
        foreach (var property in properties) result[member(property)] = Type(property.Type);
        return result;
    }

    JsonObject? Type(SemanticTypeReference reference)
    {
        var kind = SupportedKind(reference);
        return kind is null ? null : Wrap(reference, kind);
    }

    string? SupportedKind(SemanticTypeReference reference)
    {
        // The first continuation slice consumes primitive responses only; do not claim composite transport parity.
        if (reference.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept)) return null;
        var concept = reference.Kind == SemanticTypeReferenceKind.Concept ? application.Concepts.Single(concept => concept.Id == reference.Target) : null;
        var primitive = concept?.Primitive ?? reference.Primitive;
        if (!_primitiveKinds.TryGetValue(primitive, out var kind)) return null;
        return numericEnumerations && concept is { Values.IsEmpty: false } ? "number" : kind;
    }
}
