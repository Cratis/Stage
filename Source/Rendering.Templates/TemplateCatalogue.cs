// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplateCatalogue
{
    internal const string Signatures = """
        name.{words,pascal,camel,snake,kebab,constant}(nonempty-string-array)->text;no-tokenization;invariant-runes;pascal/camel-preserve-tail
        lit.string(string)->text
        lit.integer(int64)->text
        lit.decimal(int64|decimal)->text
        lit.boolean(bool)->text
        lit.null()->text
        lit.comment(string)->text
        lit.identifier(string)->text
        sym.decl(namespace:string,name:string)->opaque-declaration
        sym.ref(namespace:string,name:string)->opaque-reference
        sym.imports()->opaque-imports
        type.ref({kind:primitive|concept|composite,primitive?:string,target?:semantic-id,isCollection:bool,isOptional:bool})->parts;collection-before-optional
        expr.supported(expression:$kind,table:literal-declared-name)->bool
        expr.lower(expression:$kind,table:literal-declared-name,leaf:literal-partial,role:literal-gap-role)->segments
        render(name:literal-partial,arg?:json)->segments;shared-meter
        wrap section(name:literal-lowercase-kebab-1..64)->inline-body;unique-per-template;not-nested;overrides-body-only
        gap(role:gap-role,intent:single-line-1..512,modelPath:max1024,code:002|003|005=002)->placeholder+gap
        refuse(category:trust|integrity|budget|authorization|event_stream,message:string)->whole-plan-refusal
        operators:{open:single-line,close:single-line,operators:{and,or,equal,notEqual,greaterThan,greaterThanOrEqual,lessThan,lessThanOrEqual:{text:single-line,precedence:1..100}}};exact;unique;max65536
        expressions:$kind=comparison|logical|value|resolved|eventContext|refusal|readPath;unknown=STAGE-DEGRADE-001;right-equal-parenthesized
        values:$kind=text|number|boolean|null|array|composite;structured-leaves-delegate
        opaque:assignment/null-test/body-only;no-capture/container/text-coercion/member-access
        scope:immutable-json;depth64;int64-or-decimal;no-private-use;no-duplicate-keys
        paths:text-only;no-symbols-or-gaps
        AST:no-import/with/alias/computed-functions;reserved-roots-readonly;locals-and-functions-allowed
        culture:invariant;comparison:ordinal;no-clock/random/eval/json/regex/host-members
        refusal:latches-plan;outputs-null;subsequent-already-refused;gaps-not-refusals
        """;

    internal static readonly IReadOnlyDictionary<string, string[]> Helpers = HelperCatalogue.All.Where(entry => entry.Name.Contains('.', StringComparison.Ordinal)).GroupBy(entry => entry.Name[..entry.Name.IndexOf('.', StringComparison.Ordinal)], StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Select(entry => entry.Name[(entry.Name.IndexOf('.', StringComparison.Ordinal) + 1)..]).ToArray(), StringComparer.Ordinal);
}
