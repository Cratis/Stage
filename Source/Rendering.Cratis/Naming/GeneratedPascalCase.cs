// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Shares generated CLR member spelling with admission checks compiled into the specification executor.
/// </summary>
internal static class GeneratedPascalCase
{
    static readonly string[] _recordMembers = ["EqualityContract", "ToString", "Equals", "GetHashCode", "Deconstruct", "PrintMembers"];
    static readonly string[] _commandMembers = [.. _recordMembers, "Handle", "GetEventSourceId"];

    internal static bool EventMembersAreUnique(string eventName, IEnumerable<string> properties) =>
        MembersAreUnique(eventName, properties, _recordMembers);

    internal static bool CommandMembersAreUnique(string commandName, IEnumerable<string> properties) =>
        MembersAreUnique(commandName, properties, _commandMembers);

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

    static bool MembersAreUnique(string typeName, IEnumerable<string> properties, IEnumerable<string> reserved)
    {
        var generated = properties.Select(From).ToArray();
        return generated.Distinct(StringComparer.Ordinal).Count() == generated.Length &&
            generated.All(name => name != From(typeName) && !reserved.Contains(name, StringComparer.Ordinal));
    }
}
