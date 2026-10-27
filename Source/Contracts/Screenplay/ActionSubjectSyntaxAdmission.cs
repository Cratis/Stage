// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Refuses explicit action subjects before a syntax path can discard their selection semantics.
/// </summary>
public sealed class ActionSubjectSyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        var construct = node switch
        {
            ToolbarItemSyntax { Subject: not null } item => $"Toolbar item '{item.Name}'",
            ScreenActionSyntax { Subject: not null } action => $"Screen action '{action.Command}'",
            _ => null
        };
        if (construct is not null)
        {
            throw new InvalidEventModel(
                node.Location.Path ?? "<source>",
                [$"STAGE-ESM-058: {construct} at {node.Location} declares an explicit action subject; explicit action subjects are not yet rendered (Cratis/Stage#855, Screenplay decision 0110)."]);
        }
    }
}
