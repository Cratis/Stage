// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Turns the types read from every reference assembly into the catalog's type, extension and base type records,
/// merging an assembly referenced by both projects into one record that both projects see.
/// </summary>
/// <param name="records">Decides whether the catalog records every type of a namespace.</param>
sealed class CatalogTypes(Func<string, bool> records)
{
    readonly Dictionary<string, GeneratedProjects> _namespaces = new(StringComparer.Ordinal);
    readonly Dictionary<(string Namespace, string MetadataName), (GeneratedProjects Projects, MetadataType Type)> _types = [];
    readonly Dictionary<string, string> _bases = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds the namespaces and types of one assembly.
    /// </summary>
    /// <param name="projects">The projects referencing the assembly.</param>
    /// <param name="namespaces">The namespaces it declares types in.</param>
    /// <param name="types">Its externally visible types.</param>
    public void Add(GeneratedProjects projects, IEnumerable<string> namespaces, IEnumerable<MetadataType> types)
    {
        foreach (var @namespace in namespaces) AddNamespace(projects, @namespace);
        foreach (var type in types)
        {
            _bases.TryAdd(type.FullName, type.BaseFullName);
            var key = (type.Namespace, type.MetadataName);
            _types[key] = _types.TryGetValue(key, out var existing) ? (existing.Projects | projects, existing.Type) : (projects, type);
        }
    }

    /// <summary>
    /// Adds a namespace and every namespace containing it.
    /// </summary>
    /// <param name="projects">The projects in which the namespace exists.</param>
    /// <param name="namespace">The namespace.</param>
    public void AddNamespace(GeneratedProjects projects, string @namespace)
    {
        foreach (var name in CatalogNames.SelfAndContaining(@namespace).Where(name => name.Length > 0))
        {
            _namespaces[name] = _namespaces.GetValueOrDefault(name) | projects;
        }
    }

    /// <summary>
    /// Gets the namespace records.
    /// </summary>
    /// <returns>The namespaces.</returns>
    public IEnumerable<CatalogNamespace> Namespaces() => _namespaces.Select(entry => new CatalogNamespace(entry.Value, entry.Key));

    /// <summary>
    /// Gets the type records of the recorded namespaces.
    /// </summary>
    /// <returns>The types.</returns>
    public IEnumerable<CatalogedType> Types() => Recorded().Select(entry => new CatalogedType(entry.Projects, entry.Type.Namespace, entry.Type.MetadataName, Kind(entry.Type)));

    /// <summary>
    /// Gets the extension members of each recorded namespace.
    /// </summary>
    /// <returns>The extension records.</returns>
    public IEnumerable<CatalogExtensions> Extensions() => Recorded()
        .Where(entry => entry.Type.IsStatic && entry.Type.ExtensionNames.Length > 0 && !entry.Type.MetadataName.Contains('+', StringComparison.Ordinal))
        .GroupBy(entry => (entry.Projects, entry.Type.Namespace))
        .Select(group => new CatalogExtensions(
            group.Key.Projects,
            group.Key.Namespace,
            [.. group.SelectMany(entry => entry.Type.ExtensionNames).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]));

    /// <summary>
    /// Gets the classes of the recorded namespaces a generated type may derive from.
    /// </summary>
    /// <returns>The base type records.</returns>
    public IEnumerable<CatalogBaseType> BaseTypes() => Recorded()
        .Where(entry => entry.Type.IsDerivable && (entry.Type.BaseFullName.Length > 0 || entry.Type.FullName == "System.Object"))
        .Select(entry => new CatalogBaseType(entry.Projects, entry.Type.FullName, entry.Type.BaseFullName, entry.Type.Members, entry.Type.NestedTypes));

    IEnumerable<(GeneratedProjects Projects, MetadataType Type)> Recorded() => _types.Values.Where(entry => records(entry.Type.Namespace));

    string Kind(MetadataType type) => type switch
    {
        { IsInterface: true } => "interface",
        { BaseFullName: "System.Enum" } => "enum",
        { BaseFullName: "System.ValueType" } => "struct",
        { BaseFullName: "System.MulticastDelegate" } => "delegate",
        { IsStatic: true } => "static",
        _ when DerivesFromAttribute(type.BaseFullName) => "attribute",
        _ => "class"
    };

    bool DerivesFromAttribute(string baseFullName)
    {
        for (var current = baseFullName; current.Length > 0; current = _bases.GetValueOrDefault(current, string.Empty))
        {
            if (current == "System.Attribute") return true;
        }

        return false;
    }
}
