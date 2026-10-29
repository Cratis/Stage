// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents one structured piece of a rendered file handed to a language for resolution.
/// </summary>
/// <remarks>Segments are handles, never token text: a language decides how a symbol is spelled and imported.</remarks>
public abstract record DeclarativeSegment;

/// <summary>
/// Literal template text.
/// </summary>
/// <param name="Value">The text.</param>
public sealed record DeclarativeTextSegment(string Value) : DeclarativeSegment;

/// <summary>
/// A declaration, a reference or the imports block of a file.
/// </summary>
/// <param name="Kind">The symbol use.</param>
/// <param name="Value">The symbol, or <see langword="null"/> for the imports block.</param>
public sealed record DeclarativeSymbolSegment(TemplateSymbolKind Kind, TemplateSymbol? Value) : DeclarativeSegment;

/// <summary>
/// A span of authored implementation that the language admits and the planner never rewrites.
/// </summary>
/// <param name="RequirementId">The implementation requirement the body belongs to.</param>
/// <param name="Body">The admitted authored body; a language returns it unchanged as an authored piece.</param>
public sealed record DeclarativeAuthoredSegment(string RequirementId, string Body) : DeclarativeSegment;
