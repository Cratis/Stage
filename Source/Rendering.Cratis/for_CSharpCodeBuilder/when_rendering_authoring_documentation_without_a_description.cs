// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_rendering_authoring_documentation_without_a_description : Specification
{
    string _result = null!;

    void Because() => _result = new CSharpCodeBuilder()
        .Documentation(null, "# Notes <item> & details")
        .Line("public record Example;")
        .ToString();

    [Fact] void should_emit_escaped_remarks() => _result.ShouldContain("/// <remarks>\n/// # Notes &lt;item&gt; &amp; details\n/// </remarks>\npublic record Example;");
    [Fact] void should_not_invent_a_summary() => _result.ShouldNotContain("<summary>");
}
