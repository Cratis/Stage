// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Finds populated ESM members not covered by a supplied semantic surface ledger.
/// </summary>
internal static class SemanticUnknownMembers
{
    internal static ImmutableArray<SemanticUnknownMember> Find(ExecutableSemanticModel model, IReadOnlyDictionary<string, SemanticSurfaceDisposition> ledger)
        => FindCore(model, ledger, null);

    internal static ImmutableArray<SemanticUnknownMember> Find(ExecutableSemanticModel model, IReadOnlyDictionary<string, SemanticSurfaceDisposition> ledger, SemanticUnknownMemberScope scope)
        => FindCore(model, ledger, scope);

    static ImmutableArray<SemanticUnknownMember> FindCore(ExecutableSemanticModel model, IReadOnlyDictionary<string, SemanticSurfaceDisposition> ledger, SemanticUnknownMemberScope? scope)
    {
        var walker = new Walker(ledger, scope);
        walker.Visit(model, new(model.Application.Id, "application", string.Empty));
        return [.. walker.Unknown.OrderBy(member => member.Path, StringComparer.Ordinal).ThenBy(member => member.Member, StringComparer.Ordinal)];
    }

    static SemanticId? Identity(object value) => value.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.GetValue(value) is SemanticId { IsSet: true } id ? id : null;

    static Location Identify(object value, Location location, SemanticUnknownMemberScope? scope)
    {
        var type = value.GetType();
        var identity = Identity(value);
        return identity is { } id && scope?.Owners.Contains(id) is not false
            ? location with { Owner = id, OwnerKind = type.Name.Replace("Semantic", string.Empty, StringComparison.Ordinal).ToLowerInvariant() }
            : location;
    }

    static bool IsSemantic(Type type) => type.IsVisible && type.Namespace == "Cratis.Screenplay.Semantics";

    static IEnumerable<PropertyInfo> Properties(Type type) => type
        .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(property => property.GetMethod is { IsPublic: true, IsStatic: false } && property.GetIndexParameters().Length == 0)
        .OrderBy(property => property.Name, StringComparer.Ordinal);

    static IEnumerable<PropertyInfo> PropertiesIncludingInherited(Type type)
    {
        for (var current = type; current is not null && IsSemantic(current); current = current.BaseType)
        {
            foreach (var property in Properties(current)) yield return property;
        }
    }

    static bool IsPopulated(object? value)
    {
        if (value is null) return false;
        if (value is not IEnumerable collection || value is string) return true;
        var enumerator = collection.GetEnumerator();
        try
        {
            return enumerator.MoveNext();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Carries the nearest identity and a structural path, never an authored value.
    /// </summary>
    /// <param name="Owner">The nearest declaration identity.</param>
    /// <param name="OwnerKind">The kind of that declaration.</param>
    /// <param name="Path">The structural path to the current value.</param>
    sealed record Location(SemanticId Owner, string OwnerKind, string Path)
    {
        internal Location At(string member) => this with { Path = Path.Length == 0 ? member : $"{Path}.{member}" };
    }

    /// <summary>
    /// Tracks one reflection walk, guarding active references while preserving repeated occurrences.
    /// </summary>
    /// <param name="ledger">The member classifications already audited by the target.</param>
    /// <param name="scope">Optional selected owners, exclusions and gated member defaults.</param>
    sealed class Walker(IReadOnlyDictionary<string, SemanticSurfaceDisposition> ledger, SemanticUnknownMemberScope? scope)
    {
        readonly HashSet<object> _active = new(ReferenceEqualityComparer.Instance);
        readonly List<SemanticUnknownMember> _unknown = [];

        internal IEnumerable<SemanticUnknownMember> Unknown => _unknown;

        internal void Visit(object? value, Location location)
        {
            if (value is null || value is string) return;
            var type = value.GetType();
            if (!type.IsValueType && !_active.Add(value)) return;
            try
            {
                VisitValue(value, location);
            }
            catch (Exception)
            {
                // Fail closed without leaking getter or value details.
                Add($"{type.Name}.$type", location);
            }
            finally
            {
                if (!type.IsValueType) _active.Remove(value);
            }
        }

        void VisitValue(object value, Location location)
        {
            if (value is IEnumerable collection)
            {
                var index = 0;
                foreach (var item in collection) Visit(item, location with { Path = $"{location.Path}[{index++}]" });
                return;
            }
            var type = value.GetType();
            if (!IsSemantic(type)) return;
            if (type.IsEnum)
            {
                Check($"{type.Name}.{Enum.GetName(type, value) ?? "$value"}", location);
                return;
            }
            if (Identity(value) is { } id && scope?.Excluded.Contains(id) is true) return;
            VisitProperties(value, Identify(value, location, scope));
        }

        void VisitProperties(object value, Location location)
        {
            var type = value.GetType();
            if (!Properties(type).Any()) Check($"{type.Name}.$type", location);
            foreach (var property in PropertiesIncludingInherited(type))
            {
                VisitProperty(value, property, location.At(property.Name));
            }
        }

        void VisitProperty(object instance, PropertyInfo property, Location location)
        {
            var key = $"{property.DeclaringType!.Name}.{property.Name}";
            try
            {
                var value = property.GetValue(instance);
                if (!IsPopulated(value) || IsGatedDefault(key, value)) return;
                Check(key, location);
                Visit(value, location);
            }
            catch (Exception)
            {
                Add(key, location);
            }
        }

        bool IsGatedDefault(string key, object? value) => scope?.Gated.Contains(key) is true &&
            (value is null || (value.GetType().IsValueType && value.Equals(Activator.CreateInstance(value.GetType()))));

        void Check(string key, Location location)
        {
            if (!ledger.ContainsKey(key)) Add(key, location);
        }

        void Add(string key, Location location) => _unknown.Add(new(location.Owner, location.OwnerKind, key, location.Path, true));
    }
}
