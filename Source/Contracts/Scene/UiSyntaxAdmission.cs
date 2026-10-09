// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Refuses the UI members added in Screenplay 4.94–4.101 that the Scene converters neither translate nor admit through
/// the canonical screen composition corpus. Shared by render planning and Host scene loading.
/// </summary>
/// <remarks>
/// Package components, toolbars, UI bindings, template metadata and outlets on layouts and screen templates are
/// translated by the converters. Template assignments, template exposures, form columns, UI profile icons and guarded
/// interaction alternatives are part of the admitted screen composition corpus. Navigation routes and parameters on a
/// screen <c>navigate</c> directive and dialog template outlets are neither, so they are refused rather than dropped.
/// </remarks>
public sealed class UiSyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        var member = node switch
        {
            ScreenNavigateSyntax navigate when navigate.Route is not null || navigate.Parameters.Any() => "ScreenNavigateSyntax.Route/Parameters",
            DialogTemplateSyntax dialog when dialog.Outlets.Any() => "DialogTemplateSyntax.Outlets",
            _ => null
        };
        if (member is not null)
        {
            throw new UnsupportedUiSyntax(member, node.Location);
        }
    }
}
