// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Chooses spellings after all allocated reference targets and native bodies in a file are known.
/// Reservations are deliberately file-wide: a sibling's member type never proves a lexical binding.
/// </summary>
public sealed class JavaFileScope
{
    readonly SymbolFile _file;
    readonly JavaDeclarations _declarations;
    readonly TypeName[] _references;
    readonly HashSet<string> _nativeNames;
    readonly HashSet<string> _declaredNames;
    readonly HashSet<string> _memberNames;
    readonly Dictionary<string, TypeName> _imports = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaFileScope"/> class.
    /// </summary>
    /// <param name="file">The structured file, including its native bodies.</param>
    /// <param name="declarations">The plan's allocated declarations.</param>
    /// <param name="references">Every allocated type referenced by the file, before any shortening.</param>
    /// <exception cref="LanguagePackRefusal">A native body cannot be scanned safely.</exception>
    public JavaFileScope(SymbolFile file, JavaDeclarations declarations, IEnumerable<TypeName> references)
    {
        _file = file;
        _declarations = declarations;
        _references = [.. references.DistinctBy(type => type.QualifiedName, StringComparer.Ordinal)];
        _nativeNames = file.Segments.OfType<NativeSegment>().SelectMany(segment => InlineBodyScanner.Identifiers(segment.Body)).ToHashSet(StringComparer.Ordinal);
        _declaredNames = declarations.InFile(file.Id).Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        _memberNames = declarations.InFile(file.Id).Where(type => type.Scope.Length > 0).Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var type in _references.Where(NeedsTopLevelImport))
        {
            _imports.TryAdd(type.QualifiedName, type);
        }

        ReserveObscuredImports();
    }

    /// <summary>
    /// Gets the complete, ordinally sorted single-type imports.
    /// </summary>
    public IReadOnlyList<TypeName> Imports => [.. _imports.Values.OrderBy(type => type.QualifiedName, StringComparer.Ordinal)];

    /// <summary>
    /// Chooses a spelling that provably binds, refusing when neither shortening nor qualification is safe.
    /// </summary>
    /// <param name="type">An allocated reference target supplied when constructing this scope.</param>
    /// <param name="lexicalOwner">The proven enclosing declaration owner, if supplied.</param>
    /// <returns>The simple or qualified Java source spelling.</returns>
    /// <exception cref="LanguagePackRefusal">The unnamed package is inaccessible or every spelling is obscured.</exception>
    public string Spell(TypeName type, string lexicalOwner = "")
    {
        if (type.Package.Length == 0 && _file.Package.Length > 0) throw Obscured(type);
        if (CanUseSimple(type, lexicalOwner) || _imports.ContainsKey(type.QualifiedName)) return type.SimpleName;
        if (!IsObscured(type)) return type.QualifiedName;
        if (CanUseEnclosingType(type))
        {
            return string.Join('.', new[] { type.Name }.Concat(type.Nested));
        }

        throw Obscured(type);
    }

    static LanguagePackRefusal Obscured(TypeName type) => new(JavaRefusalCodes.ObscuredReference, $"Cannot prove a Java spelling for '{type.QualifiedName}' without obscuring its binding.");

    bool CanUseSimple(TypeName type, string lexicalOwner)
    {
        if (_nativeNames.Contains(type.SimpleName)) return false;
        if (type.Nested.Count > 0) return _declarations.CanShortenMember(_file.Id, lexicalOwner, type);
        if (_memberNames.Contains(type.Name)) return false;
        if (type.Package == _file.Package) return true;
        if (_declarations.PackageDeclares(_file.Package, type.Name)) return false;

        return type.Package == "java.lang" && JavaLangTypes.Names.Contains(type.Name);
    }

    bool CanUseEnclosingType(TypeName type) => type.Nested.Count > 0 &&
        (CanUseSimple(type.TopLevel, string.Empty) || _imports.ContainsKey(type.TopLevel.QualifiedName));

    bool NeedsTopLevelImport(TypeName type) => type.Nested.Count == 0 && type.Package != _file.Package && type.Package != "java.lang" && CanImport(type);

    bool CanImport(TypeName type)
    {
        if (type.Package.Length == 0) return false;
        if (_nativeNames.Contains(type.SimpleName) || _declaredNames.Contains(type.SimpleName)) return false;
        if (_declarations.PackageDeclares(_file.Package, type.SimpleName) || JavaLangTypes.Names.Contains(type.SimpleName)) return false;

        // Include possible enclosing-type imports up front, so fallback imports cannot introduce
        // an order-dependent winner when two different types share a simple name.
        return !_references.Concat(_references.Select(reference => reference.TopLevel)).Any(reference =>
            reference.SimpleName == type.SimpleName && reference.QualifiedName != type.QualifiedName);
    }

    bool IsObscured(TypeName type)
    {
        var first = type.QualifiedName.Split('.')[0];

        return _nativeNames.Contains(first) || _declaredNames.Contains(first) ||
            _declarations.PackageDeclares(_file.Package, first) || JavaLangTypes.Names.Contains(first) ||
            _imports.Values.Any(import => import.SimpleName == first);
    }

    void ReserveObscuredImports()
    {
        var added = true;
        while (added)
        {
            added = false;
            foreach (var type in _references.Where(IsObscured))
            {
                if (CanImport(type)) added |= _imports.TryAdd(type.QualifiedName, type);
                else if (type.Nested.Count > 0 && CanImport(type.TopLevel)) added |= _imports.TryAdd(type.TopLevel.QualifiedName, type.TopLevel);
            }
        }
    }
}
