// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// Defines how the expressions that reach outside the artifact being rendered — <c>$context</c>,
/// <c>$eventContext</c>, <c>$causedBy</c> and <c>$eventSourceId</c> — are rendered where they land.
/// </summary>
/// <remarks>
/// The same Screenplay expression has different C# in different places, because the surroundings differ: a
/// reactor method receives Chronicle's <c>EventContext</c> and can read <c>Occurred</c> straight off it, while a
/// command handler receives no such thing and has to ask a collaborator. Rendering both the same way is what made
/// a command handler emit members Arc's <c>CommandContext</c> does not have, so the enclosing artifact supplies
/// the rendering rather than the expression assuming one.
/// </remarks>
public interface IExpressionContext
{
    /// <summary>
    /// Renders a <c>$context.&lt;path&gt;</c> expression.
    /// </summary>
    /// <param name="context">The expression to render.</param>
    /// <returns>The rendered C# expression text.</returns>
    string Render(ContextExpressionSyntax context);

    /// <summary>
    /// Renders a <c>$eventContext.&lt;path&gt;</c> expression.
    /// </summary>
    /// <param name="eventContext">The expression to render.</param>
    /// <returns>The rendered C# expression text.</returns>
    string Render(EventContextExpressionSyntax eventContext);

    /// <summary>
    /// Renders a <c>$causedBy</c> expression.
    /// </summary>
    /// <param name="causedBy">The expression to render.</param>
    /// <returns>The rendered C# expression text.</returns>
    string Render(CausedByExpressionSyntax causedBy);

    /// <summary>
    /// Renders a <c>$eventSourceId</c> expression.
    /// </summary>
    /// <returns>The rendered C# expression text.</returns>
    string RenderEventSourceId();
}
