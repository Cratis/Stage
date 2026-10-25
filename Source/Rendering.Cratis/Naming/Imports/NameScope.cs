// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Models how C# binds a simple name in a generated file with one file-scoped namespace: hidden first by type parameters
/// and nested types, then members of the file namespace and the imports declared inside it, then members of every
/// enclosing namespace out to the global namespace, and last the using directives of the compilation unit together with
/// the global usings of the project. A level whose names are not all known makes the binding unknown.
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
    /// Gets a value indicating whether a namespace is the file's own namespace, whose members need no import.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns><see langword="true"/> when the file is in the namespace.</returns>
    public bool Encloses(string @namespace) => FileNamespace == @namespace;

    /// <summary>
    /// Determines whether a globally rooted directive's first segment is hidden by a global type.
    /// </summary>
    /// <param name="name">The first segment of the directive.</param>
    /// <returns>Whether the directive could be shadowed.</returns>
    public bool HidesImport(string name) => TypeNames(string.Empty).Contains(name, StringComparer.Ordinal);

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
    /// Finds what a simple name binds to.
    /// </summary>
    /// <param name="name">The simple name.</param>
    /// <param name="arity">The number of type arguments written with it.</param>
    /// <param name="imports">The namespaces imported inside the file-scoped namespace.</param>
    /// <param name="compilationUnit">The namespaces the compilation unit imports, including the global usings, or <see langword="null"/> when they are not known.</param>
    /// <param name="target">The type the name is meant to bind to.</param>
    /// <returns>What the name binds to.</returns>
    public NameLookup Find(string name, int arity, IReadOnlyCollection<string> imports, IReadOnlyCollection<string>? compilationUnit, TypeReference target)
    {
        if (_typeNames.Contains(name)) return NameLookup.Other;
        var member = Member(FileNamespace, name, arity, target);
        if (member != NameLookup.Nothing) return member;

        var imported = Imported(name, arity, imports, target);
        if (imported != NameLookup.Nothing) return imported;
        foreach (var @namespace in CatalogNames.SelfAndContaining(FileNamespace).Skip(1))
        {
            member = Member(@namespace, name, arity, target);
            if (member != NameLookup.Nothing) return member;
        }

        return compilationUnit is null ? NameLookup.Unknown : Imported(name, arity, compilationUnit, target);
    }

    /// <summary>
    /// Gets a value indicating whether a simple name binds only to framework types that are not attributes, so that an
    /// attribute written with that name still binds to the type with the <c language="csharp">Attribute</c> suffix.
    /// </summary>
    /// <param name="name">The simple name.</param>
    /// <param name="arity">The number of type arguments written with it.</param>
    /// <param name="imports">The namespaces imported inside the file-scoped namespace.</param>
    /// <param name="compilationUnit">The namespaces the compilation unit imports, or <see langword="null"/> when they are not known.</param>
    /// <returns><see langword="true"/> when every type the name binds to is known not to be an attribute.</returns>
    public bool BindsOnlyNonAttributes(string name, int arity, IReadOnlyCollection<string> imports, IReadOnlyCollection<string>? compilationUnit)
    {
        if (_typeNames.Contains(name)) return false;
        IEnumerable<(IReadOnlyCollection<string> Namespaces, bool Imported)> levels =
        [
            ([FileNamespace], false),
            (imports, true),
            .. CatalogNames.SelfAndContaining(FileNamespace).Skip(1).Select(@namespace => ((IReadOnlyCollection<string>)[@namespace], false)),
            (compilationUnit ?? [], true)
        ];
        foreach (var (namespaces, imported) in levels)
        {
            if (!imported && arity == 0 && namespaces.Any(@namespace => IsNamespace(CatalogNames.Qualify(@namespace, name)))) return false;
            if (namespaces.Any(@namespace => @namespace.Length > 0 && !Knows(@namespace))) return false;
            if (namespaces.Any(@namespace => _generated.GetValueOrDefault((@namespace, name, arity)) > 0)) return false;
            var types = namespaces.SelectMany(@namespace => _catalog.TypesNamed(@namespace, name, arity, _project)).ToArray();
            if (types.Length > 0) return types.All(type => type.Kind != "attribute");
        }

        return false;
    }

    static bool Is(TypeReference target, string @namespace, string name, int arity) =>
        target.Namespace == @namespace && target.Arity == arity && target.Name == name;

    NameLookup Member(string @namespace, string name, int arity, TypeReference target)
    {
        if (arity == 0 && IsNamespace(CatalogNames.Qualify(@namespace, name))) return NameLookup.Other;
        if (@namespace.Length > 0 && !Knows(@namespace)) return NameLookup.Unknown;
        var found = Count(@namespace, name, arity);
        if (found == 0) return NameLookup.Nothing;
        return found == 1 && Is(target, @namespace, name, arity) ? NameLookup.Target : NameLookup.Other;
    }

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
