// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Refuses public-event constructs before a legacy path can discard their semantics.
/// </summary>
public sealed class PublicEventSyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        switch (node)
        {
            case EventSyntax { Origin: not null }:
            case CaptureSourceSyntax source when CaptureEventsSource.IsEvents(source):
                throw new UnsupportedForeignEvents(node.Location);
            case EventSyntax { Visibility: EventVisibility.Public }:
                throw new UnsupportedPublicEvents(node.Location);
            case SliceSyntax { Direction: not null } slice:
                throw new UnsupportedTranslationDirection(node.Location, slice.Direction.ToString()!);
        }
    }
}
