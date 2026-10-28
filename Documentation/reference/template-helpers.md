---
title: Template helpers
description: Frozen stage-templates/v1 helpers, examples, safety codes and budgets.
---

<!-- Generated, do not edit. Source: HelperCatalogue.GenerateReference(). -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

The `stage-templates/v1` profile is the locked, target-neutral interpreter contract for declarative renderer kits.
Examples use the test language pack, not a production target. Vectors marked with operators use the test
operator table and a declared `leaf` partial that emits a `Domain` symbol reference for `item.identifier`.
Outputs show JSON-quoted text, ordered segments or a safety refusal; gaps are recorded alongside text.
Helper bytes, signatures and semantics are frozen once released. Change them under a new helper name or `templates/v2`.
See [Author a renderer kit](../guides/template-kits.md) for scope, symbols and caller safety obligations.

## expr.lower

` (expression, literalTable, literalLeaf, literalRole) -> parts ` — since v1.

Lowers supported expressions to structured parts; unsupported semantics produce a role-safe degradation.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` expression-leaf (operators) ` | ` {{ expr.lower model.expression 'operators' 'leaf' 'throwing_operation_body' }} ` | ` {"model":{"expression":{"$kind":"resolved","identifier":"Customer"}}} ` | ` ["Reference:Domain.Customer"] ` |
| ` expression-gap (operators) ` | ` {{ expr.lower model.expression 'operators' 'leaf' 'deny_enclosing_operation' }} ` | ` {"model":{"expression":{"$kind":"unknown"}}} ` | ` "FAIL[DenyEnclosingOperation:STAGE-DEGRADE-001]"; gap: STAGE-DEGRADE-001 ` |

## expr.supported

` (expression, literalTable) -> bool ` — since v1.

Tests expression support using a declared, literal operator table.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` expression-support (operators) ` | ` {{ expr.supported model.expression 'operators' }} ` | ` {"model":{"expression":{"$kind":"value","literal":{"$kind":"number","value":42}}}} ` | ` "true" ` |
| ` no-dynamic-expression-table ` | ` {{ expr.supported model.expression model.table }} ` | ` {"model":{}} ` | ` prepare refusal: STAGE-TPL-008 ` |

## gap

` (role, intent, modelPath, code = 'STAGE-DEGRADE-002') -> placeholder and gap ` — since v1.

Emits a language-pack placeholder and records a semantic gap, not a safety refusal.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` default-gap ` | ` {{ gap 'disabled_specification' 'Missing behavior' 'model.x' }} ` | ` {"model":{}} ` | ` "FAIL[DisabledSpecification:STAGE-DEGRADE-002]"; gap: STAGE-DEGRADE-002 ` |
| ` gap-003 ` | ` {{ gap 'unregistered_consumer' 'Missing behavior' 'model.x' 'STAGE-DEGRADE-003' }} ` | ` {"model":{}} ` | ` "FAIL[UnregisteredConsumer:STAGE-DEGRADE-003]"; gap: STAGE-DEGRADE-003 ` |
| ` gap-005 ` | ` {{ gap 'retained_signature_or_type' 'Missing behavior' 'model.x' 'STAGE-DEGRADE-005' }} ` | ` {"model":{}} ` | ` "FAIL[RetainedSignatureOrType:STAGE-DEGRADE-005]"; gap: STAGE-DEGRADE-005 ` |

## impl.body

` (requirementId) -> authored body ` — since v1.

Emits a structured authored-body handle for an implementation requirement; the body text never enters the template output and handles cannot be converted to text or captured.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` authored-body ` | ` A{{ impl.body 'r1' }}B ` | ` {"model":{}} ` | ` ["text:A","Authored:r1","text:B"] ` |
| ` authored-body-text ` | ` {{ (impl.body 'r1') + 'text' }} ` | ` {"model":{}} ` | ` render refusal: STAGE-TPL-022 ` |

## lit.boolean

` (bool) -> text ` — since v1.

Delegates boolean spelling to the language pack.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` literal-seam ` | ` {{ lit.string 'safe' }}\|{{ lit.integer model.integer }}\|{{ lit.decimal model.fraction }}\|{{ lit.boolean false }}\|{{ lit['null'] }}\|{{ lit.comment 'a*/b' }}\|{{ lit.identifier 'class' }} ` | ` {"model":{"integer":9223372036854775807,"fraction":1.25}} ` | ` "\u0022safe\u0022\|9223372036854775807\|1.25\|no\|nil\|a* /b\|@class" ` |

## lit.comment

` (string) -> text ` — since v1.

Delegates safe comment content to the language pack.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` literal-seam ` | ` {{ lit.string 'safe' }}\|{{ lit.integer model.integer }}\|{{ lit.decimal model.fraction }}\|{{ lit.boolean false }}\|{{ lit['null'] }}\|{{ lit.comment 'a*/b' }}\|{{ lit.identifier 'class' }} ` | ` {"model":{"integer":9223372036854775807,"fraction":1.25}} ` | ` "\u0022safe\u0022\|9223372036854775807\|1.25\|no\|nil\|a* /b\|@class" ` |

## lit.decimal

` (number) -> text ` — since v1.

Delegates decimal number spelling to the language pack.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` literal-seam ` | ` {{ lit.string 'safe' }}\|{{ lit.integer model.integer }}\|{{ lit.decimal model.fraction }}\|{{ lit.boolean false }}\|{{ lit['null'] }}\|{{ lit.comment 'a*/b' }}\|{{ lit.identifier 'class' }} ` | ` {"model":{"integer":9223372036854775807,"fraction":1.25}} ` | ` "\u0022safe\u0022\|9223372036854775807\|1.25\|no\|nil\|a* /b\|@class" ` |

## lit.identifier

` (string) -> text ` — since v1.

Delegates safe identifier spelling to the language pack.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` literal-seam ` | ` {{ lit.string 'safe' }}\|{{ lit.integer model.integer }}\|{{ lit.decimal model.fraction }}\|{{ lit.boolean false }}\|{{ lit['null'] }}\|{{ lit.comment 'a*/b' }}\|{{ lit.identifier 'class' }} ` | ` {"model":{"integer":9223372036854775807,"fraction":1.25}} ` | ` "\u0022safe\u0022\|9223372036854775807\|1.25\|no\|nil\|a* /b\|@class" ` |

## lit.integer

` (int64) -> text ` — since v1.

Delegates signed 64-bit integer spelling to the language pack without string coercion.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` literal-seam ` | ` {{ lit.string 'safe' }}\|{{ lit.integer model.integer }}\|{{ lit.decimal model.fraction }}\|{{ lit.boolean false }}\|{{ lit['null'] }}\|{{ lit.comment 'a*/b' }}\|{{ lit.identifier 'class' }} ` | ` {"model":{"integer":9223372036854775807,"fraction":1.25}} ` | ` "\u0022safe\u0022\|9223372036854775807\|1.25\|no\|nil\|a* /b\|@class" ` |
| ` strict-literal ` | ` {{ lit.integer model.number }} ` | ` {"model":{"number":"1"}} ` | ` render refusal: STAGE-TPL-024 ` |

## lit.null

` () -> text; write lit['null'] in Scriban ` — since v1.

Delegates null spelling to the language pack. Use `lit['null']` rather than `lit.null`: Scriban reserves the null keyword.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` literal-seam ` | ` {{ lit.string 'safe' }}\|{{ lit.integer model.integer }}\|{{ lit.decimal model.fraction }}\|{{ lit.boolean false }}\|{{ lit['null'] }}\|{{ lit.comment 'a*/b' }}\|{{ lit.identifier 'class' }} ` | ` {"model":{"integer":9223372036854775807,"fraction":1.25}} ` | ` "\u0022safe\u0022\|9223372036854775807\|1.25\|no\|nil\|a* /b\|@class" ` |

## lit.string

` (string) -> text ` — since v1.

Delegates string escaping to the language pack without primitive coercion.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` literal-seam ` | ` {{ lit.string 'safe' }}\|{{ lit.integer model.integer }}\|{{ lit.decimal model.fraction }}\|{{ lit.boolean false }}\|{{ lit['null'] }}\|{{ lit.comment 'a*/b' }}\|{{ lit.identifier 'class' }} ` | ` {"model":{"integer":9223372036854775807,"fraction":1.25}} ` | ` "\u0022safe\u0022\|9223372036854775807\|1.25\|no\|nil\|a* /b\|@class" ` |

## name.camel

` (nameWords) -> text ` — since v1.

Lowercases the entire first word and capitalizes subsequent words' first Unicode scalar invariantly.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` word-arrays ` | ` {{ name.words model.words }}\|{{ name.pascal model.words }}\|{{ name.camel model.words }}\|{{ name.snake model.words }}\|{{ name.kebab model.words }}\|{{ name.constant model.words }} ` | ` {"model":{"words":["order","iD","eMail"]}} ` | ` "order id email\|OrderIDEMail\|orderIDEMail\|order_id_email\|order-id-email\|ORDER_ID_EMAIL" ` |

## name.constant

` (nameWords) -> text ` — since v1.

Joins invariant uppercase words with underscores.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` word-arrays ` | ` {{ name.words model.words }}\|{{ name.pascal model.words }}\|{{ name.camel model.words }}\|{{ name.snake model.words }}\|{{ name.kebab model.words }}\|{{ name.constant model.words }} ` | ` {"model":{"words":["order","iD","eMail"]}} ` | ` "order id email\|OrderIDEMail\|orderIDEMail\|order_id_email\|order-id-email\|ORDER_ID_EMAIL" ` |

## name.kebab

` (nameWords) -> text ` — since v1.

Joins invariant lowercase words with hyphens.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` word-arrays ` | ` {{ name.words model.words }}\|{{ name.pascal model.words }}\|{{ name.camel model.words }}\|{{ name.snake model.words }}\|{{ name.kebab model.words }}\|{{ name.constant model.words }} ` | ` {"model":{"words":["order","iD","eMail"]}} ` | ` "order id email\|OrderIDEMail\|orderIDEMail\|order_id_email\|order-id-email\|ORDER_ID_EMAIL" ` |

## name.pascal

` (nameWords) -> text ` — since v1.

Capitalizes each word's first Unicode scalar invariantly, preserving the remaining characters.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` word-arrays ` | ` {{ name.words model.words }}\|{{ name.pascal model.words }}\|{{ name.camel model.words }}\|{{ name.snake model.words }}\|{{ name.kebab model.words }}\|{{ name.constant model.words }} ` | ` {"model":{"words":["order","iD","eMail"]}} ` | ` "order id email\|OrderIDEMail\|orderIDEMail\|order_id_email\|order-id-email\|ORDER_ID_EMAIL" ` |
| ` unicode-scalar ` | ` {{ name.pascal model.words }}\|{{ name.snake model.words }} ` | ` {"model":{"words":["𐐨tail","I"]}} ` | ` "\uD801\uDC00tailI\|\uD801\uDC28tail_i" ` |
| ` strict-name ` | ` {{ name.pascal model.name }} ` | ` {"model":{"name":"raw text"}} ` | ` render refusal: STAGE-TPL-024 ` |

## name.snake

` (nameWords) -> text ` — since v1.

Joins invariant lowercase words with underscores.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` word-arrays ` | ` {{ name.words model.words }}\|{{ name.pascal model.words }}\|{{ name.camel model.words }}\|{{ name.snake model.words }}\|{{ name.kebab model.words }}\|{{ name.constant model.words }} ` | ` {"model":{"words":["order","iD","eMail"]}} ` | ` "order id email\|OrderIDEMail\|orderIDEMail\|order_id_email\|order-id-email\|ORDER_ID_EMAIL" ` |
| ` unicode-scalar ` | ` {{ name.pascal model.words }}\|{{ name.snake model.words }} ` | ` {"model":{"words":["𐐨tail","I"]}} ` | ` "\uD801\uDC00tailI\|\uD801\uDC28tail_i" ` |

## name.words

` (nameWords) -> text ` — since v1.

Joins nonempty name words with spaces using invariant lowercase.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` word-arrays ` | ` {{ name.words model.words }}\|{{ name.pascal model.words }}\|{{ name.camel model.words }}\|{{ name.snake model.words }}\|{{ name.kebab model.words }}\|{{ name.constant model.words }} ` | ` {"model":{"words":["order","iD","eMail"]}} ` | ` "order id email\|OrderIDEMail\|orderIDEMail\|order_id_email\|order-id-email\|ORDER_ID_EMAIL" ` |

## refuse

` (literalCategory, message) -> whole-plan refusal ` — since v1.

Latches a safety refusal for a literal trust, integrity or budget category. The caller must discard all plan outputs.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` refuse-trust ` | ` {{ refuse 'trust' 'unsafe' }} ` | ` {"model":{}} ` | ` render refusal: STAGE-TPL-040 ` |
| ` no-unknown-refusal ` | ` {{ refuse 'anything' 'unsafe' }} ` | ` {"model":{}} ` | ` prepare refusal: STAGE-TPL-008 ` |
| ` no-clock ` | ` {{ date.now }} ` | ` {"model":{}} ` | ` prepare refusal: STAGE-TPL-005 ` |

## render

` (literalPartial, arg = null) -> partial output ` — since v1.

Renders a declared, literal partial with an isolated item argument.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` render-partial (operators) ` | ` {{ render 'leaf' model }} ` | ` {"model":{"identifier":"Customer"}} ` | ` ["Reference:Domain.Customer"] ` |
| ` no-dynamic-partial ` | ` {{ render model.partial }} ` | ` {"model":{}} ` | ` prepare refusal: STAGE-TPL-006 ` |
| ` ordinary-text ` | ` hello {{ model.name }} ` | ` {"model":{"name":"World"}} ` | ` "hello World" ` |

## section

` wrap section 'name' ... end -> inline body ` — since v1.

Renders an inline section body unless a validated override replaces it. Sections cannot be nested.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` default-section ` | ` a{{ wrap section 'body' }}{{ model.name }}{{ end }}b ` | ` {"model":{"name":"x"}} ` | ` "axb" ` |
| ` no-nested-section ` | ` {{ wrap section 'body' }}{{ wrap section 'inner' }}x{{ end }}{{ end }} ` | ` {"model":{}} ` | ` prepare refusal: STAGE-TPL-008 ` |

## sym.decl

` (namespace, name) -> declaration ` — since v1.

Emits a structured declaration handle; handles cannot be converted to text or captured.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` symbols ` | ` A{{ sym.decl 'Domain' 'Customer' }}{{ sym.ref 'Other' 'Customer' }}{{ sym.imports }}B ` | ` {"model":{}} ` | ` ["text:A","Declaration:Domain.Customer","Reference:Other.Customer","Imports:","text:B"] ` |

## sym.imports

` () -> imports insertion point ` — since v1.

Emits a structured imports insertion point for the language pack.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` symbols ` | ` A{{ sym.decl 'Domain' 'Customer' }}{{ sym.ref 'Other' 'Customer' }}{{ sym.imports }}B ` | ` {"model":{}} ` | ` ["text:A","Declaration:Domain.Customer","Reference:Other.Customer","Imports:","text:B"] ` |

## sym.ref

` (namespace, name) -> reference ` — since v1.

Emits a structured reference handle, preserving namespace and name separately.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` symbols ` | ` A{{ sym.decl 'Domain' 'Customer' }}{{ sym.ref 'Other' 'Customer' }}{{ sym.imports }}B ` | ` {"model":{}} ` | ` ["text:A","Declaration:Domain.Customer","Reference:Other.Customer","Imports:","text:B"] ` |
| ` symbol-null-test ` | ` {{ x = sym.ref 'Domain' 'Customer'; if x != null; x; end }} ` | ` {"model":{}} ` | ` ["Reference:Domain.Customer"] ` |
| ` symbol-text ` | ` {{ (sym.ref 'Domain' 'Customer') + 'text' }} ` | ` {"model":{}} ` | ` render refusal: STAGE-TPL-022 ` |

## type.ref

` (RenderTypeRef) -> type parts ` — since v1.

Resolves semantic identities and delegates primitive, collection and optional spelling to the language pack.

| Vector | Template | Scope | Expected output |
| --- | --- | --- | --- |
| ` type-text ` | ` {{ type.ref model.type }} ` | ` {"model":{"type":{"kind":"primitive","primitive":"text","isCollection":false,"isOptional":false}}} ` | ` "string" ` |
| ` type-symbol ` | ` {{ type.ref model.type }} ` | ` {"model":{"type":{"kind":"primitive","primitive":"uuid","isCollection":true,"isOptional":true}}} ` | ` ["Reference:System.Guid","text:[]?"] ` |
| ` type-identity ` | ` {{ type.ref model.type }} ` | ` {"model":{"type":{"kind":"concept","target":"customer","isCollection":false,"isOptional":false}}} ` | ` ["Reference:Domain.Customer"] ` |
| ` unknown-type ` | ` {{ type.ref model.type }} ` | ` {"model":{"type":{"kind":"concept","target":"missing","isCollection":false,"isOptional":false}}} ` | ` render refusal: STAGE-TPL-024 ` |

## Safety refusal codes

All STAGE-TPL codes are whole-plan safety refusals. Discard every output of a refused plan, including earlier artifacts.

| Code | TemplateCodes constant |
| --- | --- |
| ` STAGE-TPL-001 ` | ` MissingContract ` |
| ` STAGE-TPL-002 ` | ` TemplateSize ` |
| ` STAGE-TPL-003 ` | ` Nesting ` |
| ` STAGE-TPL-004 ` | ` Parse ` |
| ` STAGE-TPL-005 ` | ` Allowlist ` |
| ` STAGE-TPL-006 ` | ` Partial ` |
| ` STAGE-TPL-007 ` | ` TemplatePrivateUse ` |
| ` STAGE-TPL-008 ` | ` Helper ` |
| ` STAGE-TPL-009 ` | ` OperatorTable ` |
| ` STAGE-TPL-010 ` | ` Override ` |
| ` STAGE-TPL-020 ` | ` HostValue ` |
| ` STAGE-TPL-021 ` | ` Mutation ` |
| ` STAGE-TPL-022 ` | ` Symbol ` |
| ` STAGE-TPL-023 ` | ` JsonPrivateUse ` |
| ` STAGE-TPL-024 ` | ` Input ` |
| ` STAGE-TPL-030 ` | ` Steps ` |
| ` STAGE-TPL-031 ` | ` Output ` |
| ` STAGE-TPL-032 ` | ` Artifacts ` |
| ` STAGE-TPL-033 ` | ` FanOut ` |
| ` STAGE-TPL-034 ` | ` StringLength ` |
| ` STAGE-TPL-035 ` | ` Recursion ` |
| ` STAGE-TPL-036 ` | ` Deadline ` |
| ` STAGE-TPL-040 ` | ` Refuse ` |
| ` STAGE-TPL-041 ` | ` AlreadyRefused ` |

## Allowed standard library

- ` array `: ` add `, ` add_range `, ` any `, ` compact `, ` concat `, ` contains `, ` cycle `, ` each `, ` filter `, ` first `, ` join `, ` last `, ` limit `, ` map `, ` offset `, ` remove_at `, ` reverse `, ` size `, ` sort `, ` uniq `.
- ` math `: ` abs `, ` ceil `, ` divided_by `, ` floor `, ` is_number `, ` minus `, ` modulo `, ` plus `, ` round `, ` times `.
- ` object `: ` default `, ` has_key `, ` has_value `, ` keys `, ` kind `, ` size `, ` typeof `, ` values `.
- ` string `: ` append `, ` capitalize `, ` capitalizewords `, ` contains `, ` downcase `, ` empty `, ` ends_with `, ` equals_ignore_case `, ` escape `, ` handleize `, ` index_of `, ` literal `, ` lstrip `, ` pad_left `, ` pad_right `, ` pluralize `, ` prepend `, ` remove `, ` remove_first `, ` remove_last `, ` replace `, ` replace_first `, ` rstrip `, ` size `, ` slice `, ` slice1 `, ` split `, ` starts_with `, ` strip `, ` to_double `, ` to_float `, ` to_int `, ` to_long `, ` truncate `, ` upcase `, ` whitespace `.

Everything else is unavailable, including date/time, random, environment, regex, dynamic evaluation and JSON parsing.

## Budget defaults

Limits are aggregate across a plan session; all budget values must be positive.

| Limit | Default |
| --- | --- |
| Template UTF-8 bytes (pre-scan) | 65536 |
| Template nesting (pre-scan) | 32 |
| Aggregate steps | 2000000 |
| Aggregate output UTF-8 bytes | 8388608 |
| Artifacts | 2000 |
| Fan-out | 2000 |
| String length | 262144 |
| Recursion | 64 |
| Deadline (seconds) | 10 |

The caller owns the process-level CPU and memory sandbox; interpreter budgets are not a replacement.
