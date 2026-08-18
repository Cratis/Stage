// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Reports the construct families a slice declares that no renderer emits anything for — the same treatment the
/// projection blocks with no model-bound equivalent already get: a <c>TODO</c> in the emitted file and a
/// diagnostic on it.
/// </summary>
/// <remarks>
/// A construct that renders to nothing and says nothing is indistinguishable from one that was never declared.
/// The rendered application then looks complete while whole families of behavior — who may read what, which
/// values must stay unique, the entire user interface — are simply missing. Every family listed here is a
/// promise the Screenplay document makes that the rendered output does not keep, so each one says so.
/// </remarks>
public static class UnrenderedConstructs
{
    /// <summary>
    /// Reports every construct family the slice declares that the rendered file has no equivalent for.
    /// </summary>
    /// <param name="builder">The <see cref="CSharpCodeBuilder"/> to emit the in-file notes to.</param>
    /// <param name="slice">The <see cref="SliceSyntax"/> being rendered.</param>
    /// <param name="rendered">What the calling renderer emits; everything beyond it is reported.</param>
    /// <param name="diagnostics">Collects what could not be rendered faithfully.</param>
    public static void Report(CSharpCodeBuilder builder, SliceSyntax slice, RenderedConstructs rendered, ICollection<string> diagnostics)
    {
        foreach (var (count, keyword, consequence) in Families(slice, rendered).Where(family => family.Count > 0))
        {
            builder.Line($"// TODO: {count} {keyword} declaration(s) not rendered — {consequence}");
            diagnostics.Add($"Slice '{slice.Name}' declares {count} {keyword} declaration(s) with no rendered equivalent — {consequence}");
        }
    }

    static IEnumerable<(int Count, string Keyword, string Consequence)> Families(SliceSyntax slice, RenderedConstructs rendered)
    {
        yield return (
            slice.Commands.Count() - (rendered.HasFlag(RenderedConstructs.Command) ? 1 : 0),
            "command",
            "neither its input, the events it produces nor its authorization is rendered.");
        yield return (
            rendered.HasFlag(RenderedConstructs.ReadModel) ? slice.Projections.Count() - 1 : slice.Projections.Count(),
            "projection",
            "no read model is rendered for it.");

        // A rendered read model is inferred from the slice's first projection, so at most one declared readmodel
        // has a rendered counterpart — counted the same way the projections above are.
        yield return (
            rendered.HasFlag(RenderedConstructs.ReadModel) ? ReadModels(slice).Count() - 1 : ReadModels(slice).Count(),
            "readmodel",
            "nothing in the rendered application holds the state it declares.");
        yield return (
            Reducers(slice).Count(),
            "reducer",
            "the read model it builds is never populated in the rendered application.");
        yield return (
            rendered.HasFlag(RenderedConstructs.Reactions) ? 0 : slice.Reactions.Count(),
            "reaction",
            "nothing reacts to the events in the rendered application.");
        yield return (
            slice.Queries.Count(),
            "query",
            "the read model carries the fixed all/by-id pair instead, guarded by the union of the authorization declared by the queries that read it.");
        yield return (
            slice.Queries.Count(query => query.Performer is not null),
            "query performer",
            "the query logic is not rendered.");

        // Only what rendering does not place on an event: a file-backed constraint, and one naming an event or
        // property the slice does not declare. The rest now render as Chronicle [Unique] attributes.
        yield return (
            slice.Constraints.Count(constraint => !ConstraintRenderer.IsRendered(constraint, slice.Events)),
            "constraint",
            "uniqueness is not enforced in the rendered application.");
        yield return (
            slice.Screens.Count(),
            "screen",
            "the rendered application has no user interface.");
        yield return (
            slice.Captures.Count(),
            "capture",
            "no ingestion of the captured source is rendered.");

        // Specifications are rendered separately, one file each, and each one that cannot be says so on its own
        // — so counting them here would report the same thing twice and count the rendered ones as dropped.
    }

    // Both collections are trailing optionals on SliceSyntax and are null on a slice that declares neither.
    static IEnumerable<ReadModelSyntax> ReadModels(SliceSyntax slice) => slice.ReadModels ?? [];

    static IEnumerable<ReducerSyntax> Reducers(SliceSyntax slice) => slice.Reducers ?? [];
}
