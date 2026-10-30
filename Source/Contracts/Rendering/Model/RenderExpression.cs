// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal enum RenderTypeState
{
    Resolved,
    AbsentContextualType
}

internal abstract record RenderExpression(RenderTypeRef? Type) : RenderMember
{
    public RenderTypeState TypeState => Type is null ? RenderTypeState.AbsentContextualType : RenderTypeState.Resolved;

    internal sealed record Value(RenderLiteral Literal, RenderTypeRef? ContextualType) : RenderExpression(ContextualType);
    internal sealed record Resolved(SemanticExpressionRootKind Root, SemanticExpressionSourceKind Source, SemanticId Target, RenderTypeRef ResolvedType) : RenderExpression(ResolvedType);
    internal sealed record EventContext(SemanticEventContextValueKind ContextValue, RenderTypeRef ResolvedType) : RenderExpression(ResolvedType);
    internal sealed record Refusal(string Member, RenderTypeRef ResolvedType) : RenderExpression(ResolvedType);
    internal sealed record ReadPath(string Read, SemanticId PropertyId, RenderTypeRef ResolvedType) : RenderExpression(ResolvedType);
    internal sealed record Comparison(RenderExpression Left, SemanticComparisonOperator Operator, RenderExpression Right, RenderTypeRef ResolvedType) : RenderExpression(ResolvedType);
    internal sealed record Logical(RenderExpression Left, SemanticLogicalOperator Operator, RenderExpression Right, RenderTypeRef ResolvedType) : RenderExpression(ResolvedType);
}

internal abstract record RenderLiteral : RenderMember
{
    RenderLiteral()
    {
    }
    internal sealed record Null : RenderLiteral;
    internal sealed record Text(string Value) : RenderLiteral;
    internal sealed record Number(decimal Value) : RenderLiteral;
    internal sealed record Boolean(bool Value) : RenderLiteral;
    internal sealed record Array(RenderSequence<RenderLiteral> Values) : RenderLiteral;
    internal sealed record Composite(RenderSequence<RenderLiteralProperty> Properties) : RenderLiteral;
}

internal sealed record RenderLiteralProperty(SemanticId TargetProperty, RenderLiteral Value) : RenderMember;
