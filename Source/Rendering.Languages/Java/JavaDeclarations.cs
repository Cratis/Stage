// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Allocates type names per package and enclosing declaration, independent of artifact kind or input order.
/// Parents are allocated first; children retain their parent's identity through every rename.
/// </summary>
public sealed class JavaDeclarations
{
    readonly List<DeclaredType> _all = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaDeclarations"/> class.
    /// </summary>
    /// <param name="plan">The structured declarations and external types.</param>
    /// <exception cref="LanguagePackRefusal">A declaration or enclosing identity is missing or ambiguous.</exception>
    public JavaDeclarations(SymbolPlan plan)
    {
        var requested = plan.ExternalTypes.DistinctBy(type => type.QualifiedName, StringComparer.Ordinal).Select(External)
            .Concat(plan.Files.SelectMany(file => file.Segments.OfType<DeclarationSegment>().Select(segment => Requested(file, segment)))).ToArray();
        if (requested.Distinct().Count() != requested.Length)
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.AmbiguousDeclaration, "A Java declaration identity is duplicated.");
        }

        foreach (var level in requested.GroupBy(declaration => Depth(declaration.Scope)).OrderBy(group => group.Key))
        {
            foreach (var declarations in level.Select(BindParent).GroupBy(declaration => (declaration.Package, Scope: declaration.AllocatedScope.ToUpperInvariant())))
            {
                _all.AddRange(JavaDeclarationNames.Allocate(declarations));
            }
        }
    }

    /// <summary>
    /// Enumerates the allocated declarations in a file.
    /// </summary>
    /// <param name="file">The stable file identity.</param>
    /// <returns>The file's declarations in ordinal type-name order.</returns>
    public IEnumerable<DeclaredType> InFile(string file) => _all.Where(declaration => !declaration.External && declaration.File == file)
        .OrderBy(declaration => declaration.Final.QualifiedName, StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a package declares a top-level simple name.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="simpleName">The case-sensitive allocated simple name.</param>
    /// <returns>Whether the package declares that name.</returns>
    public bool PackageDeclares(string package, string simpleName) => _all.Exists(declaration =>
        declaration.Package == package && declaration.Scope.Length == 0 && declaration.Name == simpleName);

    /// <summary>
    /// Finds a preferred type, optionally disambiguating a renamed declaration by owner.
    /// </summary>
    /// <param name="preferred">The structured preferred type.</param>
    /// <param name="owner">The target owner, or an empty string if not supplied.</param>
    /// <returns>The allocated type, or an absent or ambiguous lookup result.</returns>
    public (TypeName? Type, bool Ambiguous) Find(TypeName preferred, string owner = "")
    {
        var matches = _all.Where(declaration => declaration.Package == preferred.Package && declaration.Scope == preferred.EnclosingScope &&
            declaration.Preferred == preferred.SimpleName && (owner.Length == 0 || declaration.Owner == owner)).ToArray();

        return matches.Length == 1 ? (matches[0].Final, false) : (null, matches.Length > 1);
    }

    /// <summary>
    /// Finds the declaration represented by a segment, refusing missing declarations instead of inventing a name.
    /// </summary>
    /// <param name="file">The stable file identity.</param>
    /// <param name="segment">The declaration segment.</param>
    /// <returns>The allocated declaration.</returns>
    /// <exception cref="LanguagePackRefusal">The declaration is absent or ambiguous.</exception>
    public DeclaredType Declared(string file, DeclarationSegment segment)
    {
        var matches = InFile(file).Where(declaration => declaration.Owner == segment.Owner && declaration.Package == segment.Type.Package &&
            declaration.Scope == Scope(segment) && declaration.Preferred == segment.Type.SimpleName).ToArray();
        if (matches.Length != 1)
        {
            throw new LanguagePackRefusal(
                matches.Length == 0 ? JavaRefusalCodes.MissingDeclaration : JavaRefusalCodes.AmbiguousDeclaration,
                $"Cannot identify declaration '{segment.Owner}' in file '{file}'.");
        }

        return matches[0];
    }

    /// <summary>
    /// Determines whether a member type can bind by simple name in a proven enclosing declaration.
    /// A sibling declaration is not an enclosing scope.
    /// </summary>
    /// <param name="file">The reference's file.</param>
    /// <param name="lexicalOwner">The enclosing declaration owner.</param>
    /// <param name="target">The allocated member type.</param>
    /// <returns>Whether the lexical identity proves the member binding.</returns>
    public bool CanShortenMember(string file, string lexicalOwner, TypeName target)
    {
        if (lexicalOwner.Length == 0 || target.Nested.Count == 0) return false;
        var enclosing = InFile(file).Where(declaration => declaration.Owner == lexicalOwner).ToArray();
        if (enclosing.Length != 1) return false;

        return HasUniqueMember(enclosing[0], target);
    }

    static int Depth(string scope) => scope.Length == 0 ? 0 : scope.Count(character => character == '.') + 1;

    static string Scope(DeclarationSegment segment) => segment.Scope.Length == 0 ? segment.Type.EnclosingScope : segment.Scope;

    static DeclaredType External(TypeName type) => new(type.Package, type.EnclosingScope, type.SimpleName, type.SimpleName, "external:" + type.QualifiedName, string.Empty, true, string.Empty, type.EnclosingScope);

    static DeclaredType Requested(SymbolFile file, DeclarationSegment segment) => new(segment.Type.Package, Scope(segment), segment.Type.SimpleName, JavaIdentifiers.Sanitize(segment.Type.SimpleName), segment.Owner, file.Id, false, segment.ParentOwner, Scope(segment));

    static bool IsParentOf(DeclaredType parent, DeclaredType child) => parent.Package == child.Package &&
        (parent.Scope.Length == 0 ? parent.Preferred : parent.Scope + "." + parent.Preferred) == child.Scope &&
        (child.ParentOwner.Length > 0 ? parent.Owner == child.ParentOwner : parent.File == child.File);

    bool HasUniqueMember(DeclaredType parent, TypeName target)
    {
        var scope = parent.Final.EnclosingScope.Length == 0 ? parent.Name : parent.Final.EnclosingScope + "." + parent.Name;

        return parent.Package == target.Package && scope == target.EnclosingScope && _all.Count(declaration =>
            declaration.Package == target.Package && declaration.AllocatedScope == scope && declaration.Name == target.SimpleName) == 1;
    }

    DeclaredType BindParent(DeclaredType child)
    {
        if (child.Scope.Length == 0 || child.External) return child;
        var candidates = _all.Where(parent => IsParentOf(parent, child)).ToArray();
        if (candidates.Length != 1)
        {
            throw new LanguagePackRefusal(
                candidates.Length == 0 ? JavaRefusalCodes.MissingDeclaration : JavaRefusalCodes.AmbiguousDeclaration,
                $"Cannot prove enclosing declaration identity for '{child.Owner}' in file '{child.File}'.");
        }

        var parent = candidates[0];
        var scope = parent.AllocatedScope.Length == 0 ? parent.Name : parent.AllocatedScope + "." + parent.Name;

        return child with { AllocatedScope = scope, ParentOwner = parent.Owner };
    }
}
