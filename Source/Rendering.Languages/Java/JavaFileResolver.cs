// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Substitutes a validated symbol file using the plan's shared declaration allocation.
/// </summary>
/// <param name="declarations">The complete plan's allocated declarations.</param>
public sealed class JavaFileResolver(JavaDeclarations declarations)
{
    readonly JavaDeclarations _declarations = declarations;

    /// <summary>
    /// Resolves every segment and authoritatively checks the final text before returning it.
    /// </summary>
    /// <param name="file">The validated file.</param>
    /// <returns>The complete output file.</returns>
    /// <exception cref="LanguagePackRefusal">A target is absent or ambiguous, cannot bind, or the output fails integrity checks.</exception>
    public ResolvedSymbolFile Resolve(SymbolFile file)
    {
        var references = file.Segments.OfType<ReferenceSegment>().Select(reference => Find(reference.Type, reference.Owner)).ToArray();
        var scope = new JavaFileScope(file, _declarations, references);
        var text = string.Concat(file.Segments.Select(segment => Substitute(file, segment, scope)));
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

    string Substitute(SymbolFile file, SymbolSegment segment, JavaFileScope scope) => segment switch
    {
        TextSegment text => text.Text.ReplaceLineEndings("\n"),
        DeclarationSegment declaration => _declarations.Declared(file.Id, declaration).Name,
        NameSegment name => Find(name.Type, name.Owner).SimpleName,
        ReferenceSegment reference => scope.Spell(Find(reference.Type, reference.Owner), reference.LexicalOwner),
        ImportsSegment => JavaImportBlock.Render(scope.Imports),
        _ => AuthoredOrGap(segment),
    };

    TypeName Find(TypeName preferred, string owner)
    {
        var (type, ambiguous) = _declarations.Find(preferred, owner);

        return type ?? throw new LanguagePackRefusal(
            ambiguous ? JavaRefusalCodes.AmbiguousDeclaration : JavaRefusalCodes.MissingDeclaration,
            $"Cannot identify Java type '{preferred.QualifiedName}' for owner '{owner}'.");
    }
}
