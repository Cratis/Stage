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

    internal static bool EventMembersAreUnique(string eventName, IEnumerable<string> properties) =>
        RecordMembersAreUnique(eventName, properties);

    internal static bool CommandMembersAreUnique(string commandName, IEnumerable<string> properties) =>
        RecordMembersAreUnique(commandName, properties, ["Handle", "GetEventSourceId"]) &&
        From(commandName) is not ("Handle" or "GetEventSourceId");

    internal static bool ReadModelMembersAreUnique(string readModelName, IEnumerable<string> properties, IEnumerable<string> queryNames) =>
        RecordMembersAreUnique(readModelName, properties, queryNames) &&
        queryNames.All(name => From(name) != From(readModelName) && From(name) is not ("EqualityContract" or "Clone"));

    internal static bool RecordMembersAreUnique(string typeName, IEnumerable<string> properties, IEnumerable<string>? methods = null)
    {
        var generated = properties.Select(From).ToArray();
        var reserved = _recordMembers.Concat((methods ?? []).Select(From)).ToHashSet(StringComparer.Ordinal);
        return generated.Distinct(StringComparer.Ordinal).Count() == generated.Length &&
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
