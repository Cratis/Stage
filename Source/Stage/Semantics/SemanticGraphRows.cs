// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Api;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Helpers for the rows a graph query returns.
/// </summary>
internal static class SemanticGraphRows
{
    /// <summary>
    /// Copies the values of one row onto another that already carries the identity.
    /// </summary>
    /// <param name="withId">The row that carries the identity.</param>
    /// <param name="row">The row whose values are copied.</param>
    /// <returns>The row that carries the identity and the values.</returns>
    internal static DynamicReadModel Merge(DynamicReadModel withId, DynamicReadModel row)
    {
        foreach (var value in row.Values) withId.Values[value.Key] = value.Value;
        return withId;
    }
}
