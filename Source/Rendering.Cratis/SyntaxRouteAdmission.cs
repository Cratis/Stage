// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Rendering.Cratis;

internal sealed class SyntaxRouteAdmission : ScreenplaySyntaxWalker
{
    public override void VisitNode(SyntaxNode node)
    {
        if (EventRouteSyntaxAdmission.IsRouted(node))
        {
            throw new UnsupportedEventRoutes(node.Location);
        }
    }
}
