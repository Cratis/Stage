// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// Resolves the inline and file implementation bodies of a compiled Screenplay source set.
/// </summary>
/// <remarks>
/// <see cref="SemanticModelLoader"/> uses this for the source sets it loads itself. A host that compiles its own
/// <see cref="SemanticDocumentSet"/> calls it directly, so that it resolves bodies exactly as the loader does
/// without recompiling or changing document identities.
/// </remarks>
public static class SemanticImplementationBodies
{
    /// <summary>
    /// Resolves the bodies whose file contents or unambiguous inline source are available.
    /// </summary>
    /// <param name="documents">The exact source documents and attachment contents the requirements were compiled from.</param>
    /// <param name="requirements">The implementation requirements emitted by compiling <paramref name="documents"/>.</param>
    /// <returns>
    /// The bodies keyed by requirement identity. A requirement whose file is not among the attachment contents, or
    /// whose inline body cannot be matched to exactly one block in its source, has no entry. Attachment diagnostics
    /// are not part of the result: they belong to whoever loaded the attachment files, as
    /// <see cref="LoadedSemanticModel.AttachmentDiagnostics"/> does for the loader.
    /// </returns>
    public static ImmutableDictionary<string, string> Resolve(
        SemanticDocumentSet documents,
        ImmutableArray<SemanticImplementationRequirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var inlineBodies = new Dictionary<(DocumentId Document, int Line, int Column), List<string>>();
        foreach (var document in documents.Documents)
        {
            if (new ScreenplayCompiler().Parse(document.Text, document.DisplayPath).Value is not { } syntax)
            {
                continue;
            }

            var collector = new InlineBodies();
            collector.VisitApplication(syntax);
            foreach (var block in collector.Bodies)
            {
                var key = (document.Id, block.Location.Line, block.Location.Column);
                if (!inlineBodies.TryGetValue(key, out var matches))
                {
                    inlineBodies[key] = matches = [];
                }

                matches.Add(block.Code);
            }
        }

        var bodies = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var requirement in requirements.IsDefault ? [] : requirements)
        {
            if (requirement.File is { } file)
            {
                if (AttachmentFiles.TryNormalize(file, out var key, out _) && documents.AttachmentContents.TryGetValue(key, out var content))
                {
                    bodies[requirement.RequirementId] = content;
                }

                continue;
            }

            if (inlineBodies.TryGetValue(
                (requirement.Source.Span.Document, requirement.Source.Span.StartLine, requirement.Source.Span.StartColumn),
                out var matches) && matches.Count == 1)
            {
                bodies[requirement.RequirementId] = matches[0];
            }
        }

        return bodies.ToImmutable();
    }

    sealed class InlineBodies : ScreenplaySyntaxWalker
    {
        public List<CodeBlockSyntax> Bodies { get; } = [];

        public override void VisitCodeBlock(CodeBlockSyntax syntax)
        {
            Bodies.Add(syntax);
            base.VisitCodeBlock(syntax);
        }
    }
}
