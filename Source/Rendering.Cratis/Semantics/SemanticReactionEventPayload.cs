// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticReactionEventPayload
{
    internal static ExpressionSyntax? From(ObjectCreationExpressionSyntax creation)
    {
        var overrides = creation.Initializer?.Expressions.OfType<AssignmentExpressionSyntax>()
            .Where(IsEventPayloadOverride).ToArray() ?? [];
        if (overrides.Length > 1) return null;
        return overrides.SingleOrDefault()?.Right ?? ConstructorPayload(creation.ArgumentList?.Arguments ?? []);
    }

    static bool IsEventPayloadOverride(AssignmentExpressionSyntax assignment) =>
        assignment.Left is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == "Event";

    static ExpressionSyntax? ConstructorPayload(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var named = arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "Event");
        if (named is not null) return named.Expression;
        return arguments.Count > 1 && arguments[1].NameColon is null ? arguments[1].Expression : null;
    }
}
