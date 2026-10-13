// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Refuses generation strategies before a legacy path can replace them with UUIDs.
/// </summary>
public sealed class GenerationSyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (node is ConceptSyntax { Generation: not null }) throw new UnsupportedGenerationStrategy(node.Location);
    }
}
