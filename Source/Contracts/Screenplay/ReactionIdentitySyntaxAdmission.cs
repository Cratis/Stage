// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Refuses reaction identity and observer filters before legacy paths can discard them.
/// </summary>
public sealed class ReactionIdentitySyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (node is ReactionIdentitySyntax || node is ReactionSyntax { RunsAs: not null })
        {
            throw new UnsupportedReactionIdentity(node is ReactionSyntax reaction ? reaction.RunsAs!.Location : node.Location);
        }

        if (node is ObserverFilterSyntax)
        {
            throw new UnsupportedObserverFilters(node.Location);
        }
    }
}
