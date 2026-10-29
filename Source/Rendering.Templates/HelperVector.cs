// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// A frozen template input and its expected text, structured output or safety refusal.
/// </summary>
/// <param name="Id">Stable example identifier.</param>
/// <param name="Template">Template source.</param>
/// <param name="Scope">JSON input scope.</param>
/// <param name="Text">Expected text, if applicable.</param>
/// <param name="Segments">Expected ordered text and symbol segments, if applicable.</param>
/// <param name="Gap">Expected degradation code, if applicable.</param>
/// <param name="Refusal">Expected render refusal code, if applicable.</param>
/// <param name="PrepareRefusal">Expected preparation refusal code, if applicable.</param>
/// <param name="Operators">Whether the example uses the test operator table and leaf partial.</param>
public sealed record HelperVector(
    string Id,
    string Template,
    JsonElement Scope,
    string? Text = null,
    ImmutableArray<string> Segments = default,
    string? Gap = null,
    string? Refusal = null,
    string? PrepareRefusal = null,
    bool Operators = false);
