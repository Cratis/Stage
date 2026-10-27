// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Contains fully substituted files and deterministic semantic-gap diagnostics. Integrity failures throw instead.
/// </summary>
/// <param name="Files">The resolved files in ordinal file-identity order.</param>
/// <param name="Diagnostics">The diagnostics ordered by file identity, segment position, and code.</param>
public sealed record LanguagePackResolution(IReadOnlyList<ResolvedSymbolFile> Files, IReadOnlyList<LanguagePackDiagnostic> Diagnostics);
