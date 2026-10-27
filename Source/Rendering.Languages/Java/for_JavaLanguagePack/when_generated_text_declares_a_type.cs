// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_generated_text_declares_a_type : given.a_symbol_plan
{
    [Theory]
    [InlineData("class Hidden {}")]
    [InlineData("interface Hidden {}")]
    [InlineData("enum Hidden { A }")]
    [InlineData("record Hidden(int value) {}")]
    [InlineData("@interface Hidden {}")]
    [InlineData("cl\\u0061ss Hidden {}")]
    [InlineData("class H\\u0000idden {}")]
    [InlineData("class /* comment */ Hidden {}")]
    void should_refuse_unstructured_type_declarations(string text)
    {
        var error = Catch.Exception(() => JavaLanguagePack.Validate(WithSegment(new TextSegment(text))));
        ((LanguagePackRefusal)error).Code.ShouldEqual(JavaRefusalCodes.UnstructuredDeclaration);
    }

    [Fact]
    void should_keep_lexical_state_across_text_segments()
    {
        var error = Catch.Exception(() => JavaLanguagePack.Validate(WithSegments(new TextSegment("cl"), new TextSegment("ass Hidden {}"))));
        ((LanguagePackRefusal)error).Code.ShouldEqual(JavaRefusalCodes.UnstructuredDeclaration);
    }

    [Fact]
    void should_not_accept_a_name_segment_in_place_of_a_declaration()
    {
        var error = Catch.Exception(() => JavaLanguagePack.Validate(WithSegments(new TextSegment("class "), new NameSegment(KnownType, "owner"), new TextSegment(" {}"))));
        ((LanguagePackRefusal)error).Code.ShouldEqual(JavaRefusalCodes.UnstructuredDeclaration);
    }

    [Theory]
    [InlineData("// class Hidden {}\n")]
    [InlineData("/* interface Hidden {} */")]
    [InlineData("String s = \"enum Hidden {}\";")]
    [InlineData("String s = \"\"\"\nrecord Hidden() {}\n\"\"\";")]
    [InlineData("Class<?> type = Widget.class;")]
    [InlineData("void record() {} int record = 1;")]
    void should_ignore_non_declarations(string text) => Catch.Exception(() => JavaLanguagePack.Validate(WithSegments(new TextSegment(text)))).ShouldBeNull();

    [Fact]
    void should_accept_a_structured_declaration() => Catch.Exception(() => JavaLanguagePack.Validate(WithSegments(
        new TextSegment("class "), new DeclarationSegment(new("example", "Widget", []), "owner") { Supertypes = KnownTypes<TypeName>.None }, new TextSegment(" {}")))).ShouldBeNull();

    [Fact]
    void should_allow_native_type_declarations() => Catch.Exception(() => JavaLanguagePack.Validate(WithSegments(new NativeSegment("java", "class Hidden {}", "owner", "member", [])))).ShouldBeNull();
}
