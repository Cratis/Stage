// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Guards;
using Cratis.Scene.Model.Outlets;
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

    static OutletButton ToolbarItem(ScreenActionSubjectKind? subject)
    {
        var toolbar = new ScreenToolbarSyntax(
            "main",
            [new ToolbarItemSyntax("register", ToolbarItemKind.Action, "RegisterOrder", SourceLocation.Start) { Subject = subject }],
            SourceLocation.Start);
        var element = (ExternalComponent)ScreenDirectiveConverter.Convert([toolbar], "screen").Single();
        return ((IEnumerable<OutletItem>)element.Properties["items"]!).Cast<OutletButton>().Single();
    }

    [Theory]
    [InlineData(ScreenActionSubjectKind.Item, "item", OutletSubject.Item)]
    [InlineData(ScreenActionSubjectKind.Screen, "screen", OutletSubject.Screen)]
    public void should_carry_the_subject_of_a_plain_action_and_a_toolbar_item(ScreenActionSubjectKind subject, string expected, OutletSubject outlet)
    {
        PlainAction(subject).Properties["actionSubject"].ShouldEqual(expected);
        ToolbarItem(subject).Subject.ShouldEqual(outlet);
    }

    [Fact]
    public void should_carry_the_item_inputs_of_a_plain_action()
    {
        var input = new InteractionArgumentSyntax("itemId", "item.rowId", SourceLocation.Start);
        var inputs = (IDictionary<string, SubjectReference>)PlainAction(ScreenActionSubjectKind.Item, input).Properties["subjectInputs"]!;
        inputs.Count.ShouldEqual(1);
        inputs["itemId"].Subject.ShouldEqual("item.rowId");
    }

    [Fact]
    public void should_write_neither_key_when_the_subject_is_omitted()
    {
        var input = new InteractionArgumentSyntax("itemId", "item.rowId", SourceLocation.Start);
        var element = PlainAction(null, input);
        Assert.False(element.Properties.ContainsKey("actionSubject"));
        Assert.False(element.Properties.ContainsKey("subjectInputs"));
        ToolbarItem(null).Subject.ShouldBeNull();
    }
}
