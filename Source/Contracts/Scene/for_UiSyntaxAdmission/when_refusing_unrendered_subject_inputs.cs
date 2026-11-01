// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiSyntaxAdmission;

public class when_refusing_unrendered_subject_inputs : Specification
{
    const string Prefix = "concept OrderId : Uuid\nmodule Sales\n  feature Orders\n    slice StateView List\n      readmodel Row\n        orderId OrderId key\n      query Rows => Row[]\n      command RegisterOrder\n        orderId OrderId identifier\n        note string\n      screen Orders\n        data Row[] via query Rows\n";

    [Theory]
    [InlineData("        action RegisterOrder for screen\n          with note from item.note\n", "note", "item.note")]
    [InlineData("        action RegisterOrder for item\n          with note from $context.note\n", "note", "$context.note")]
    public void should_refuse_both_syntax_paths_at_the_authored_location(string directive, string input, string binding)
    {
        var parsed = new ScreenplayCompiler().Parse(Prefix + directive);
        Assert.True(parsed.Success, string.Join(Environment.NewLine, parsed.Diagnostics));
        var syntax = parsed.Value!;
        AssertRefusal(Catch.Exception(() => new ScreenplayEventModelVisitor().Visit(syntax)), input, binding);
        AssertRefusal(Catch.Exception(() => new ScreenplaySceneVisitor().Visit(syntax)), input, binding);
    }

    static void AssertRefusal(Exception? error, string input, string binding)
    {
        var refusal = Assert.IsType<InvalidEventModel>(error);
        refusal.Message.ShouldContain("STAGE-ESM-059: Screen action 'RegisterOrder' input '" + input + "' from '" + binding + "' at ");
        refusal.Message.ShouldContain("Line = 13, Column = 9");
    }
}
