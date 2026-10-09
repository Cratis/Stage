// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Stage.Rendering.Cratis;

internal sealed class SyntaxRouteAdmission : ScreenplaySyntaxWalker
{
    public override void VisitNode(SyntaxNode node)
    {
        var routed = node switch
        {
            EventSourceSyntax source => source.Streams.Any(),
            CommandSyntax command => command.Stream is not null,
            SpecificationExampleSyntax example => example.Stream is not null || example.NoStream is not null,
            SpecificationRedeliverySyntax redelivery => redelivery.Stream is not null || redelivery.NoStream is not null,
            SpecificationEventSyntax occurrence => occurrence.Stream is not null || occurrence.NoStream is not null,
            _ => false
        };
        if (routed)
        {
            throw new UnsupportedEventRoutes(node.Location);
        }
    }
}
