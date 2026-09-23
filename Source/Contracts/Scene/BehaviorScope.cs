// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneModel = Cratis.Scene.Model.Interactions;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Carries what a <c language="csharp">uses</c> clause resolves against, and where to report one that resolves to nothing.
/// </summary>
/// <param name="Declared">The behaviors the document declares, by name.</param>
/// <param name="Findings">Where to report an unresolved <c language="csharp">uses</c>.</param>
public sealed record BehaviorScope(
    IReadOnlyDictionary<string, ScreenplaySyntax.BehaviorSyntax> Declared,
    ICollection<RenderFinding> Findings)
{
    /// <summary>
    /// A scope for a document that declares nothing, used where behaviors are not being translated.
    /// </summary>
    public static BehaviorScope None => new(new Dictionary<string, ScreenplaySyntax.BehaviorSyntax>(StringComparer.Ordinal), []);

    /// <summary>
    /// Creates a scope from a document's declarations.
    /// </summary>
    /// <param name="syntax">The application to read declarations from.</param>
    /// <param name="findings">Where to report an unresolved <c language="csharp">uses</c>.</param>
    /// <returns>The <see cref="BehaviorScope"/>.</returns>
    public static BehaviorScope For(ScreenplaySyntax.ApplicationSyntax syntax, ICollection<RenderFinding> findings) =>
        new(BehaviorAttachments.Declared(syntax.Behaviors), findings);

    /// <summary>
    /// Resolves what is attached at one point.
    /// </summary>
    /// <param name="inline">The inline <c language="csharp">on</c> blocks written there.</param>
    /// <param name="used">The <c language="csharp">uses</c> clauses written there.</param>
    /// <param name="subject">What is being attached to, for reporting.</param>
    /// <returns>The attached behaviors, in authored order.</returns>
    public IReadOnlyList<SceneModel.Behavior> Resolve(
        IEnumerable<ScreenplaySyntax.BehaviorSyntax> inline,
        IEnumerable<ScreenplaySyntax.UsesBehaviorSyntax> used,
        string subject) =>
        BehaviorAttachments.Convert(inline, used, Declared, subject, Findings);
}
