// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_resolving_semantic_gaps : given.a_resolved_plan
{
    LanguagePackResolution _result = null!;

    void Because() => _result = JavaLanguagePack.Resolve(Plan(
        new GapSegment("STAGE-DEGRADE-EXISTING", "collection", "retain the value", "type"),
        new TextSegment(" value;\n"),
        new GapSegment("STAGE-DEGRADE-EXISTING", "handler", "retain the action", "member"),
        new TextSegment("void run() {\n"),
        new GapSegment("STAGE-DEGRADE-EXISTING", "call", "perform it", "statement"),
        new TextSegment("\n}\n")));

    [Fact]
    void should_render_only_supported_placeholders() => _result.Files.Single().Text.ShouldEqual(
        "package example;\npublic class Widget {\n// TODO STAGE-DEGRADE-EXISTING: collection: retain the value\njava.lang.Object value;\n" +
        "// TODO STAGE-DEGRADE-EXISTING: handler: retain the action\nvoid run() {\n// TODO STAGE-DEGRADE-EXISTING: call: perform it\n" +
        "throw new java.lang.UnsupportedOperationException(\"STAGE-DEGRADE-EXISTING: call: perform it\");\n}\n}\n");

    [Fact] void should_preserve_the_planner_owned_code() => _result.Diagnostics.All(diagnostic => diagnostic.Code == "STAGE-DEGRADE-EXISTING").ShouldBeTrue();
    [Fact] void should_order_diagnostics_by_segment() => _result.Diagnostics.Select(diagnostic => diagnostic.Segment).ShouldEqual([5, 7, 9]);

    [Theory]
    [InlineData("expression")]
    [InlineData("annotation")]
    [InlineData("import")]
    void should_refuse_other_contexts(string context)
    {
        var error = Catch.Exception(() => JavaLanguagePack.Resolve(Plan(new GapSegment("STAGE-DEGRADE-EXISTING", "call", "intent", context))));
        ((LanguagePackRefusal)error).Code.ShouldEqual(JavaRefusalCodes.UnsupportedContext);
    }
}
