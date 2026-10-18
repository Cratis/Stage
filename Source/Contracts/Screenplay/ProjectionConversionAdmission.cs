// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Names every unsupported construct before a projection is converted as one indivisible definition.
/// </summary>
internal sealed class ProjectionConversionAdmission(ConversionDiagnostics diagnostics, string path) : ScreenplaySyntaxWalker
{
    public override void VisitNode(SyntaxNode node)
    {
        switch (node)
        {
            case ProjectionSyntax projection:
                if (projection.Sequence is not null)
                {
                    Report(ProjectionConverter.SequenceDiagnosticCode, $"projection sequence '{projection.Sequence}'", projection);
                }
                AutoMap(projection.AutoMap, projection);
                break;
            case ProjectionVariantSyntax:
                Report(ProjectionConverter.VariantDiagnosticCode, "projection variant", node);
                break;
            case ProjectionBlockSyntax block:
                Block(block);
                break;
            case MappingSyntax mapping when mapping is not (SetMappingSyntax or ClearMappingSyntax or AddMappingSyntax or SubtractMappingSyntax or IncrementMappingSyntax or DecrementMappingSyntax or CountMappingSyntax):
                Report(ProjectionConverter.MappingDiagnosticCode, $"projection mapping {mapping.GetType().Name} on '{mapping.Property}'", mapping);
                break;
            case ExpressionSyntax expression when expression is not (PathExpressionSyntax or EventContextExpressionSyntax or EventSourceIdExpressionSyntax or CausedByExpressionSyntax or LiteralExpressionSyntax or TemplateExpressionSyntax or RawExpressionSyntax):
                Report(ProjectionConverter.ExpressionDiagnosticCode, $"projection expression {expression.GetType().Name}", expression);
                break;
            case KeySyntax key when key is not (ExpressionKeySyntax or CompositeKeySyntax):
                Report(ProjectionConverter.KeyDiagnosticCode, $"projection key {key.GetType().Name}", key);
                break;
        }
    }

    void Block(ProjectionBlockSyntax block)
    {
        switch (block)
        {
            case EverySyntax every: AutoMap(every.AutoMap, every); break;
            case AllSyntax all: AutoMap(all.AutoMap, all); break;
            case ChildrenSyntax children: AutoMap(children.AutoMap, children); break;
            case NestedSyntax nested: AutoMap(nested.AutoMap, nested); break;
            case FromSyntax or JoinSyntax or RemoveWithSyntax or RemoveViaJoinSyntax or ClearWithSyntax: break;
            default: Report(ProjectionConverter.BlockDiagnosticCode, $"projection block {block.GetType().Name}", block); break;
        }
    }

    void AutoMap(AutoMapMode mode, SyntaxNode node)
    {
        if (mode is not (AutoMapMode.Inherit or AutoMapMode.Enabled or AutoMapMode.Disabled))
        {
            Report(ProjectionConverter.AutoMapDiagnosticCode, $"projection automap mode {mode}", node);
        }
    }

    void Report(string code, string construct, SyntaxNode node) => diagnostics.Report(new(
        code,
        DiagnosticSeverity.Error,
        construct,
        path,
        node.Location,
        $"Projection construct '{construct}' cannot be converted without changing its meaning."));
}
