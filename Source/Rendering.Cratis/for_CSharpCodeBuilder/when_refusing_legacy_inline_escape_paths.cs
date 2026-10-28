// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Expressions;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_refusing_legacy_inline_escape_paths : Specification
{
    [Fact]
    void should_refuse_a_foreign_reaction_body_at_the_renderer()
    {
        var (application, slice) = Reaction(new("java", "return null;", SourceLocation.Start));
        var error = Assert.Throws<InlineCodeRejected>(() => new ReactionSliceRenderer().Render(slice, application, "Shop"));
        Assert.DoesNotContain(error.Diagnostics, diagnostic => diagnostic.Code == "STAGE-DEGRADE-003");
    }

    [Theory]
    [InlineData("0); Inject(); RuleFor(x => x.Value).MinimumLength(0")]
    [InlineData("DateTime.UtcNow")]
    void should_refuse_all_raw_expressions_in_validation_with_a_typed_diagnostic(string text)
    {
        var location = SourceLocation.Start;
        var raw = new RawExpressionSyntax(text, location);
        var command = new CommandSyntax(
            "Register",
            [new("value", new("String", false, false, location), location)],
            null,
            [new DeclarativeValidateSyntax([new("value", ValidationRuleKind.Min, raw, null, location)], location)],
            [],
            null,
            location);
        var builder = new CSharpCodeBuilder();
        Assert.Throws<UnsupportedExpression>(() => CommandValidatorRenderer.Render(builder, command, "Register", new ApplicationSet([]), []));
        Assert.DoesNotContain(text, builder.ToString());
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    void should_keep_a_file_path_inside_the_reaction_comment(string newline)
    {
        var (application, slice) = Reaction(null, new("Ops" + newline + "public class Escaped {}", SourceLocation.Start));
        var source = new ReactionSliceRenderer().Render(slice, application, "Shop").Content;
        Assert.Contains("// TODO: implementation lives in 'Ops public class Escaped {}'", source);
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        Assert.DoesNotContain(root.DescendantNodes().OfType<CS.ClassDeclarationSyntax>(), type => type.Identifier.ValueText == "Escaped");
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    void should_refuse_unsafe_header_lines_and_normalize_generated_comments(string newline)
    {
        ArtifactFileHeader.IsValid(["Before" + newline + "After"]).ShouldBeFalse();
        new CSharpCodeBuilder().Line("// Before" + newline + "After").ToString().ShouldEqual("// Before After\n");
    }

    static (ApplicationSet Application, LocatedSlice Slice) Reaction(CodeBlockSyntax? code, FileReferenceSyntax? file = null)
    {
        var location = SourceLocation.Start;
        var trigger = new ReactionTriggerSyntax(new NamedTriggerSourceSyntax("Occurred", location), [], file, code, location);
        var reaction = new ReactionSyntax("Observe", [trigger], location);
        var slice = new SliceSyntax(SliceType.Automation, "Observe", [new("Occurred", [], location)], [], [], [], [], [reaction], [], [], [], location);
        var feature = new FeatureSyntax("Changes", [], [slice], location);
        var module = new ModuleSyntax("Shop", [], [feature], location);
        var application = new ApplicationSet([new ApplicationSyntax([], [], [], [module], location)]);
        return (application, application.Slices.Single());
    }
}
