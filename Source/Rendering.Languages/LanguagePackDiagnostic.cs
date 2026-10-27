// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Reports a semantic gap without inventing a diagnostic code in the planner-owned STAGE-DEGRADE namespace.
/// </summary>
/// <param name="File">The stable file identity.</param>
/// <param name="Segment">The zero-based input segment position.</param>
/// <param name="Code">The existing degradation code supplied by the gap's caller.</param>
/// <param name="Construct">The unsupported construct.</param>
/// <param name="Intent">The stated model intent.</param>
/// <param name="Context">The placeholder's grammar context.</param>
public sealed record LanguagePackDiagnostic(string File, int Segment, string Code, string Construct, string Intent, string Context);
