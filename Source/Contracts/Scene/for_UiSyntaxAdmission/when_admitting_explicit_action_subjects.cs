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

public class when_admitting_explicit_action_subjects : Specification
{
    const string Prefix = "concept OrderId : Uuid\nmodule Sales\n  feature Orders\n    slice StateView List\n      readmodel Row\n        orderId OrderId key\n      query Rows => Row[]\n      command RegisterOrder\n        orderId OrderId identifier\n      screen Orders\n        data Row[] via query Rows\n";

    [Theory]
    [InlineData("        toolbar\n          item register action RegisterOrder for item\n")]
    [InlineData("        toolbar\n          item register action RegisterOrder for screen\n")]
    [InlineData("        action RegisterOrder for item\n")]
    [InlineData("        action RegisterOrder for screen\n")]
    [InlineData("        action RegisterOrder for item\n          with orderId from item.orderId\n")]
    public void should_admit_both_syntax_paths(string directive)
    {
        var parsed = new ScreenplayCompiler().Parse(Prefix + directive);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        var syntax = parsed.Value!;
        Assert.Null(Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(syntax)));
        Assert.Null(Catch.Exception(() => new ScreenplaySceneVisitor().Visit(syntax)));
        Assert.Null(Catch.Exception(() => new ScreenplaySceneVisitor().Convert(syntax)));
    }

    [Fact]
    public void should_admit_the_effective_shape_of_an_inferred_item_subject()
    {
        var action = new ScreenActionSyntax("RegisterOrder", null, null, SourceLocation.Start)
        {
            Subject = ScreenActionSubjectKind.Item,
            Inputs = [new InteractionArgumentSyntax("orderId", "item.id", SourceLocation.Start)]
        };
        Assert.Null(Catch.Exception(() => new UiSyntaxAdmission().VisitScreenDirective(action)));
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
}
