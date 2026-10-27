// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// A declaration, reference or imports insertion point.
/// </summary>
/// <param name="Kind">Use kind.</param>
/// <param name="Symbol">Symbol, or null for imports.</param>
public sealed record TemplateSymbolSegment(TemplateSymbolKind Kind, TemplateSymbol? Symbol) : TemplateSegment;
