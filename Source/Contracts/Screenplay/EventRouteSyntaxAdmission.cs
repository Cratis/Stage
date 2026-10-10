// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Refuses event-source declarations and routes before translation to the legacy event model.
/// </summary>
public sealed class EventRouteSyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <summary>
    /// Determines whether a syntax node declares or selects an event-source route.
    /// </summary>
    /// <param name="node">The syntax node to inspect.</param>
    /// <returns>Whether the node requires routed event execution.</returns>
    public static bool IsRouted(SyntaxNode node) => node switch
    {
        EventSourceSyntax or EventStreamSyntax or CommandStreamSyntax or SpecificationStreamSyntax or SpecificationNoStreamSyntax => true,
        CommandSyntax command => command.Stream is not null || command.StreamCandidates.Any(),
        SpecificationExampleSyntax example => example.Stream is not null || example.NoStream is not null,
        SpecificationRedeliverySyntax redelivery => redelivery.Stream is not null || redelivery.NoStream is not null,
        SpecificationEventSyntax occurrence => occurrence.Stream is not null || occurrence.NoStream is not null,
        _ => false
    };

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (IsRouted(node))
        {
            throw new UnsupportedEventSourceRoutes(node.Location);
        }
    }
}
