// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_checking_final_file_structure : given.a_resolved_plan
{
    [Fact]
    void should_not_shorten_inside_an_anonymous_class_body()
    {
        TypeName baseType = new("library", "Base", []);
        var plan = Plan(new TextSegment("Object value = new library.Base() { "), new ReferenceSegment(StringType, LexicalOwner: "widget"), new TextSegment(" field; };\n")) with
        {
            ExternalDeclarations = [new(baseType, KnownTypes<string>.Of("String")) { Supertypes = KnownTypes<TypeName>.None }],
        };
        JavaLanguagePack.Resolve(plan).Files.Single().Text.Contains("java.lang.String field", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_declaration_split_between_native_and_generated_tokens()
    {
        var plan = Plan(new NativeSegment("java", "cl", "widget", "member", []), new TextSegment("ass Hidden {}\n"));
        var error = Catch.Exception(() => JavaLanguagePack.Resolve(plan));
        ((LanguagePackRefusal)error).Code.ShouldEqual(JavaRefusalCodes.UnstructuredDeclaration);
    }

    [Fact]
    void should_preserve_a_unicode_separator_inside_a_java_line_comment()
    {
        var plan = Plan(new TextSegment("// note\u2028class Evil extends library.Base {}\n"));
        JavaLanguagePack.Resolve(plan).Files.Single().Text.Contains("// note\u2028class Evil", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    void should_not_treat_record_instanceof_as_a_record_declaration()
    {
        var plan = Plan(new TextSegment("void check(Object record) { if (record instanceof String) {} }\n"));
        Catch.Exception(() => JavaLanguagePack.Resolve(plan)).ShouldBeNull();
    }

    [Fact]
    void should_preserve_a_wholly_native_declaration()
    {
        var plan = new SymbolPlan([new("native", "", [new NativeSegment("java", "class Widget {}", "native", "member", [])])], []);
        JavaLanguagePack.Resolve(plan).Files.Single().Text.ShouldEqual("class Widget {}");
    }
}
