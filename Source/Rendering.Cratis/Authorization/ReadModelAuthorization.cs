// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Authorization;

/// <summary>
/// Determines whether a read model's synthesized query pair has a type-level authorization attribute.
/// </summary>
/// <remarks>
/// Declared queries carry their own authorization on their generated methods. A synthesized pair has no
/// authorization declaration of its own and must not infer one from a sibling query. The legacy syntax path
/// refuses enclosing slice and module authorization rather than ignoring it; the semantic path renders those
/// clauses from the effective authorization supplied by Screenplay.
/// </remarks>
public static class ReadModelAuthorization
{
    /// <summary>
    /// Leaves authorization to declared query methods or the host when no clause covers a synthesized pair.
    /// </summary>
    /// <param name="readModel">The rendered read model's C# type name.</param>
    /// <param name="queries">Every <see cref="QuerySyntax"/> the slice declares, across all of its read models.</param>
    /// <param name="diagnostics">Collects authorization diagnostics.</param>
    /// <returns>No type-level attribute.</returns>
    public static string? Render(string readModel, IEnumerable<QuerySyntax> queries, ICollection<string> diagnostics) => null;
}
