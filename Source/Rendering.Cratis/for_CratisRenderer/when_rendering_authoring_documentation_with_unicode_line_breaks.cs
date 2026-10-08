// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_authoring_documentation_with_unicode_line_breaks : Specification
{
    string _result = null!;

    void Because()
    {
        var compilation = new ScreenplayCompiler().Compile(for_CratisArtifactRenderPlanner.when_rendering_authoring_documentation.Source);
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics));
        var application = compilation.Value!;
        var set = new ApplicationSet([application]);
        var @event = application.Modules.Single().Features.Single().Slices.First().Events.Single() with
        {
            Description = "Summary\u0085public class NextLine;\u2028public class LineSeparator;\u2029public class ParagraphSeparator;",
            Documentation = "Remarks\u0085public class NextLine;\u2028public class LineSeparator;\u2029public class ParagraphSeparator;"
        };
        var builder = new CSharpCodeBuilder();
        EventRenderer.Render(builder, @event, set, []);
        _result = builder.ToString();
    }

    [Fact] void should_keep_next_line_text_inside_a_comment() => _result.ShouldContain("/// Summary\n/// public class NextLine;\n");
    [Fact] void should_keep_line_separator_text_inside_a_comment() => _result.ShouldContain("/// public class NextLine;\n/// public class LineSeparator;\n");
    [Fact] void should_keep_paragraph_separator_text_inside_a_comment() => _result.ShouldContain("/// public class LineSeparator;\n/// public class ParagraphSeparator;\n/// </summary>");
    [Fact] void should_keep_remarks_text_inside_comments() => _result.ShouldContain("/// Remarks\n/// public class NextLine;\n/// public class LineSeparator;\n/// public class ParagraphSeparator;\n/// </remarks>");
}
