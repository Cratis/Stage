// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Admits only query intent the legacy renderer can preserve, before any emission.
/// </summary>
internal static class QueryAdmission
{
    internal static void EnsureSupported(IEnumerable<QuerySyntax> queries, string? slicePath = null)
    {
        foreach (var query in queries)
        {
            // Filter declarations describe an argument contract, not an executable predicate. Prefer the first
            // filter deterministically even if a performer is also present; neither contract may be discarded.
            if (query.Filters.FirstOrDefault() is { } filter)
            {
                throw new UnsupportedQueryIntent(query.Name, query.ReturnType.Name, filter.Location, UnsupportedQueryIntentReason.FilterContract, slicePath);
            }

            if (query.Performer is not { } performer)
            {
                continue;
            }

            // Inspect attachment kinds only: never parse a body, resolve a path, or probe the filesystem.
            var (reason, location) = (performer.File, performer.Code) switch
            {
                ({ } file, null) => (UnsupportedQueryIntentReason.FilePerformer, file.Location),
                (null, { } code) => (UnsupportedQueryIntentReason.InlinePerformer, code.Location),
                _ => (UnsupportedQueryIntentReason.UnsupportedPerformer, performer.Location)
            };
            throw new UnsupportedQueryIntent(query.Name, query.ReturnType.Name, location, reason, slicePath);
        }
    }
}
