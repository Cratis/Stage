// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// The exception that is thrown when an expression has no C# rendering. Rendering it as a null literal would compile
/// and mean something else entirely, so the slice fails loudly instead.
/// </summary>
/// <param name="expression">The <see cref="ExpressionSyntax"/> that could not be rendered.</param>
public class UnsupportedExpression(ExpressionSyntax expression)
    : Exception($"Expression of type '{expression.GetType().Name}' at line {expression.Location.Line} has no C# rendering.");
