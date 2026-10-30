// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Kits;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// Target-owned deterministic escaping, type spelling and placeholders.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "The accepted template v1 language seam names literal operations by type.")]
public interface ITemplateLanguage
{
    /// <summary>
    /// Gets the exact pack identity.
    /// </summary>
    LanguagePackVersion Pack { get; }

    /// <summary>
    /// Spells a string literal.
    /// </summary>
    /// <param name="value">Literal value.</param>
    /// <returns>Escaped target text.</returns>
    string String(string value);

    /// <summary>
    /// Spells an integer literal.
    /// </summary>
    /// <param name="value">Literal value.</param>
    /// <returns>Target text.</returns>
    string Integer(long value);

    /// <summary>
    /// Spells a decimal literal.
    /// </summary>
    /// <param name="value">Literal value.</param>
    /// <returns>Target text.</returns>
    string Decimal(decimal value);

    /// <summary>
    /// Spells a Boolean literal.
    /// </summary>
    /// <param name="value">Literal value.</param>
    /// <returns>Target text.</returns>
    string Boolean(bool value);

    /// <summary>
    /// Spells a null literal.
    /// </summary>
    /// <returns>Target text.</returns>
    string Null();

    /// <summary>
    /// Escapes comment text.
    /// </summary>
    /// <param name="value">Comment contents.</param>
    /// <returns>Safe target text.</returns>
    string Comment(string value);

    /// <summary>
    /// Escapes an identifier.
    /// </summary>
    /// <param name="value">Identifier.</param>
    /// <returns>Legal target identifier.</returns>
    string Identifier(string value);

    /// <summary>
    /// Spells a primitive as text or symbols.
    /// </summary>
    /// <param name="primitive">Semantic primitive.</param>
    /// <returns>Structured spelling.</returns>
    /// <exception cref="TemplateLanguageRefusal">The pack cannot express the input.</exception>
    TypeSpelling Primitive(string primitive);

    /// <summary>
    /// Wraps an element as a collection.
    /// </summary>
    /// <param name="element">Element spelling.</param>
    /// <returns>Structured spelling.</returns>
    TypeSpelling Collection(TypeSpelling element);

    /// <summary>
    /// Wraps a spelling as optional.
    /// </summary>
    /// <param name="element">Element spelling.</param>
    /// <returns>Structured spelling.</returns>
    TypeSpelling Optional(TypeSpelling element);

    /// <summary>
    /// Spells a safe placeholder for a degraded artifact.
    /// </summary>
    /// <param name="role">Safety role.</param>
    /// <param name="code">Degradation code.</param>
    /// <param name="intent">Bounded intent.</param>
    /// <returns>Target text.</returns>
    string Placeholder(GapRole role, string code, string intent);
}
