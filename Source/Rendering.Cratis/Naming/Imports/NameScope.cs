// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Models how C# binds a simple name at the top level of a generated file's file-scoped namespace: hidden first by
/// type parameters and nested types, then members of the file namespace and imports declared inside it. A short name
/// must bind at that innermost level; enclosing namespaces and global usings cannot establish a safe short spelling.
/// </summary>
internal sealed class NameScope
{
    readonly FrameworkNameCatalog _catalog;
    readonly GeneratedProjects _project;
    readonly Dictionary<(string Namespace, string Name, int Arity), int> _generated = [];
    readonly Dictionary<string, HashSet<string>> _generatedNames = new(StringComparer.Ordinal);
    readonly HashSet<string> _generatedNamespaces = new(StringComparer.Ordinal);
    readonly HashSet<string> _typeNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="NameScope"/> class.
    /// </summary>
    /// <param name="request">The file being resolved.</param>
    /// <param name="catalog">The framework names in scope.</param>
    public NameScope(ImportRequest request, FrameworkNameCatalog catalog)
    {
        _catalog = catalog;
        _project = request.Project;
        FileNamespace = request.FileNamespace;
        _typeNames = request.ScopeTypeNames.ToHashSet(StringComparer.Ordinal);
        ValueNames = request.ScopeValueNames.ToHashSet(StringComparer.Ordinal);
        foreach (var type in request.GeneratedTypes)
        {
            _generated[(type.Namespace, type.Name, type.Arity)] = _generated.GetValueOrDefault((type.Namespace, type.Name, type.Arity)) + 1;
            if (!_generatedNames.TryGetValue(type.Namespace, out var names)) _generatedNames[type.Namespace] = names = new(StringComparer.Ordinal);
            names.Add(type.Name);
            _generatedNamespaces.UnionWith(CatalogNames.SelfAndContaining(type.Namespace));
        }

        _generatedNamespaces.UnionWith(CatalogNames.SelfAndContaining(FileNamespace));
        _generatedNamespaces.Remove(string.Empty);
    }

    /// <summary>
    /// Gets the namespace of the file.
    /// </summary>
    public string FileNamespace { get; }

    /// <summary>
    /// Gets the member, parameter and local names that hide a type in an expression.
    /// </summary>
    public IReadOnlySet<string> ValueNames { get; }

    /// <summary>
    /// Gets a value indicating whether the names of a namespace are fully known, from the generated inventory, the
    /// catalog or both.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns><see langword="true"/> when every type of the namespace is known.</returns>
    public bool Knows(string @namespace)
    {
        var referenced = _catalog.HasNamespace(@namespace, _project);
        return referenced ? _catalog.Records(@namespace) : _generatedNamespaces.Contains(@namespace);
    }

    /// <summary>
    /// Gets a value indicating whether a namespace encloses the file.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns><see langword="true"/> when the file is in the namespace or one nested in it.</returns>
    public bool Encloses(string @namespace) => FileNamespace == @namespace;

    /// <summary>
    /// Determines whether an in-namespace directive's first segment could bind to another type or namespace.
    /// </summary>
    /// <param name="name">The first segment of the directive.</param>
    /// <returns>Whether the directive could be shadowed.</returns>
    public bool HidesImport(string name) => CatalogNames.SelfAndContaining(FileNamespace).Any(@namespace =>
        TypeNames(@namespace).Contains(name, StringComparer.Ordinal) ||
        (@namespace.Length > 0 && IsNamespace(CatalogNames.Qualify(@namespace, name))));

    /// <summary>
    /// Gets the names of every top-level type in a namespace, generated or referenced.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The type names.</returns>
    public IEnumerable<string> TypeNames(string @namespace) =>
        (_generatedNames.TryGetValue(@namespace, out var names) ? names : []).Concat(_catalog.TypeNames(@namespace, _project));

    /// <summary>
    /// Gets the extension member names an import of a namespace brings into scope.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The extension member names.</returns>
    public IEnumerable<string> ExtensionNames(string @namespace) => _catalog.ExtensionNames(@namespace, _project);

    /// <summary>
    /// Finds what a simple name binds to at the file's top level.
    /// </summary>
    /// <param name="name">The simple name.</param>
    /// <param name="arity">The number of type arguments written with it.</param>
    /// <param name="imports">The namespaces imported inside the file-scoped namespace.</param>
    /// <param name="target">The type the name is meant to bind to.</param>
    /// <returns>What the name binds to.</returns>
    public NameLookup Find(string name, int arity, IReadOnlyCollection<string> imports, TypeReference target)
    {
        if (_typeNames.Contains(name)) return NameLookup.Other;
        if (arity == 0 && IsNamespace(CatalogNames.Qualify(FileNamespace, name))) return NameLookup.Other;
        if (!Knows(FileNamespace) && FileNamespace.Length > 0) return NameLookup.Unknown;
        var found = Count(FileNamespace, name, arity);
        if (found > 0) return found == 1 && Is(target, FileNamespace, name, arity) ? NameLookup.Target : NameLookup.Other;

        return Imported(name, arity, imports, target);
    }

    static bool Is(TypeReference target, string @namespace, string name, int arity) =>
        target.Namespace == @namespace && target.Arity == arity && target.Name == name;

    NameLookup Imported(string name, int arity, IReadOnlyCollection<string> imports, TypeReference target)
    {
        if (imports.Any(@namespace => !Knows(@namespace))) return NameLookup.Unknown;
        var found = imports.Select(@namespace => (Namespace: @namespace, Count: Count(@namespace, name, arity))).Where(entry => entry.Count > 0).ToArray();
        return found switch
        {
            [] => NameLookup.Nothing,
            [{ Count: 1 } single] when Is(target, single.Namespace, name, arity) => NameLookup.Target,
            _ => NameLookup.Other
        };
    }

    bool IsNamespace(string @namespace) => _generatedNamespaces.Contains(@namespace) || _catalog.HasNamespace(@namespace, _project);

    int Count(string @namespace, string name, int arity) =>
        _generated.GetValueOrDefault((@namespace, name, arity)) + _catalog.TypesNamed(@namespace, name, arity, _project).Count();
}
