// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiSyntaxAdmission;

public class when_refusing_explicit_action_subjects : Specification
{
    const string Prefix = "concept OrderId : Uuid\nmodule Sales\n  feature Orders\n    slice StateView List\n      readmodel Row\n        orderId OrderId key\n      query Rows => Row[]\n      command RegisterOrder\n        orderId OrderId identifier\n      screen Orders\n        data Row[] via query Rows\n";

    [Theory]
    [InlineData("        toolbar\n          item register action RegisterOrder for item\n", "Toolbar item 'register'", 13, 11)]
    [InlineData("        toolbar\n          item register action RegisterOrder for screen\n", "Toolbar item 'register'", 13, 11)]
    [InlineData("        action RegisterOrder for item\n", "Screen action 'RegisterOrder'", 12, 9)]
    [InlineData("        action RegisterOrder for screen\n", "Screen action 'RegisterOrder'", 12, 9)]
    public void should_refuse_both_syntax_paths_at_the_authored_location(string directive, string construct, int line, int column)
    {
        var parsed = new ScreenplayCompiler().Parse(Prefix + directive);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        var syntax = parsed.Value!;
        AssertRefusal(Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(syntax)), construct, line, column);
        AssertRefusal(Catch.Exception(() => new ScreenplaySceneVisitor().Visit(syntax)), construct, line, column);
        AssertRefusal(Catch.Exception(() => new ScreenplaySceneVisitor().Convert(syntax)), construct, line, column);
    }

    [Fact]
    public void should_preserve_omitted_subject_scene_output_byte_for_byte()
    {
        ScreenDirectiveSyntax[] directives =
        [
            new ScreenActionSyntax("RegisterOrder", null, null, SourceLocation.Start),
            new ScreenToolbarSyntax("main", [new ToolbarItemSyntax("register", ToolbarItemKind.Action, "RegisterOrder", SourceLocation.Start)], SourceLocation.Start)
        ];
        var before = JsonSerializer.SerializeToUtf8Bytes(ScreenDirectiveConverter.Convert(directives, "Orders"));
        var admission = new UiSyntaxAdmission();
        foreach (var directive in directives)
        {
            admission.VisitScreenDirective(directive);
        }
        var after = JsonSerializer.SerializeToUtf8Bytes(ScreenDirectiveConverter.Convert(directives, "Orders"));
        Assert.NotEmpty(before);
        Assert.Equal(before, after);
    }

    static void AssertRefusal(Exception? error, string construct, int line, int column)
    {
        var refusal = Assert.IsType<InvalidEventModel>(error);
        refusal.Message.ShouldContain($"STAGE-ESM-058: {construct} at ");
        refusal.Message.ShouldContain($"Line = {line}, Column = {column}");
        refusal.Message.ShouldContain("explicit action subjects are not yet rendered (Cratis/Stage#855, Screenplay decision 0110).");
    }
}
