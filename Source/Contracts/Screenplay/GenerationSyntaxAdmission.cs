// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Refuses generation strategies before a legacy path can replace them with UUIDs.
/// </summary>
public sealed class GenerationSyntaxAdmission : ScreenplaySyntaxWalker
{
    HashSet<string> _ruleBearingConcepts = [];

    /// <inheritdoc/>
    public override void VisitApplication(ApplicationSyntax syntax)
    {
        _ruleBearingConcepts = syntax.Concepts.Where(concept => concept.Validations?.Any() == true).Select(concept => concept.Name).ToHashSet(StringComparer.Ordinal);
        base.VisitApplication(syntax);
    }

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (node is ConceptSyntax { Generation: not null } ||
            (node is PropertySyntax { IsGenerated: true } property && _ruleBearingConcepts.Contains(property.Type.Name)))
        {
            throw new UnsupportedGenerationStrategy(node.Location);
        }
    }
}
