// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// Renders the context expressions against Chronicle's <c>EventContext</c>, for the artifacts that receive one —
/// a reactor method and a projection.
/// </summary>
/// <remarks>
/// <c>EventContext</c> carries <c>Occurred</c>, <c>CausedBy</c> and <c>EventSourceId</c> under those names, so
/// PascalCasing the declared path resolves for the paths a document actually uses here.
/// </remarks>
public sealed class EventContextAccess : IExpressionContext
{
    /// <summary>
    /// Gets the shared instance — the rendering carries no state.
    /// </summary>
    public static readonly EventContextAccess Instance = new();

    /// <inheritdoc/>
    public string Render(ContextExpressionSyntax context) => Path(context.Path);

    /// <inheritdoc/>
    public string Render(EventContextExpressionSyntax eventContext) => Path(eventContext.Path);

    /// <inheritdoc/>
    public string Render(CausedByExpressionSyntax causedBy) =>
        causedBy.Property is null ? "context.CausedBy" : $"context.CausedBy.{Identifiers.ToPascalCase(causedBy.Property)}";

    /// <inheritdoc/>
    public string RenderEventSourceId() => "context.EventSourceId";

    static string Path(string path) => $"context.{string.Join('.', path.Split('.').Select(Identifiers.ToPascalCase))}";
}
