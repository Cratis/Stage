// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Reference semantics, deliberately independent of the renderer's support ledger.</summary>
internal static class SemanticDependencySurface
{
    static readonly IReadOnlyDictionary<string, SemanticDependencyClassification> _references = SemanticDependencyReferences.Build();

    internal static SemanticDependencyClassification? Classify(string member)
    {
        if (_references.TryGetValue(member, out var classification)) return classification;
        var parts = member.Split('.');
        var type = typeof(SemanticApplication).Assembly.GetType($"Cratis.Screenplay.Semantics.{parts[0]}");
        return NonReference(type, parts[0], parts[1]);
    }

    internal static IEnumerable<PropertyInfo> Properties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0);

    static SemanticDependencyClassification? NonReference(Type? type, string name, string member)
    {
        if (type?.IsEnum == true) return new(null, "A discriminator, not an owner identity; runtime variants are checked separately.");
        if (name == "SemanticImplementationRequirement" || name == "SemanticExecutionCapability")
            return new(null, "Attachment metadata is outside ESM; typed-context references are indexed from request descriptors.");
        if (name == "SemanticEventContract" && member == "ContractId")
            return new(null, "Stable external identity declares the same event; ESM cross-owner references use its SemanticId.");
        if (member == "Id" && SemanticDependencyOwnership.DeclaresIdentity(type))
            return new(null, "Declaration/member identity is registered with its enclosing semantic owner, not a dependency.");
        return DescribeShape(type, member);
    }

    static SemanticDependencyClassification? DescribeShape(Type? type, string member)
    {
        if (member == "$type") return new(null, "An empty variant has no owner references; runtime variants are checked separately.");
        var property = type?.GetProperty(member);
        return property is not null && !CarriesReferences(property.PropertyType, [])
            ? new(null, "Scalar, literal or descriptive metadata: its declared shape contains no semantic owner identity.")
            : null;
    }

    static bool CarriesReferences(Type type, HashSet<Type> visited)
    {
        if (type == typeof(SemanticId) || type == typeof(EventContractId)) return true;
        if (!visited.Add(type)) return false;
        if (type.IsGenericType) return type.GetGenericArguments().Any(argument => CarriesReferences(argument, visited));
        return HasReferences(type, visited);
    }

    static bool IsReference(PropertyInfo property, HashSet<Type> visited) =>
        _references.ContainsKey($"{property.DeclaringType!.Name}.{property.Name}") || CarriesReferences(property.PropertyType, visited);

    static bool HasReferences(Type type, HashSet<Type> visited)
    {
        if (type.Namespace != typeof(SemanticApplication).Namespace || type.IsEnum) return false;
        if (Properties(type).Any(property => IsReference(property, visited))) return true;
        return type.IsAbstract && type.Assembly.GetExportedTypes().Any(candidate => candidate.IsSubclassOf(type) && CarriesReferences(candidate, visited));
    }
}
