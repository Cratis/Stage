// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Shares generated CLR member spelling with admission checks compiled into the specification executor.
/// </summary>
internal static class GeneratedPascalCase
{
    // The record-synthesized members and the accessible object members are verified against
    // Roslyn positional-record compilation in the generated-member matrix specifications.
    static readonly string[] _recordMembers = ["EqualityContract", "ToString", "Equals", "GetHashCode", "Deconstruct", "PrintMembers", "GetType", "MemberwiseClone", "ReferenceEquals", "Clone"];

    // Only the members synthesized *on* a record conflict with the enclosing type name.
    // Inherited object members (including Clone and GetType) do not.
    static readonly HashSet<string> _recordTypeNames = new(StringComparer.Ordinal)
    {
        "EqualityContract", "ToString", "Equals", "GetHashCode", "PrintMembers"
    };

    internal static IReadOnlyList<string> RecordMembers => _recordMembers;

    internal static bool ConceptMembersAreUnique(string conceptName, IEnumerable<string> values, bool hasPublicFactory)
    {
        var names = values.Select(From).ToArray();
        if (names.Length > 0)
        {
            // Unlike record members, an enum value can have the enclosing enum's name.
            return names.Distinct(StringComparer.Ordinal).Count() == names.Length;
        }

        // ConceptAs<T> already supplies Value; the derived record does not synthesize it anew.
        // ConceptAs<T> supplies ToString, so a derived concept does not synthesize it again.
        return (From(conceptName) == "ToString" || RecordTypeNameIsSafe(conceptName)) &&
            From(conceptName) != "NotSet" && (!hasPublicFactory || From(conceptName) != "New");
    }

    internal static bool EventMembersAreUnique(string eventName, IEnumerable<string> properties) =>
        RecordMembersAreUnique(eventName, properties);

    internal static bool CommandMembersAreUnique(string commandName, IEnumerable<string> properties) =>
        RecordTypeNameIsSafe(commandName, properties.Any()) &&
        properties.Select(From).Distinct(StringComparer.OrdinalIgnoreCase).Count() == properties.Count() &&
        From(commandName) != "Handle" && From(commandName) != "GetEventSourceId";

    internal static bool ReadModelMembersAreUnique(string readModelName, IEnumerable<string> properties, IEnumerable<string> queryNames) =>
        RecordMembersAreUnique(readModelName, properties);

    // From removes all authored underscores; no authored-derived member can end in the escape suffix.
    // Case-insensitive duplicates are refused before emission, so suffixing cannot merge two inputs.
    internal static string CommandProperty(string command, string property) =>
        Escape(From(property), From(property) == From(command) || _recordMembers.Contains(From(property), StringComparer.Ordinal) || From(property) == "Handle" || From(property) == "GetEventSourceId");

    internal static string QueryMethod(string readModel, IEnumerable<string> properties, string query) =>
        Escape(From(query), From(query) == From(readModel) || From(query) == "EqualityContract" || From(query) == "Clone" || properties.Select(From).Contains(From(query), StringComparer.Ordinal));

    internal static string Escape(string name, bool collision) => collision ? name + "_" : name;

    // Response fields keep their authored spelling on the wire, so a collision is resolved on the type name instead:
    // a field, a sibling declaration or a namespace with the response record's name escapes the record, never the field.
    // The escaped name cannot collide again: From removes authored underscores, so no authored name ends in '_'.
    internal static string ResponseType(string command, IEnumerable<string> fields, Func<string, bool> occupied)
    {
        var name = $"{From(command)}Response";

        return Escape(name, occupied(name) || fields.Any(field => From(field) == name));
    }

    // A field equal to the response type name is resolved by ResponseType; every other collision cannot be renamed.
    internal static bool ResponseMembersAreUnique(IEnumerable<string> fields)
    {
        var generated = fields.Select(From).ToArray();

        return generated.Distinct(StringComparer.OrdinalIgnoreCase).Count() == generated.Length &&
            !generated.Any(name => _recordMembers.Contains(name, StringComparer.Ordinal));
    }

    // The service argument is always IReadModels; the second argument is the rendered CLR type.
    // Different argument types are genuine C# overloads, even if their authored query names normalize alike.
    internal static bool QueriesAreUnique(IEnumerable<(string Name, string Type, string Argument)> queries)
    {
        var signatures = queries.Select(query => (Name: From(query.Name), query.Type)).ToArray();
        return signatures.Distinct().Count() == signatures.Length &&
            queries.All(query => char.ToLowerInvariant(From(query.Argument)[0]) + From(query.Argument)[1..] != "readModels");
    }

    internal static bool QueryParametersAreUnique(IEnumerable<string> parameters)
    {
        var names = parameters.Select(name => char.ToLowerInvariant(From(name)[0]) + From(name)[1..]).ToArray();

        return names.Distinct(StringComparer.Ordinal).Count() == names.Length && !names.Contains("readModels", StringComparer.Ordinal);
    }

    internal static bool ProjectionTypeNameIsSafe(string name) => From(name) != "Define";

    internal static bool ConstraintTypeNameIsSafe(string name) => From(name) != "Define";

    internal static bool RecordTypeNameIsSafe(string name, bool hasProperties = true) =>
        !_recordTypeNames.Contains(From(name)) && (From(name) != "Deconstruct" || !hasProperties);

    internal static bool RecordMembersAreUnique(string typeName, IEnumerable<string> properties, IEnumerable<string>? methods = null)
    {
        var generated = properties.Select(From).ToArray();
        var reserved = _recordMembers.Concat((methods ?? []).Select(From)).ToHashSet(StringComparer.Ordinal);
        return RecordTypeNameIsSafe(typeName, generated.Length > 0) &&
            generated.Distinct(StringComparer.Ordinal).Count() == generated.Length &&

            // Positional records also bind constructor arguments by case-insensitive JSON property name.
            // C# accepts Name and NAme but System.Text.Json cannot serialize that record.
            generated.Distinct(StringComparer.OrdinalIgnoreCase).Count() == generated.Length &&
            generated.All(name => name != From(typeName) && !reserved.Contains(name));
    }

    internal static string From(string name)
    {
        var result = string.Concat(name.Split([' ', '_', '-', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        if (result.Length == 0)
        {
            return "Item";
        }

        if (char.GetUnicodeCategory(result[0]) is System.Globalization.UnicodeCategory.NonSpacingMark or
            System.Globalization.UnicodeCategory.SpacingCombiningMark or System.Globalization.UnicodeCategory.EnclosingMark)
        {
            throw new InvalidGeneratedIdentifier(name);
        }

        return char.IsDigit(result[0]) ? $"_{result}" : result;
    }
}
