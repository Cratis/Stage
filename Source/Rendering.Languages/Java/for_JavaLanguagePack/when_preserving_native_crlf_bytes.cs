// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_preserving_native_crlf_bytes : given.a_resolved_plan
{
    ResolvedSymbolFile _file = null!;

    void Because() => _file = JavaLanguagePack.Resolve(Plan(
        new TextSegment("  public "),
        new NameSegment(Widget, "widget"),
        new TextSegment("() {}\r\n"),
        new NativeSegment("java", "  void run() {\r\n    ", "widget", "member", []),
        new ReferenceSegment(List, LexicalOwner: "widget"),
        new NativeSegment("java", "<", "widget", "type", []),
        new ReferenceSegment(StringType, LexicalOwner: "widget"),
        new NativeSegment("java", "> values;\r\n  }\r\n", "widget", "member", []),
        new ReferenceSegment(StringType, LexicalOwner: "widget"),
        new TextSegment(" name;\r\n"))).Files.Single();

    [Fact]
    void should_preserve_exact_mixed_newline_bytes_around_substitutions() => Encoding.UTF8.GetBytes(_file.Text).ShouldEqual(Encoding.UTF8.GetBytes(
        "package example;\nimport java.util.List;\npublic class Widget {\n  public Widget() {}\n  void run() {\r\n    List<String> values;\r\n  }\r\nString name;\n}\n"));

    [Fact] void should_return_the_allocated_primary_type() => _file.PrimaryType!.QualifiedName.ShouldEqual("example.Widget");
    [Fact] void should_return_the_stable_file_identity() => _file.Id.ShouldEqual("widget");
}
