// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTypeSystem;

public class when_rendering_escaped_text_values : a_register_project_render_request
{
    [Theory]
    [InlineData("line\n\"quoted\"\\tail")]
    [InlineData("line\nreturn\rcell\ttail")]
    [InlineData("separator\u2028paragraph\u2029end")]
    [InlineData("non-BMP \U0001D11E")]
    public void should_compile_and_round_trip_every_text_character(string value)
    {
        var types = new SemanticTypeSystem(new SemanticApplicationContext(_request, _options));
        var literal = types.Value(SemanticValue.Text(value), SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        var code = $"public static class TextProbe {{ public static string Value() => {literal}; }}";
        var files = new[] { new RenderedFile("TextProbe.cs", code) };
        RenderedOutput.Errors(files).ShouldBeEmpty();
        var assembly = RenderedOutput.Load(files);
        assembly.GetType("TextProbe")!.GetMethod("Value")!.Invoke(null, null).ShouldEqual(value);
        literal.ShouldEqual(CSharpCodeBuilder.StringLiteral(value));
    }

    [Fact]
    public void should_round_trip_an_escaped_command_specification_value()
    {
        const string value = "line\n\"quoted\"\\tail";
        var context = new SemanticApplicationContext(_request, _options);
        var specification = _registerProject.Specifications.First(specification => specification.When is not null);
        var property = context.Commands[specification.When!.Command].Properties.Single(property => property.Name == "name");
        specification = specification with
        {
            When = specification.When with
            {
                Values = [.. specification.When.Values.Select(input => input.TargetProperty == property.Id ? input with { Value = SemanticValue.Text(value) } : input)]
            }
        };
        var file = SemanticCommandSpecificationRenderer.Render(specification, context);
        var syntax = CSharpSyntaxTree.ParseText(file.Content, new CSharpParseOptions(preprocessorSymbols: ["DEBUG"]), cancellationToken: TestContext.Current.CancellationToken);
        syntax.GetDiagnostics(TestContext.Current.CancellationToken).ShouldBeEmpty();
        var literal = syntax.GetRoot(TestContext.Current.CancellationToken).DescendantTokens().Single(token => token.RawKind == (int)SyntaxKind.StringLiteralToken && token.ValueText == value);
        literal.Text.ShouldEqual(CSharpCodeBuilder.StringLiteral(value));
        literal.ValueText.ShouldEqual(value);
    }

    [Theory]
    [InlineData(SemanticPrimitiveType.Uuid, "uuid\n\"\\\u2028\u2029\U0001D11E")]
    [InlineData(SemanticPrimitiveType.Date, "date\r\"\\\u2028\u2029\U0001D11E")]
    [InlineData(SemanticPrimitiveType.DateTime, "time\t\"\\\u2028\u2029\U0001D11E")]
    public void should_escape_text_arguments_of_scalar_parse_calls(SemanticPrimitiveType primitive, string value)
    {
        var types = new SemanticTypeSystem(new SemanticApplicationContext(_request, _options));
        var expression = types.Value(SemanticValue.Text(value), SemanticTypeReference.ForPrimitive(primitive));
        var syntax = SyntaxFactory.ParseExpression(expression);
        syntax.ContainsDiagnostics.ShouldBeFalse();
        var literal = syntax.DescendantTokens().Single(token => token.RawKind == (int)SyntaxKind.StringLiteralToken);
        literal.ValueText.ShouldEqual(value);
    }
}
