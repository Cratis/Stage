// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Answers which framework names are in scope in a generated project. The renderer has no Cratis assemblies at run
/// time, so it reads a checked-in catalog generated from the compile assets the scaffold restores: the namespaces, the
/// public types of every recorded namespace, the extension members a namespace import brings in, the members a base
/// class contributes to a derived type and the global usings of each project.
/// </summary>
internal sealed class FrameworkNameCatalog
{
    /// <summary>
    /// The manifest resource name of the checked-in catalog.
    /// </summary>
    public const string ResourceName = "Cratis.Stage.Rendering.Cratis.Naming.Imports.FrameworkNames.catalog";

    static readonly Lazy<FrameworkNameCatalog> _current = new(() => new(FrameworkNameCatalogText.Read(ReadResource())));

    readonly Dictionary<string, GeneratedProjects> _namespaces = new(StringComparer.Ordinal);
    readonly Dictionary<(string Namespace, string Name, int Arity), List<CatalogedType>> _types = [];
    readonly Dictionary<string, List<CatalogedType>> _typesByNamespace = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<CatalogExtensions>> _extensions = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<CatalogBaseType>> _baseTypes = new(StringComparer.Ordinal);
    readonly HashSet<string> _importedNamespaces = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkNameCatalog"/> class.
    /// </summary>
    /// <param name="content">The catalog content.</param>
    public FrameworkNameCatalog(FrameworkNameCatalogContent content)
    {
        Content = content;
        foreach (var @namespace in content.Namespaces)
        {
            _namespaces[@namespace.Name] = _namespaces.GetValueOrDefault(@namespace.Name) | @namespace.Projects;
        }

        foreach (var type in content.Types.Where(type => type.IsTopLevel))
        {
            Add(_types, (type.Namespace, type.Name, type.Arity), type);
            Add(_typesByNamespace, type.Namespace, type);
        }

        foreach (var extensions in content.Extensions) Add(_extensions, extensions.Namespace, extensions);
        foreach (var type in content.BaseTypes) Add(_baseTypes, type.FullName, type);
        _importedNamespaces.UnionWith(content.Usings.Where(@using => @using.Kind == CatalogUsingKind.Namespace).Select(@using => @using.Target));
    }

    /// <summary>
    /// Gets the checked-in catalog.
    /// </summary>
    public static FrameworkNameCatalog Current => _current.Value;

    /// <summary>
    /// Gets the catalog content.
    /// </summary>
    public FrameworkNameCatalogContent Content { get; }

    /// <summary>
    /// Gets the namespaces every file of a project imports through implicit and package global usings.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <returns>The imported namespaces, sorted ordinally.</returns>
    public IReadOnlyList<string> GlobalUsings(GeneratedProjects project) =>
    [
        .. Content.Usings.Where(@using => @using.Kind == CatalogUsingKind.Namespace && Sees(@using.Projects, project))
            .Select(@using => @using.Target).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
    ];

    /// <summary>
    /// Gets a value indicating whether a project has a static or alias global using, whose names the catalog does not model.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <returns><see langword="true"/> when such a directive exists.</returns>
    public bool HasUnmodeledUsings(GeneratedProjects project) =>
        Content.Usings.Any(@using => @using.Kind != CatalogUsingKind.Namespace && Sees(@using.Projects, project));

    /// <summary>
    /// Gets a value indicating whether the catalog records every type of a namespace.
    /// </summary>
    /// <param name="namespace">The namespace, empty for the global namespace.</param>
    /// <returns><see langword="true"/> when the types are recorded.</returns>
    public bool Records(string @namespace) =>
        @namespace.Length == 0 || _importedNamespaces.Contains(@namespace) ||
        Content.Scopes.Any(scope => @namespace == scope || @namespace.StartsWith($"{scope}.", StringComparison.Ordinal));

    /// <summary>
    /// Gets a value indicating whether a namespace exists in the references of a project.
    /// </summary>
    /// <param name="namespace">The full namespace name.</param>
    /// <param name="project">The project.</param>
    /// <returns><see langword="true"/> when the namespace exists.</returns>
    public bool HasNamespace(string @namespace, GeneratedProjects project) =>
        _namespaces.TryGetValue(@namespace, out var projects) && Sees(projects, project);

    /// <summary>
    /// Finds the top-level types of a namespace with a name and generic arity.
    /// </summary>
    /// <param name="namespace">The namespace, empty for the global namespace.</param>
    /// <param name="name">The type name.</param>
    /// <param name="arity">The generic arity.</param>
    /// <param name="project">The project.</param>
    /// <returns>The matching types.</returns>
    public IEnumerable<CatalogedType> TypesNamed(string @namespace, string name, int arity, GeneratedProjects project) =>
        _types.TryGetValue((@namespace, name, arity), out var types) ? types.Where(type => Sees(type.Projects, project)) : [];

    /// <summary>
    /// Gets the names of every top-level type in a namespace, whatever their arity.
    /// </summary>
    /// <param name="namespace">The namespace, empty for the global namespace.</param>
    /// <param name="project">The project.</param>
    /// <returns>The type names.</returns>
    public IEnumerable<string> TypeNames(string @namespace, GeneratedProjects project) =>
        _typesByNamespace.TryGetValue(@namespace, out var types) ? types.Where(type => Sees(type.Projects, project)).Select(type => type.Name) : [];

    /// <summary>
    /// Gets the extension member names a namespace import brings into scope.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <param name="project">The project.</param>
    /// <returns>The extension member names.</returns>
    public IEnumerable<string> ExtensionNames(string @namespace, GeneratedProjects project) =>
        _extensions.TryGetValue(@namespace, out var extensions) ? extensions.Where(entry => Sees(entry.Projects, project)).SelectMany(entry => entry.Names) : [];

    /// <summary>
    /// Gets the names a type deriving from a cataloged class inherits through its whole base chain.
    /// </summary>
    /// <param name="fullName">The full metadata name of the base class, such as <c language="csharp">Cratis.Concepts.ConceptAs`1</c>.</param>
    /// <param name="project">The project.</param>
    /// <returns>The inherited names, or <see langword="null"/> when a class in the chain is not cataloged.</returns>
    public InheritedNames? Inherited(string fullName, GeneratedProjects project)
    {
        var members = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var nested = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        for (var current = fullName; current.Length > 0;)
        {
            var type = _baseTypes.GetValueOrDefault(current)?.FirstOrDefault(entry => Sees(entry.Projects, project));
            if (type is null) return null;
            members.UnionWith(type.Members);
            nested.UnionWith(type.NestedTypes);
            current = type.BaseFullName;
        }

        return new(members.ToImmutable(), nested.ToImmutable());
    }

    static bool Sees(GeneratedProjects projects, GeneratedProjects project) => (projects & project) != GeneratedProjects.None;

    static void Add<TKey, TValue>(Dictionary<TKey, List<TValue>> index, TKey key, TValue value)
        where TKey : notnull
    {
        if (!index.TryGetValue(key, out var values)) index[key] = values = [];
        values.Add(value);
    }

    static string ReadResource()
    {
        using var stream = typeof(FrameworkNameCatalog).Assembly.GetManifestResourceStream(ResourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
