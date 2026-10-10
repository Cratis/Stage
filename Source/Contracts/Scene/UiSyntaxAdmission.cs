// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Refuses the UI members the Scene converters do not translate. Shared by render planning and Host scene loading.
/// </summary>
/// <remarks>
/// Package components, toolbars, UI bindings, template metadata and outlets on layouts and screen templates are
/// translated by the converters, and so is the typed composition ABI of Screenplay 4.120: exposures, instance values,
/// screen contributions, template slot content and picker metadata, grid/grow/span arrangements, and a
/// <c>navigate</c> directive's route, outlet and parameters (as a Scene <c>DestinationReference</c>). Dialog template
/// outlets have no Scene counterpart - a Scene dialog template declares none - so they are refused rather than dropped.
/// </remarks>
public sealed class UiSyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        var member = node switch
        {
            DialogTemplateSyntax dialog when dialog.Outlets.Any() => "DialogTemplateSyntax.Outlets",
            _ => null
        };
        if (member is not null)
        {
            throw new UnsupportedUiSyntax(member, node.Location);
        }
    }
}
