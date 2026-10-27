// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Supplies structured, sentinel-free input to a language pack. Sentinel decoding and authenticity belong to the planner.
/// </summary>
/// <param name="Files">The files in the plan.</param>
/// <param name="ExternalTypes">Types available outside the plan. Their member sets are unknown unless supplied through <see cref="ExternalDeclarations"/>.</param>
public sealed record SymbolPlan(IReadOnlyList<SymbolFile> Files, IReadOnlyList<TypeName> ExternalTypes)
{
    /// <summary>
    /// Gets enriched external declarations, including any additional external types not listed in <see cref="ExternalTypes"/>.
    /// This additive metadata keeps existing name-only plans conservative: absent member metadata is unknown, not known-empty.
    /// </summary>
    public IReadOnlyList<ExternalType> ExternalDeclarations { get; init; } = [];
}
