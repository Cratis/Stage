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
    /// Reports every construct family the slice declares that the rendered file has no equivalent for, for a
    /// renderer that emits no read model.
    /// </summary>
    /// <param name="builder">The <see cref="CSharpCodeBuilder"/> to emit the in-file notes to.</param>
    /// <param name="slice">The <see cref="SliceSyntax"/> being rendered.</param>
    /// <param name="rendered">What the calling renderer emits; everything beyond it is reported.</param>
    /// <param name="diagnostics">Collects what could not be rendered faithfully.</param>
    public static void Report(CSharpCodeBuilder builder, SliceSyntax slice, RenderedConstructs rendered, ICollection<string> diagnostics) =>
        Report(builder, slice, rendered, diagnostics, null);

    /// <summary>
    /// Reports every construct family the slice declares that the rendered file has no equivalent for.
    /// </summary>
    /// <param name="builder">The <see cref="CSharpCodeBuilder"/> to emit the in-file notes to.</param>
    /// <param name="slice">The <see cref="SliceSyntax"/> being rendered.</param>
    /// <param name="rendered">What the calling renderer emits; everything beyond it is reported.</param>
    /// <param name="diagnostics">Collects what could not be rendered faithfully.</param>
    /// <param name="readModel">The read model the calling renderer emits, and <see langword="null"/> when it emits
    /// none. A query's method lives on the read model its return type names, so this is what decides which of the
    /// slice's queries the file can hold a method for at all.</param>
    public static void Report(
        CSharpCodeBuilder builder,
        SliceSyntax slice,
        RenderedConstructs rendered,
        ICollection<string> diagnostics,
        string? readModel)
    {
        foreach (var (count, keyword, consequence) in Families(slice, rendered, readModel).Where(family => family.Count > 0))
        {
            builder.Line($"// TODO: {count} {keyword} declaration(s) not rendered — {consequence}");
            diagnostics.Add($"Slice '{slice.Name}' declares {count} {keyword} declaration(s) with no rendered equivalent — {consequence}");
        }

        ReportQueriesReadingAnotherReadModel(builder, slice, readModel, diagnostics);
    }

    // Reported one by one rather than counted, because which query it is and what it returns is the whole content
    // of the report: the alternative to leaving the method out is rendering it against the read model that is here,
    // which answers with a different read model than the document states — a value nobody wrote, and worse than a
    // missing method.
    static void ReportQueriesReadingAnotherReadModel(
        CSharpCodeBuilder builder, SliceSyntax slice, string? readModel, ICollection<string> diagnostics)
    {
        foreach (var query in slice.Queries.Where(query => !QueryRenderer.HasRenderedMethod(query, readModel)))
        {
            builder.Line($"// TODO: query '{query.Name}' not rendered — it returns '{query.ReturnType.Name}', which is not the read model rendered here");
            diagnostics.Add(
                $"Query '{query.Name}' returns '{query.ReturnType.Name}', which is not the read model rendered here — no method is " +
                "rendered for it, since rendering one against the read model that is here would answer with a read model the " +
                "document does not ask it for.");
        }
    }

    static IEnumerable<(int Count, string Keyword, string Consequence)> Families(SliceSyntax slice, RenderedConstructs rendered, string? readModel)
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

        // A declared query now renders as a method named after it. What is still reported is the narrowing it
        // states and cannot get - a filter, or a performer holding the body - since the method is rendered either
        // way and a reader has no other way to learn it answers less than the document says. Only the queries that
        // do get a method are counted here; a query with no method at all is reported whole, on its own.
        yield return (
            slice.Queries.Count(query => QueryRenderer.HasRenderedMethod(query, readModel) && !QueryRenderer.IsFullyRendered(query)),
            "query",
            "it is rendered as a method reading the whole read model, without the narrowing the document states.");
        yield return (
            slice.Queries.Count(query => QueryRenderer.HasRenderedMethod(query, readModel) && query.Performer is not null),
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
