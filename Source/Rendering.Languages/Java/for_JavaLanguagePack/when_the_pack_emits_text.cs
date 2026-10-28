// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_the_pack_emits_text : Specification
{
    readonly JavaLanguagePack _pack = new();
    string[] _emitted = [];
    TypeSpelling[] _types = [];

    void Because()
    {
        _emitted =
        [
            _pack.String("a"), _pack.Integer(5), _pack.Decimal(1.5m), _pack.Boolean(true), _pack.Null(), _pack.Comment("c"), _pack.Identifier("x"),
            .. Enum.GetValues<GapRole>().Select(role => _pack.Placeholder(role, "C", "i"))
        ];
        _types = [.. new[] { "text", "wholeNumber", "decimalNumber", "boolean", "uuid", "date", "dateTime" }.Select(_pack.Primitive)];
    }

    static IEnumerable<string> Constructed(string text)
    {
        var index = text.IndexOf("new ", StringComparison.Ordinal);
        while (index >= 0)
        {
            var start = index + 4;
            yield return text[start..text.IndexOf('(', start)];
            index = text.IndexOf("new ", start, StringComparison.Ordinal);
        }
    }

    [Fact] void should_write_the_pack_identity() => _pack.Pack.ShouldEqual(new Cratis.Stage.Rendering.Kits.LanguagePackVersion("java", "1.0.0-prerelease"));
    [Fact] void should_suffix_integers_with_long() => _pack.Integer(-5).ShouldEqual("-5L");
    [Fact] void should_write_decimals_as_big_decimal_strings() => _pack.Decimal(1.50m).ShouldEqual("new java.math.BigDecimal(\"1.50\")");
    [Fact] void should_throw_for_a_throwing_operation_body() => _pack.Placeholder(GapRole.ThrowingOperationBody, "C", "i").ShouldEqual("throw new java.lang.UnsupportedOperationException(\"C: i\");");
    [Fact] void should_comment_every_other_role() => _pack.Placeholder(GapRole.DisabledSpecification, "C", "i").ShouldEqual("/* TODO C: i */");
    [Fact] void should_write_only_java_rooted_constructed_types() => _emitted.SelectMany(Constructed).ShouldEachConformTo(name => name.StartsWith("java.", StringComparison.Ordinal));
    [Fact] void should_write_no_new_expression_outside_decimals_and_throwing_placeholders() => _emitted.Count(text => text.Contains("new ", StringComparison.Ordinal)).ShouldEqual(3);
    [Fact] void should_spell_primitives_as_structured_references() => _types.ShouldEachConformTo(spelling => spelling.Parts.Length == 1 && spelling.Parts[0] is TemplateSymbolSegment);
    [Fact] void should_spell_collections_with_a_structured_list_and_only_punctuation_as_text() => _pack.Collection(_types[0]).Parts.OfType<TemplateTextSegment>().ShouldEachConformTo(part => part.Text == "<" || part.Text == ">");
    [Fact] void should_leave_optional_unchanged() => _pack.Optional(_types[0]).ShouldEqual(_types[0]);
    [Fact] void should_refuse_an_unsupported_primitive() => Catch.Exception(() => _pack.Primitive("blob")).ShouldBeOfExactType<TemplateLanguageRefusal>();
    [Fact] void should_not_resolve_semantic_ids() => _pack.TryGet("anything", out _).ShouldBeFalse();
}
