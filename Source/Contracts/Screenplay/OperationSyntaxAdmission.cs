// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Refuses operation constructs before a legacy path can drop them or render them as event appends.
/// </summary>
public sealed class OperationSyntaxAdmission : ScreenplaySyntaxWalker
{
    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        var construct = Describe(node);
        if (construct is not null)
        {
            throw new InvalidEventModel(
                node.Location.Path ?? "<source>",
                [$"STAGE-ESM-057: {construct} at {node.Location} cannot be rendered yet; the legacy syntax path would drop it or render it as an event append (Cratis/Stage#176, Cratis/Screenplay#301)."]);
        }
    }

    static string? Describe(SyntaxNode node) => node switch
    {
        SystemSyntax system => $"System '{system.Name}'",
        OperationSyntax operation => $"Operation '{operation.Name}'",
        OperationPhaseSyntax => "Operation phase",
        ProducesSyntax { InlineOperation: not null } production => $"Inline operation '{production.InlineOperation.Name}'",
        _ => DescribeSpecification(node)
    };

    static string? DescribeSpecification(SyntaxNode node) => node switch
    {
        SpecificationOperationFailureSyntax failure => $"Operation failure fixture '{failure.Operation}'",
        SpecificationOperationSyntax assertion => $"Operation assertion '{assertion.Operation}'",
        SpecificationCompensatedSyntax compensation => $"Compensation assertion '{compensation.Operation}'",
        _ => null
    };
}
