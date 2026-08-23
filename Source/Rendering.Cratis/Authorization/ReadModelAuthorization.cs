// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.Renderers;

namespace Cratis.Stage.Rendering.Cratis.Authorization;

/// <summary>
/// Renders type-level authorization only for the fixed query pair synthesized when a read model has no declared
/// query method of its own.
/// </summary>
/// <remarks>
/// Declared queries are separate Arc operations and carry their exact authorization on their generated methods.
/// Their policies must never be combined on the read-model type: doing so widens distinct operations, such as an
/// administrator-only <c>All</c> and an auditor-only <c>Mine</c>, so either role can call both.
/// <para>
/// A type-level attribute remains necessary only when <see cref="QueryRenderer"/> synthesizes its fixed all/by-id
/// pair because no declared query returns the read model. If the slice declares queries for other read models,
/// the document grants no access to this invented pair, so it requires authentication and reports that inference.
/// If the slice declares no query at all, the historical explicit <c>AllowAnonymous</c> semantics remain.
/// </para>
/// </remarks>
public static class ReadModelAuthorization
{
    /// <summary>
    /// Renders the authorization shared by the synthesized fixed pair, or no type-level attribute when declared
    /// query methods carry their own authorization.
    /// </summary>
    /// <param name="readModel">The rendered read model's C# type name.</param>
    /// <param name="queries">Every <see cref="QuerySyntax"/> the slice declares, across all of its read models.</param>
    /// <param name="diagnostics">Collects the inferred fixed-pair authorization diagnostic.</param>
    /// <returns>The attribute content without brackets, or <see langword="null"/> for method-authorized queries.</returns>
    public static string? Render(string readModel, IEnumerable<QuerySyntax> queries, ICollection<string> diagnostics)
    {
        var declared = queries.ToArray();
        if (declared.Any(query => QueryRenderer.Reads(query, readModel)))
        {
            return null;
        }

        if (declared.Length > 0)
        {
            diagnostics.Add(
                $"Read model '{readModel}' is returned by none of the {declared.Length} query declaration(s) in its slice — the " +
                "document states who may read the other read models and nothing about this one, so its synthesized " +
                "query pair requires an authenticated caller rather than being left open to everyone.");
            return "Authorize";
        }

        return "AllowAnonymous";
    }
}
