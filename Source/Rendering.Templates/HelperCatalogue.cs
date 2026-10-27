// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

internal static class HelperCatalogue
{
    internal static readonly Entry[] Entries =
    [
        new("name.words", "(nameWords) -> text"),
        new("name.pascal", "(nameWords) -> text"),
        new("name.camel", "(nameWords) -> text"),
        new("name.snake", "(nameWords) -> text"),
        new("name.kebab", "(nameWords) -> text"),
        new("name.constant", "(nameWords) -> text"),
        new("lit.string", "(string) -> text"),
        new("lit.integer", "(int64) -> text"),
        new("lit.decimal", "(number) -> text"),
        new("lit.boolean", "(bool) -> text"),
        new("lit.null", "() -> text; write lit['null'] in Scriban"),
        new("lit.comment", "(string) -> text"),
        new("lit.identifier", "(string) -> text"),
        new("sym.decl", "(namespace, name) -> declaration"),
        new("sym.ref", "(namespace, name) -> reference"),
        new("sym.imports", "() -> imports insertion point"),
        new("type.ref", "(RenderTypeRef) -> type parts"),
        new("expr.supported", "(expression, literalTable) -> bool"),
        new("expr.lower", "(expression, literalTable, literalLeaf, literalRole) -> parts"),
        new("render", "(literalPartial, arg = null) -> partial output"),
        new("section", "wrap section 'name' ... end -> inline body"),
        new("gap", "(role, intent, modelPath, code = 'STAGE-DEGRADE-002') -> placeholder and gap"),
        new("refuse", "(literalCategory, message) -> whole-plan refusal")
    ];

    internal static string Canonical => string.Join('\n', Entries.OrderBy(entry => entry.Name, StringComparer.Ordinal).Select(entry => entry.Name + "|" + entry.Signature + "|" + entry.Since));

    internal static string Reference => """
        ---
        title: Template profile v1 helpers
        description: The locked stage-templates/v1 helper signatures and standard-library allowlist.
        ---

        # Template profile v1 helpers

        The `stage-templates/v1` profile is the shared interpreter contract for declarative renderer kits.
        This table is generated from the helper catalogue and checked against it by specifications.
        Target packs own literal escaping, primitive spelling, optional and collection wrappers, and safe placeholders.
        See [Author a renderer kit](../guides/template-kits.md) for scope, budgets, symbols and safety boundaries.

        ## Helpers

        | Helper | Signature | Since |
        | --- | --- | --- |
        """ + "\n" + string.Join('\n', Entries.OrderBy(entry => entry.Name, StringComparer.Ordinal).Select(entry => "| `" + entry.Name + "` | `" + entry.Signature + "` | " + entry.Since + " |")) + "\n\n## Allowed standard library\n\n" + string.Join('\n', LockedBuiltins.Members.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => "- `" + entry.Key + "`: " + string.Join(", ", entry.Value.Order(StringComparer.Ordinal).Select(name => "`" + name + "`")) + ".")) + "\n\nEverything else is unavailable, including date/time, random, environment, regex, dynamic evaluation and JSON parsing.\n";

    internal sealed record Entry(string Name, string Signature, string Since = "1.0");
}
