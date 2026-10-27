// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_resolution_cannot_bind : given.a_resolved_plan
{
    [Fact]
    void should_refuse_a_missing_reference_target()
    {
        var error = Catch.Exception(() => JavaLanguagePack.Resolve(Plan(new ReferenceSegment(new("missing", "Type", []), LexicalOwner: "widget"))));
        ((LanguagePackRefusal)error).Code.ShouldEqual(JavaRefusalCodes.MissingDeclaration);
    }

    [Fact]
    void should_refuse_an_ambiguous_reference_target()
    {
        var plan = Plan(new ReferenceSegment(Widget, LexicalOwner: "widget"));
        plan = plan with { Files = [.. plan.Files, new("other", "example", [new DeclarationSegment(Widget, "other") { Supertypes = KnownTypes<TypeName>.None }])] };
        var error = Catch.Exception(() => JavaLanguagePack.Resolve(plan));
        ((LanguagePackRefusal)error).Code.ShouldEqual(JavaRefusalCodes.AmbiguousDeclaration);
    }

    [Fact]
    void should_refuse_an_obscured_target_rather_than_emit_a_misbinding()
    {
        var target = new TypeName("com.library", "Widget", []);
        var plan = Plan(new TextSegment("int com; int Widget;\n"), new ReferenceSegment(target, LexicalOwner: "widget")) with { ExternalTypes = [target] };
        var error = Catch.Exception(() => JavaLanguagePack.Resolve(plan));
        ((LanguagePackRefusal)error).Code.ShouldEqual(JavaRefusalCodes.ObscuredReference);
    }
}
