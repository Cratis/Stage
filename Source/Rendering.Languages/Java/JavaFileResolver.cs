// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Resolves only after complete-output structural analysis. A provisional spelling is not a binding proof.
/// </summary>
/// <param name="declarations">The complete plan's allocated declarations.</param>
public sealed class JavaFileResolver(JavaDeclarations declarations)
{
    readonly JavaDeclarations _declarations = declarations;
    readonly Dictionary<SymbolFile, (JavaRenderedFile Source, JavaFileStructure Structure)> _prepared = [];
    readonly HashSet<string> _unqualifiedTypes = new(StringComparer.Ordinal);

    /// <summary>
    /// Emits fully qualified provisional references, checks integrity, and analyzes the whole translated file once.
    /// All files in a plan must be prepared before any is resolved; local/unproven target identities are plan-wide.
    /// </summary>
    /// <param name="file">The validated file.</param>
    /// <exception cref="LanguagePackRefusal">The file's structure or declaration provenance is invalid.</exception>
    public void Prepare(SymbolFile file)
    {
        var source = new JavaRenderedFile(file, file.Segments.Select(segment => Provisional(file, segment)));
        var structure = new JavaFileStructure(source, _declarations);
        _unqualifiedTypes.UnionWith(structure.UnqualifiedTypes);
        _prepared[file] = (source, structure);
    }

    /// <summary>
    /// Substitutes proven spellings without fixpoint iteration, then checks final raw and translated integrity.
    /// </summary>
    /// <param name="file">The prepared, validated file.</param>
    /// <returns>The complete output file.</returns>
    /// <exception cref="LanguagePackRefusal">A generated reference or placeholder cannot be proven to bind.</exception>
    public ResolvedSymbolFile Resolve(SymbolFile file)
    {
        if (!_prepared.ContainsKey(file)) Prepare(file);
        var (source, structure) = _prepared[file];
        var targets = file.Segments.OfType<ReferenceSegment>().Select(reference => Find(reference.Type, reference.Owner)).ToArray();
        var scope = new JavaFileScope(file, _declarations, structure.CanShorten ? targets : [], structure.ReservedNames);
        var text = source.Finish(index => Final(file, index, structure, scope));
        JavaOutputIntegrity.Validate(text);
        var primary = file.Segments.OfType<DeclarationSegment>().FirstOrDefault(segment => segment.Type.Nested.Count == 0);

        return new(file.Id, file.Package, primary is null ? null : _declarations.Declared(file.Id, primary).Final, text);
    }

    static string AuthoredOrGap(SymbolSegment segment) => segment switch
    {
        NativeSegment native => native.Body,
        GapSegment gap => JavaPlaceholders.Render(gap),
        _ => throw new LanguagePackRefusal(JavaRefusalCodes.UnsupportedContext, "Unknown Java symbol segment."),
    };

    string Provisional(SymbolFile file, SymbolSegment segment) => segment switch
    {
        TextSegment text => text.Text,
        DeclarationSegment declaration => _declarations.Declared(file.Id, declaration).Name,
        NameSegment name => Find(name.Type, name.Owner).SimpleName,
        ReferenceSegment reference => Find(reference.Type, reference.Owner).QualifiedName,
        ImportsSegment => string.Empty,
        _ => AuthoredOrGap(segment),
    };

    string Final(SymbolFile file, int index, JavaFileStructure structure, JavaFileScope scope)
    {
        var segment = file.Segments[index];
        if (segment is ImportsSegment) return JavaImportBlock.Render(scope.Imports);
        if (segment is ReferenceSegment reference) return Reference(index, reference, structure, scope);
        if (segment is GapSegment gap && gap.Context is "type" or "statement") ProveQualified(index, "java", structure, scope);
        return Provisional(file, segment);
    }

    string Reference(int index, ReferenceSegment reference, JavaFileStructure structure, JavaFileScope scope)
    {
        var target = Find(reference.Type, reference.Owner);
        if (!structure.IsCodeReference(index) || _unqualifiedTypes.Contains(target.QualifiedName))
            throw new LanguagePackRefusal(JavaRefusalCodes.UnprovenStructure, "The generated reference has no proven emitted code-token or qualified target identity.");
        if (target.Package.Length == 0 && (!structure.CanShorten || scope.Package.Length != 0))
            throw new LanguagePackRefusal(JavaRefusalCodes.UnprovenQualifiedBinding, "A default-package type has no fully qualified fallback binding.");
        var spelling = structure.CanShorten ? scope.Spell(target, reference.LexicalOwner) : target.QualifiedName;
        if (spelling == target.QualifiedName && target.Package.Length > 0) ProveQualified(index, spelling.Split('.')[0], structure, scope);
        return spelling;
    }

    static void ProveQualified(int index, string first, JavaFileStructure structure, JavaFileScope scope)
    {
        if (!structure.ProvesPrefix(index, first) || scope.Imports.Any(type => type.SimpleName == first))
            throw new LanguagePackRefusal(JavaRefusalCodes.UnprovenQualifiedBinding, $"Cannot prove that Java package prefix '{first}' is unobscured at this generated reference or placeholder.");
    }

    TypeName Find(TypeName preferred, string owner)
    {
        var (type, ambiguous) = _declarations.Find(preferred, owner);

        return type ?? throw new LanguagePackRefusal(
            ambiguous ? JavaRefusalCodes.AmbiguousDeclaration : JavaRefusalCodes.MissingDeclaration,
            $"Cannot identify Java type '{preferred.QualifiedName}' for owner '{owner}'.");
    }
}
