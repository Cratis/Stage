// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_resolution_is_repeated : given.a_resolved_plan
{
    SymbolPlan _plan = null!;
    LanguagePackResolution _first = null!;
    LanguagePackResolution _second = null!;
    LanguagePackResolution _turkish = null!;

    void Establish() => _plan = Plan(new ReferenceSegment(List, LexicalOwner: "widget"), new TextSegment(" values;\n"));

    void Because()
    {
        _first = JavaLanguagePack.Resolve(_plan);
        _second = JavaLanguagePack.Resolve(_plan);
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            _turkish = JavaLanguagePack.Resolve(_plan);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact] void should_allocate_fresh_resolvers_without_changing_bytes() => _second.Files.ShouldEqual(_first.Files);
    [Fact] void should_be_independent_of_turkish_culture() => _turkish.Files.ShouldEqual(_first.Files);
    [Fact] void should_not_report_gaps_for_supported_segments() => _first.Diagnostics.Count.ShouldEqual(0);
    [Fact] void should_leave_no_structured_segment_markers() => _first.Files.Single().Text.ShouldEqual("package example;\nimport java.util.List;\npublic class Widget {\nList values;\n}\n");
}
