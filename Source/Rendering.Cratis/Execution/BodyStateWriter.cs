// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Writes a reducer's returned state back as a portable value, refusing any type the unit did not declare.</summary>
internal static class BodyStateWriter
{
    internal static SemanticValue Write(object? value, BodyUnitEntry unit) => value switch
    {
        null => SemanticValue.Null,
        IEnumerable sequence and not string => SemanticValue.Array([.. sequence.Cast<object?>().Select(element => Write(element, unit))]),
        _ => Scalar(value, unit) ?? Structured(value, unit)
    };

    static SemanticValue? Scalar(object value, BodyUnitEntry unit) => value switch
    {
        string text => SemanticValue.Text(text),
        bool boolean => SemanticValue.Boolean(boolean),
        Enum member => SemanticValue.Text(EnumValue(member, unit)),
        _ => Formatted(value) ?? BodyStateNumbers.Write(value)
    };

    static SemanticValue? Formatted(object value) => value switch
    {
        Guid uuid => SemanticValue.Text(uuid.ToString("D", CultureInfo.InvariantCulture)),
        DateOnly date => SemanticValue.Text(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
        DateTimeOffset moment => SemanticValue.Text(moment.ToString("O", CultureInfo.InvariantCulture)),
        _ => null
    };

    static string EnumValue(Enum member, BodyUnitEntry unit) =>
        unit.EnumValues.TryGetValue($"{member.GetType().FullName}.{member}", out var authored)
            ? authored
            : throw new ArgumentException("The reducer returned an undeclared enum value.");

    static SemanticValue Structured(object value, BodyUnitEntry unit)
    {
        var type = value.GetType();
        if (ConceptValue(type) is { } concept) return Write(concept.GetValue(value), unit);
        var prefix = type.FullName + ".";
        var members = unit.StateMembers.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal) && !pair.Key[prefix.Length..].Contains('.')).ToArray();
        if (members.Length == 0) throw new ArgumentException("The reducer returned a type the unit does not declare.");
        return SemanticValue.Composite([.. members.Select(pair => new SemanticPropertyValue(
            SemanticId.Parse(pair.Value),
            Write((type.GetProperty(pair.Key[prefix.Length..]) ?? throw new ArgumentException("The reducer state lacks a declared member.")).GetValue(value), unit)))]);
    }

    // Concepts carry their scalar on the inherited ConceptAs<T>.Value, the same member the materializer constructs.
    static System.Reflection.PropertyInfo? ConceptValue(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().FullName == "Cratis.Concepts.ConceptAs`1") return current.GetProperty("Value");
        }
        return null;
    }
}
