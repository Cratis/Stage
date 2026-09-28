// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    internal static bool ConceptMembersAreUnique(string conceptName, IEnumerable<string> values, bool isIdentifier)
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
            From(conceptName) != "NotSet" && (!isIdentifier || From(conceptName) != "New");
    }

    internal static bool EventMembersAreUnique(string eventName, IEnumerable<string> properties) =>
        RecordMembersAreUnique(eventName, properties);

    internal static bool CommandMembersAreUnique(string commandName, IEnumerable<string> properties) =>
        RecordMembersAreUnique(commandName, properties, ["Handle", "GetEventSourceId"]) &&
        From(commandName) is not ("Handle" or "GetEventSourceId");

    internal static bool ReadModelMembersAreUnique(string readModelName, IEnumerable<string> properties, IEnumerable<string> queryNames) =>
        RecordMembersAreUnique(readModelName, properties, queryNames) &&
        queryNames.All(name => From(name) != From(readModelName) && From(name) is not ("EqualityContract" or "Clone"));

    // The service argument is always IReadModels; the second argument is the rendered CLR type.
    // Different argument types are genuine C# overloads, even if their authored query names normalize alike.
    internal static bool QueriesAreUnique(IEnumerable<(string Name, string Type, string Argument)> queries)
    {
        var signatures = queries.Select(query => (Name: From(query.Name), query.Type)).ToArray();
        return signatures.Distinct().Count() == signatures.Length &&
            queries.All(query => char.ToLowerInvariant(From(query.Argument)[0]) + From(query.Argument)[1..] != "readModels");
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

        return char.IsDigit(result[0]) ? $"_{result}" : result;
    }
}
