// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Lifecycles;

/// <summary>
/// Describes the ability a lifecycle invokes and composes the input it is given.
/// </summary>
/// <remarks>
/// A workflow action or a state entry maps each ability input from a literal, from a member of the subject read model, or from a field
/// an earlier workflow node returned. The description is plain JSON, so a workflow node carries it in its configuration and composes the
/// input from what the run recorded, without the model.
/// </remarks>
public static class AbilityInputs
{
    /// <summary>
    /// The prefix of the capability of an ability node.
    /// </summary>
    public const string CapabilityPrefix = "ability:";

    /// <summary>
    /// The outlet an ability leaves through when it returned without a declared outcome. A workflow model writes it as <c>next</c>.
    /// </summary>
    public const string ReturnedOutlet = "returned";

    const string InputMember = "input";
    const string SubjectMember = "subject";
    const string NodeMember = "node";
    const string FieldMember = "field";
    const string ValueMember = "value";

    /// <summary>
    /// Gets the capability that performs an ability.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="ability">The name of the ability.</param>
    /// <returns>The capability.</returns>
    public static string Capability(string system, string ability) => $"{CapabilityPrefix}{system}.{ability}";

    /// <summary>
    /// Finds the system and the ability a lifecycle invokes.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="system">The system.</param>
    /// <param name="ability">The name of the ability.</param>
    /// <returns>The system and the ability, or null when the model declares neither.</returns>
    public static (SemanticSystem System, SemanticAbility Ability)? Find(SemanticApplication application, SemanticId system, string? ability)
    {
        var declaring = application.Systems.FirstOrDefault(candidate => candidate.Id == system);
        var declared = declaring?.Abilities.FirstOrDefault(candidate => candidate.Name == ability);
        return declaring is null || declared is null ? null : (declaring, declared);
    }

    /// <summary>
    /// Gets the outlets of an ability: what it returned, each declared outcome, a failure and, when it answers later, an expiry.
    /// </summary>
    /// <param name="ability">The ability.</param>
    /// <returns>The outlets.</returns>
    public static ImmutableArray<string> Outlets(SemanticAbility ability) =>
        ability.AnswersLater ? [ReturnedOutlet, .. ability.Outcomes, "failed", "expired"] : [ReturnedOutlet, .. ability.Outcomes, "failed"];

    /// <summary>
    /// Describes how the input of an ability is composed.
    /// </summary>
    /// <param name="inputs">The input mappings.</param>
    /// <returns>The description, or null when an input reads something the runtime cannot compose.</returns>
    public static JsonArray? Describe(IEnumerable<SemanticAbilityInputMapping> inputs)
    {
        var description = new JsonArray();
        foreach (var input in inputs)
        {
            if (Source(input.Input, input.Source) is not { } source) return null;
            description.Add(source);
        }

        return description;
    }

    /// <summary>
    /// Composes the input of an ability from its description.
    /// </summary>
    /// <param name="description">The description <see cref="Describe"/> made.</param>
    /// <param name="subject">The subject read model instance.</param>
    /// <param name="nodes">The outputs of the nodes a workflow run already completed, by node name.</param>
    /// <returns>The input. An input whose source has no value is left out.</returns>
    public static JsonObject Compose(JsonArray description, JsonObject? subject, JsonObject? nodes)
    {
        var composed = new JsonObject();
        foreach (var entry in description.OfType<JsonObject>())
        {
            var (found, value) = Read(entry, subject, nodes);
            if (found) composed[Text(entry, InputMember)] = value?.DeepClone();
        }

        return composed;
    }

    static (bool Found, JsonNode? Value) Read(JsonObject entry, JsonObject? subject, JsonObject? nodes)
    {
        if (entry.TryGetPropertyValue(ValueMember, out var literal)) return (true, literal);
        if (entry.ContainsKey(SubjectMember)) return Member(subject, Text(entry, SubjectMember));
        return Member(Member(nodes, Text(entry, NodeMember)).Value as JsonObject, Text(entry, FieldMember));
    }

    static string Text(JsonObject entry, string member) => entry[member]?.GetValue<string>() ?? string.Empty;

    static JsonObject? Source(string input, SemanticExpression expression) => expression switch
    {
        SemanticValueExpression literal when IsLiteral(literal.Value) => new JsonObject { [InputMember] = input, [ValueMember] = Literal(literal.Value) },
        SemanticInvocationValueExpression { Source: SubjectMember } read => new JsonObject { [InputMember] = input, [SubjectMember] = read.Member },
        SemanticInvocationValueExpression { Source: NodeMember } read => NodeField(input, read.Member),
        _ => null
    };

    static JsonObject? NodeField(string input, string member)
    {
        var dot = member.IndexOf('.', StringComparison.Ordinal);
        return dot > 0 && dot < member.Length - 1
            ? new JsonObject { [InputMember] = input, [NodeMember] = member[..dot], [FieldMember] = member[(dot + 1)..] }
            : null;
    }

    static bool IsLiteral(SemanticValue value) => value is SemanticNullValue or SemanticTextValue or SemanticNumberValue or SemanticBooleanValue;

    static JsonValue? Literal(SemanticValue value) => value switch
    {
        SemanticTextValue text => JsonValue.Create(text.Value),
        SemanticNumberValue number => JsonValue.Create(number.Value),
        SemanticBooleanValue boolean => JsonValue.Create(boolean.Value),
        _ => null
    };

    static (bool Found, JsonNode? Value) Member(JsonObject? source, string name)
    {
        if (source is null) return (false, null);
        foreach (var (key, value) in source)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) return (true, value);
        }

        return (false, null);
    }
}
