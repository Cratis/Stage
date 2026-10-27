// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Supplies structured, sentinel-free input to a language pack. Sentinel decoding and authenticity belong to the planner.
/// </summary>
/// <param name="Files">The files in the plan.</param>
/// <param name="ExternalTypes">Types available outside the plan.</param>
public sealed record SymbolPlan(IReadOnlyList<SymbolFile> Files, IReadOnlyList<TypeName> ExternalTypes);
