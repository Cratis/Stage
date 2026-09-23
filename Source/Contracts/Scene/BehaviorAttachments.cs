// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using SceneModel = Cratis.Scene.Model.Interactions;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Resolves what is attached at one point in the containment tree into the behaviors the model carries there.
/// </summary>
/// <remarks>
/// A single attachment point mixes two forms: inline <c language="csharp">on</c> blocks, which are anonymous behaviors written
/// where they apply, and <c language="csharp">uses</c> clauses, which name a behavior declared elsewhere. They are the same
/// construct once resolved, which is why they end up in one list.
/// </remarks>
public static class BehaviorAttachments
{
    /// <summary>
    /// Builds the lookup a <c language="csharp">uses</c> clause resolves against.
    /// </summary>
    /// <param name="declared">The behaviors the document declares.</param>
    /// <returns>The declared behaviors by name.</returns>
    /// <remarks>
    /// A behavior declared without a name cannot be the target of a <c language="csharp">uses</c>, so it is not in the lookup.
    /// The compiler already rejects a duplicate name, so the last one wins here rather than throwing - by the
    /// time Stage sees the document, that has been reported.
    /// </remarks>
    public static IReadOnlyDictionary<string, ScreenplaySyntax.BehaviorSyntax> Declared(
        IEnumerable<ScreenplaySyntax.BehaviorSyntax> declared)
    {
        var lookup = new Dictionary<string, ScreenplaySyntax.BehaviorSyntax>(StringComparer.Ordinal);
        foreach (var behavior in declared.Where(behavior => behavior.Name is not null))
        {
            lookup[behavior.Name!] = behavior;
        }

        return lookup;
    }

    /// <summary>
    /// Converts what is attached at one point into Scene behaviors.
    /// </summary>
    /// <param name="inline">The inline <c language="csharp">on</c> blocks written at this point.</param>
    /// <param name="used">The <c language="csharp">uses</c> clauses written at this point.</param>
    /// <param name="declared">The declared behaviors to resolve names against.</param>
    /// <param name="subject">What is being attached to, for reporting.</param>
    /// <param name="findings">Where to report a <c language="csharp">uses</c> that names nothing.</param>
    /// <returns>The attached behaviors, in the order they were authored.</returns>
    /// <remarks>
    /// Inline blocks and <c language="csharp">uses</c> clauses arrive in separate collections, so authored order is recovered
    /// from source location rather than assumed. Order matters: these run in sequence, and a confirm written
    /// before an execute is only a gate if it stays before it.
    /// <para>
    /// A <c language="csharp">uses</c> naming a behavior that does not exist is reported and dropped, not substituted with an
    /// empty behavior. The compiler reports it too; this is the second line of defence for a model assembled
    /// from parts that did not compile together.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<SceneModel.Behavior> Convert(
        IEnumerable<ScreenplaySyntax.BehaviorSyntax> inline,
        IEnumerable<ScreenplaySyntax.UsesBehaviorSyntax> used,
        IReadOnlyDictionary<string, ScreenplaySyntax.BehaviorSyntax> declared,
        string subject,
        ICollection<RenderFinding> findings)
    {
        var attached = new List<(SourceLocation Location, ScreenplaySyntax.BehaviorSyntax Behavior)>();

        foreach (var behavior in inline)
        {
            attached.Add((behavior.Location, behavior));
        }

        foreach (var uses in used)
        {
            if (declared.TryGetValue(uses.Behavior, out var behavior))
            {
                attached.Add((uses.Location, behavior));
                continue;
            }

            findings.Add(new RenderFinding(
                RenderFindingKind.BehaviorNotFound,
                subject,
                $"'{subject}' uses the behavior '{uses.Behavior}', which the document does not declare."));
        }

        return
        [
            .. attached
                .OrderBy(entry => entry.Location.Line)
                .ThenBy(entry => entry.Location.Column)
                .Select(entry => BehaviorConverter.Convert(entry.Behavior))
        ];
    }
}
