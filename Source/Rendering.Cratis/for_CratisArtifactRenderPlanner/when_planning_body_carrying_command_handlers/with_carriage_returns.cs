// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;

public class with_carriage_returns
{
    const string Return = "return new OrderPlaced(text);";

    [Fact]
    public void should_publish_lf_only_bytes_when_crlf_is_outside_literals()
    {
        var body = string.Join("\r\n", "// first", "var text = \"a\" +", "    \"b\";", "/* block", "comment */", Return);
        var plan = handler_model.Plan(handler_model.Request(body));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.All(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)), artifact => Assert.DoesNotContain((byte)'\r', artifact.Bytes.ToArray()));
    }

    [Theory]
    [InlineData("var text = @\"a\r\nb\";")]
    [InlineData("var text = \"\"\"\r\n    a\r\n    \"\"\";")]
    [InlineData("var text = $@\"a\r\n{1}\";")]
    [InlineData("var text = $\"\"\"\r\n    a{1}\r\n    \"\"\";")]
    public void should_refuse_a_carriage_return_inside_a_literal(string statement)
    {
        var plan = handler_model.Plan(handler_model.Request(statement + "\n" + Return));
        var diagnostic = Assert.Single(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022");
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("literal", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("PlaceOrder", diagnostic.Message, StringComparison.Ordinal);
        Assert.Empty(plan.Artifacts);
    }

    [Theory]
    [InlineData("var text = \"a\rb\";")]
    [InlineData("var text = '\r'.ToString();")]
    public void should_still_block_a_carriage_return_the_lexer_cannot_contain(string statement)
    {
        // A raw CR in a regular string or char literal is not valid C#; containment refuses it first.
        var plan = handler_model.Plan(handler_model.Request(statement + "\n" + Return));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_keep_escaped_carriage_return_sequences_in_literals()
    {
        const string body = "var text = \"a\\r\\nb\" + '\\r'.ToString();\n" + Return;
        var plan = handler_model.Plan(handler_model.Request(body));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
    }
}
