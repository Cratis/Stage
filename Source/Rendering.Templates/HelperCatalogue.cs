// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// The frozen stage-templates/v1 helper surface and executable examples.
/// </summary>
public static class HelperCatalogue
{
    /// <summary>
    /// Gets every helper in the template profile.
    /// </summary>
    public static ImmutableArray<HelperDescriptor> All { get; } =
    [
        new("name.words", "(nameWords) -> text", "Joins nonempty name words with spaces using invariant lowercase.", Vectors: FrozenHelperVectors.For("word-arrays")),
        new("name.pascal", "(nameWords) -> text", "Capitalizes each word's first Unicode scalar invariantly, preserving the remaining characters.", Vectors: FrozenHelperVectors.For("word-arrays", "unicode-scalar", "strict-name")),
        new("name.camel", "(nameWords) -> text", "Lowercases the entire first word and capitalizes subsequent words' first Unicode scalar invariantly.", Vectors: FrozenHelperVectors.For("word-arrays")),
        new("name.snake", "(nameWords) -> text", "Joins invariant lowercase words with underscores.", Vectors: FrozenHelperVectors.For("word-arrays", "unicode-scalar")),
        new("name.kebab", "(nameWords) -> text", "Joins invariant lowercase words with hyphens.", Vectors: FrozenHelperVectors.For("word-arrays")),
        new("name.constant", "(nameWords) -> text", "Joins invariant uppercase words with underscores.", Vectors: FrozenHelperVectors.For("word-arrays")),
        new("lit.string", "(string) -> text", "Delegates string escaping to the language pack without primitive coercion.", Vectors: FrozenHelperVectors.For("literal-seam")),
        new("lit.integer", "(int64) -> text", "Delegates signed 64-bit integer spelling to the language pack without string coercion.", Vectors: FrozenHelperVectors.For("literal-seam", "strict-literal")),
        new("lit.decimal", "(number) -> text", "Delegates decimal number spelling to the language pack.", Vectors: FrozenHelperVectors.For("literal-seam")),
        new("lit.boolean", "(bool) -> text", "Delegates boolean spelling to the language pack.", Vectors: FrozenHelperVectors.For("literal-seam")),
        new("lit.null", "() -> text; write lit['null'] in Scriban", "Delegates null spelling to the language pack. Use `lit['null']` rather than `lit.null`: Scriban reserves the null keyword.", Vectors: FrozenHelperVectors.For("literal-seam")),
        new("lit.comment", "(string) -> text", "Delegates safe comment content to the language pack.", Vectors: FrozenHelperVectors.For("literal-seam")),
        new("lit.identifier", "(string) -> text", "Delegates safe identifier spelling to the language pack.", Vectors: FrozenHelperVectors.For("literal-seam")),
        new("sym.decl", "(namespace, name) -> declaration", "Emits a structured declaration handle; handles cannot be converted to text or captured.", Vectors: FrozenHelperVectors.For("symbols")),
        new("sym.ref", "(namespace, name) -> reference", "Emits a structured reference handle, preserving namespace and name separately.", Vectors: FrozenHelperVectors.For("symbols", "symbol-null-test", "symbol-text")),
        new("sym.imports", "() -> imports insertion point", "Emits a structured imports insertion point for the language pack.", Vectors: FrozenHelperVectors.For("symbols")),
        new("impl.body", "(requirementId) -> authored body", "Emits a structured authored-body handle for an implementation requirement; the body text never enters the template output and handles cannot be converted to text or captured.", Vectors: FrozenHelperVectors.For("authored-body", "authored-body-text")),
        new("type.ref", "(RenderTypeRef) -> type parts", "Resolves semantic identities and delegates primitive, collection and optional spelling to the language pack.", Vectors: FrozenHelperVectors.For("type-text", "type-symbol", "type-identity", "unknown-type")),
        new("expr.supported", "(expression, literalTable) -> bool", "Tests expression support using a declared, literal operator table.", Vectors: FrozenHelperVectors.For("expression-support", "no-dynamic-expression-table")),
        new("expr.lower", "(expression, literalTable, literalLeaf, literalRole) -> parts", "Lowers supported expressions to structured parts; unsupported semantics produce a role-safe degradation.", Vectors: FrozenHelperVectors.For("expression-leaf", "expression-gap")),
        new("render", "(literalPartial, arg = null) -> partial output", "Renders a declared, literal partial with an isolated item argument.", Vectors: FrozenHelperVectors.For("render-partial", "no-dynamic-partial", "ordinary-text")),
        new("section", "wrap section 'name' ... end -> inline body", "Renders an inline section body unless a validated override replaces it. Sections cannot be nested.", Vectors: FrozenHelperVectors.For("default-section", "no-nested-section")),
        new("gap", "(role, intent, modelPath, code = 'STAGE-DEGRADE-002') -> placeholder and gap", "Emits a language-pack placeholder and records a semantic gap, not a safety refusal.", Vectors: FrozenHelperVectors.For("default-gap", "gap-003", "gap-005")),
        new("refuse", "(literalCategory, message) -> whole-plan refusal", "Latches a safety refusal for a literal trust, integrity or budget category. The caller must discard all plan outputs.", Vectors: FrozenHelperVectors.For("refuse-trust", "no-unknown-refusal", "no-clock"))
    ];

    internal static string Canonical => string.Join('\n', All.OrderBy(helper => helper.Name, StringComparer.Ordinal).Select(helper => helper.Name + "|" + helper.Signature + "|" + helper.Since));

    /// <summary>
    /// Generates the Markdown reference from the catalogue, frozen vectors and profile limits.
    /// </summary>
    /// <returns>The reference text with deterministic LF line endings.</returns>
    public static string GenerateReference() => HelperReference.Generate();
}
