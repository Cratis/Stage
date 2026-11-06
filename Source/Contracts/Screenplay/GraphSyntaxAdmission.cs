// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Refuses graph constructs before a legacy path can drop them. Graphs render from the executable semantic model only.
/// </summary>
public sealed class GraphSyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <summary>
    /// The code for a graph construct reaching a path that renders from syntax.
    /// </summary>
    public const string Code = "STAGE-ESM-067";

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        var construct = Describe(node);
        if (construct is not null)
        {
            throw new InvalidEventModel(
                node.Location.Path ?? "<source>",
                [$"{Code}: {construct} at {node.Location} cannot be rendered by the legacy syntax path, which would drop it; graphs render from the executable semantic model (Cratis/Stage#976)."]);
        }
    }

    static string? Describe(SyntaxNode node) => node switch
    {
        GraphSyntax graph => $"Graph '{graph.Name}'",
        GraphProjectionSyntax projection => $"Graph projection '{projection.Name}'",
        SpecificationGraphSyntax step => $"Graph specification step '{step.Graph}'",
        _ => null
    };
}
