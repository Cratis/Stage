// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_validating_a_structured_plan : given.a_symbol_plan
{
    SymbolPlan _plan = null!;
    NativeSegment _native = null!;
    Exception? _error;

    void Establish()
    {
        _native = new("java", "void run() {\r\n  String s = \"original\";\r\n}\r\n", "owner", "member", [KnownType]);
        SymbolSegment[] segments =
        [
            new TextSegment("package example;\n"),
            new DeclarationSegment(KnownType, "owner", "scope", "parent"),
            new NameSegment(KnownType, "owner"),
            new ReferenceSegment(KnownType, "owner", "lexical"),
            new ImportsSegment(),
            _native,
            _native with { Context = "type" },
            _native with { Context = "statement" },
            new GapSegment("existing-code", "construct", "intent", "type"),
            new GapSegment("existing-code", "construct", "intent", "member"),
            new GapSegment("existing-code", "construct", "intent", "statement"),
        ];
        _plan = new([new("file", "example", segments)], [new("java.lang", "String", [])]);
    }

    void Because() => _error = Catch.Exception(() => JavaLanguagePack.Validate(_plan));

    [Fact] void should_accept_all_seven_segment_kinds_and_all_v1_contexts() => _error.ShouldBeNull();
    [Fact] void should_leave_the_authored_crlf_bytes_unchanged() => _native.Body.ShouldEqual("void run() {\r\n  String s = \"original\";\r\n}\r\n");
    [Fact] void should_preserve_generated_lf_text() => ((TextSegment)_plan.Files[0].Segments[0]).Text.ShouldEqual("package example;\n");
}
