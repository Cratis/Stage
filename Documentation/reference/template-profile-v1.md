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
| `expr.lower` | `(expression, literalTable, literalLeaf, literalRole) -> parts` | 1.0 |
| `expr.supported` | `(expression, literalTable) -> bool` | 1.0 |
| `gap` | `(role, intent, modelPath, code = 'STAGE-DEGRADE-002') -> placeholder and gap` | 1.0 |
| `lit.boolean` | `(bool) -> text` | 1.0 |
| `lit.comment` | `(string) -> text` | 1.0 |
| `lit.decimal` | `(number) -> text` | 1.0 |
| `lit.identifier` | `(string) -> text` | 1.0 |
| `lit.integer` | `(int64) -> text` | 1.0 |
| `lit.null` | `() -> text; write lit['null'] in Scriban` | 1.0 |
| `lit.string` | `(string) -> text` | 1.0 |
| `name.camel` | `(nameWords) -> text` | 1.0 |
| `name.constant` | `(nameWords) -> text` | 1.0 |
| `name.kebab` | `(nameWords) -> text` | 1.0 |
| `name.pascal` | `(nameWords) -> text` | 1.0 |
| `name.snake` | `(nameWords) -> text` | 1.0 |
| `name.words` | `(nameWords) -> text` | 1.0 |
| `refuse` | `(literalCategory, message) -> whole-plan refusal` | 1.0 |
| `render` | `(literalPartial, arg = null) -> partial output` | 1.0 |
| `section` | `wrap section 'name' ... end -> inline body` | 1.0 |
| `sym.decl` | `(namespace, name) -> declaration` | 1.0 |
| `sym.imports` | `() -> imports insertion point` | 1.0 |
| `sym.ref` | `(namespace, name) -> reference` | 1.0 |
| `type.ref` | `(RenderTypeRef) -> type parts` | 1.0 |

## Allowed standard library

- `array`: `add`, `add_range`, `any`, `compact`, `concat`, `contains`, `cycle`, `each`, `filter`, `first`, `join`, `last`, `limit`, `map`, `offset`, `remove_at`, `reverse`, `size`, `sort`, `uniq`.
- `math`: `abs`, `ceil`, `divided_by`, `floor`, `is_number`, `minus`, `modulo`, `plus`, `round`, `times`.
- `object`: `default`, `has_key`, `has_value`, `keys`, `kind`, `size`, `typeof`, `values`.
- `string`: `append`, `capitalize`, `capitalizewords`, `contains`, `downcase`, `empty`, `ends_with`, `equals_ignore_case`, `escape`, `handleize`, `index_of`, `literal`, `lstrip`, `pad_left`, `pad_right`, `pluralize`, `prepend`, `remove`, `remove_first`, `remove_last`, `replace`, `replace_first`, `rstrip`, `size`, `slice`, `slice1`, `split`, `starts_with`, `strip`, `to_double`, `to_float`, `to_int`, `to_long`, `truncate`, `upcase`, `whitespace`.

Everything else is unavailable, including date/time, random, environment, regex, dynamic evaluation and JSON parsing.
