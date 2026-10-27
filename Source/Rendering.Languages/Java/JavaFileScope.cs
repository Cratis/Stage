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
    /// <param name="reservedNames">Identifiers from authoritative complete-output analysis, when available.</param>
    /// <exception cref="LanguagePackRefusal">A native body cannot be scanned safely.</exception>
    public JavaFileScope(SymbolFile file, JavaDeclarations declarations, IEnumerable<TypeName> references, IReadOnlySet<string>? reservedNames = null)
    {
        _file = file;
        _declarations = declarations;
        _references = [.. references.DistinctBy(type => type.QualifiedName, StringComparer.Ordinal)];
        _nativeNames = (reservedNames ?? InlineBodyScanner.Identifiers(file)).ToHashSet(StringComparer.Ordinal);
        _declaredNames = declarations.InFile(file.Id).Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        _memberNames = declarations.InFile(file.Id).Where(type => type.Scope.Length > 0).Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var type in _references.Where(NeedsTopLevelImport))
        {
            _imports.TryAdd(type.QualifiedName, type);
        }

        ReserveObscuredImports();
    }

    /// <summary>Gets the file's declared package.</summary>
    public string Package => _file.Package;

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
        if (CanUseShortName(type, lexicalOwner)) return type.SimpleName;
        if (!IsObscured(type, lexicalOwner)) return type.QualifiedName;
        if (CanUseEnclosingType(type, lexicalOwner))
        {
            return string.Join('.', new[] { type.Name }.Concat(type.Nested));
        }

        throw Obscured(type);
    }

    static LanguagePackRefusal Obscured(TypeName type) => new(JavaRefusalCodes.ObscuredReference, $"Cannot prove a Java spelling for '{type.QualifiedName}' without obscuring its binding.");

    bool CanUseShortName(TypeName type, string lexicalOwner) => !_declarations.Inheritance.BlocksShortening(_file.Id, lexicalOwner, type) &&
        (CanUseSimple(type, lexicalOwner) || _imports.ContainsKey(type.QualifiedName));

    bool CanUseSimple(TypeName type, string lexicalOwner)
    {
        if (_nativeNames.Contains(type.SimpleName)) return false;
        if (type.Nested.Count > 0) return _declarations.CanShortenMember(_file.Id, lexicalOwner, type);
        if (_memberNames.Contains(type.Name)) return false;
        if (type.Package == _file.Package) return true;
        if (_declarations.PackageDeclares(_file.Package, type.Name)) return false;

        return type.Package == "java.lang" && JavaLangTypes.Names.Contains(type.Name);
    }

    bool CanUseEnclosingType(TypeName type, string lexicalOwner) => type.Nested.Count > 0 && CanUseShortName(type.TopLevel, lexicalOwner);

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

    // Reserve candidates for proven owners up front. Spell still checks the actual reference owner;
    // the existence of this import never proves an omitted owner's binding.
    bool CanImportUnshadowed(TypeName type) => CanImport(type) && _declarations.InFile(_file.Id).Any(owner =>
        !_declarations.Inheritance.BlocksShortening(_file.Id, owner.Owner, type));

    bool IsObscured(TypeName type) => IsObscured(type, string.Empty) ||
        _declarations.InFile(_file.Id).Any(owner => IsObscured(type, owner.Owner));

    bool IsObscured(TypeName type, string lexicalOwner)
    {
        var first = type.QualifiedName.Split('.')[0];

        return _nativeNames.Contains(first) || _declaredNames.Contains(first) ||
            _declarations.PackageDeclares(_file.Package, first) || JavaLangTypes.Names.Contains(first) ||
            _imports.Values.Any(import => import.SimpleName == first) || _declarations.Inheritance.ObscuresName(_file.Id, lexicalOwner, first);
    }

    void ReserveObscuredImports()
    {
        var added = true;
        while (added)
        {
            added = false;
            foreach (var type in _references.Where(IsObscured))
            {
                if (CanImportUnshadowed(type)) added |= _imports.TryAdd(type.QualifiedName, type);
                else if (type.Nested.Count > 0 && CanImportUnshadowed(type.TopLevel)) added |= _imports.TryAdd(type.TopLevel.QualifiedName, type.TopLevel);
            }
        }
    }
}
