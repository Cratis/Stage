// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.Renderers;

namespace Cratis.Stage.Rendering.Cratis.Authorization;

/// <summary>
/// Renders the authorization attribute guarding one read model's rendered read surface, from the queries that
/// read <b>that</b> read model.
/// </summary>
/// <remarks>
/// <para>
/// A slice declares as many read models as its behavior needs, and a query names the one it reads with its
/// return type. Authorization has to follow that attribution: a query returning <c>OverdueInvoices</c> says
/// nothing about who may read <c>InvoiceSummary</c>, so the union guarding a read model is taken over its own
/// queries and no others. Which queries those are is <see cref="QueryRenderer.Reads"/>, the same rule that decides
/// which read model a query's rendered method lives on — a guard drawn from one set of queries and a method
/// rendered for another would guard something nobody reads.
/// </para>
/// <para>
/// The attribution is a correctness concern rather than a tidy one, because getting it wrong fails in the
/// <b>permissive</b> direction. <see cref="AuthorizationRenderer"/> collapses a union to <c>AllowAnonymous</c>
/// as soon as one member is unguarded — correctly, for queries that genuinely read the same model — so drawing
/// the union from every query in the slice let a single unguarded query on an unrelated read model publish a
/// guarded one to everyone.
/// </para>
/// <para>
/// When the slice declares queries but none of them return this read model, the document has stated who may
/// read the slice's other models and nothing about this one. Reading that silence as anonymous would take
/// permission from a document that never granted it, so it falls back to requiring an authenticated caller and
/// reports it — the same fallback used for every other requirement no attribute expresses faithfully. A slice
/// declaring no query at all says nothing about reading anything, and keeps the <c>AllowAnonymous</c> that
/// absence has always rendered as.
/// </para>
/// </remarks>
public static class ReadModelAuthorization
{
    /// <summary>
    /// Renders the authorization attribute for a read model, from the queries that return it.
    /// </summary>
    /// <param name="readModel">The rendered read model's C# type name.</param>
    /// <param name="queries">Every <see cref="QuerySyntax"/> the slice declares, across all of its read models.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> the policies are resolved against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The attribute content, without the surrounding brackets.</returns>
    public static string Render(
        string readModel, IEnumerable<QuerySyntax> queries, ApplicationSet applicationSet, ICollection<string> diagnostics)
    {
        var declared = queries.ToArray();
        var own = declared.Where(query => QueryRenderer.Reads(query, readModel)).ToArray();
        var subject = $"Read model '{readModel}'";

        if (own.Length == 0 && declared.Length > 0)
        {
            diagnostics.Add(
                $"{subject} is returned by none of the {declared.Length} query declaration(s) in its slice — the " +
                "document states who may read the other read models and nothing about this one, so its rendered " +
                "read surface requires an authenticated caller rather than being left open to everyone.");
            return "Authorize";
        }

        return AuthorizationRenderer.Render(own.Select(query => query.Authorize), applicationSet, subject, diagnostics);
    }
}
