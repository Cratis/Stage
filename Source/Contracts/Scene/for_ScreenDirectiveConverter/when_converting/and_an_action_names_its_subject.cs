// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_an_action_names_its_subject : Specification
{
    static ExternalComponent PlainAction(ScreenActionSubjectKind? subject, params InteractionArgumentSyntax[] inputs) =>
        (ExternalComponent)ScreenDirectiveConverter.Convert(
            [new ScreenActionSyntax("RegisterOrder", null, null, SourceLocation.Start) { Subject = subject, Inputs = inputs }],
            "screen").Single();

    static ExternalComponent ToolbarItem(ScreenActionSubjectKind? subject)
    {
        var toolbar = new ScreenToolbarSyntax(
            "main",
            [new ToolbarItemSyntax("register", ToolbarItemKind.Action, "RegisterOrder", SourceLocation.Start) { Subject = subject }],
            SourceLocation.Start);
        var element = (ExternalComponent)ScreenDirectiveConverter.Convert([toolbar], "screen").Single();
        return (ExternalComponent)element.Slots["items"].Single();
    }

    [Theory]
    [InlineData(ScreenActionSubjectKind.Item, "item")]
    [InlineData(ScreenActionSubjectKind.Screen, "screen")]
    public void should_carry_the_subject_of_a_plain_action_and_a_toolbar_item(ScreenActionSubjectKind subject, string expected)
    {
        PlainAction(subject).Properties["actionSubject"].ShouldEqual(expected);
        ToolbarItem(subject).Properties["actionSubject"].ShouldEqual(expected);
    }

    [Fact]
    public void should_carry_the_item_inputs_of_a_plain_action()
    {
        var input = new InteractionArgumentSyntax("itemId", "item.rowId", SourceLocation.Start);
        var inputs = (IDictionary<string, IDictionary<string, object?>>)PlainAction(ScreenActionSubjectKind.Item, input).Properties["subjectInputs"]!;
        inputs["itemId"]["subject"].ShouldEqual("item.rowId");
    }

    [Fact]
    public void should_write_neither_key_when_the_subject_is_omitted()
    {
        var input = new InteractionArgumentSyntax("itemId", "item.rowId", SourceLocation.Start);
        foreach (var element in new[] { PlainAction(null, input), ToolbarItem(null) })
        {
            Assert.False(element.Properties.ContainsKey("actionSubject"));
            Assert.False(element.Properties.ContainsKey("subjectInputs"));
        }
    }
}
