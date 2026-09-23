// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

/// <summary>
/// A screen's attachments arrive as directives, sharing the body with its content. They are what the content
/// does rather than more of it, so they must leave the directive stream and become attachments - an end-to-end
/// translation found them reaching the content converter, which knows nothing about them.
/// </summary>
public class and_the_screen_attaches_behaviors : Specification
{
    ScreenSyntax _syntax = null!;
    BehaviorScope _scope = null!;
    List<RenderFinding> _findings = null!;
    Screen _result = null!;

    void Establish()
    {
        _findings = [];
        _scope = new BehaviorScope(
            BehaviorAttachments.Declared([new BehaviorSyntax("ConfirmThenCancel", [], [], SourceLocation.Start)]),
            _findings);

        _syntax = new(
            "InvoiceList",
            null,
            [
                new ScreenTitleSyntax("Invoices", new SourceLocation(1, 1)),
                new ScreenUsesBehaviorSyntax(
                    new UsesBehaviorSyntax("ConfirmThenCancel", [], new SourceLocation(2, 1)),
                    new SourceLocation(2, 1)),
                new ScreenBehaviorSyntax(
                    new BehaviorSyntax(null, [], [], new SourceLocation(3, 1)),
                    new SourceLocation(3, 1))
            ],
            SourceLocation.Start);
    }

    void Because() => _result = ScreenConverter.Convert(_syntax, "AppShell", [], [], _scope);

    [Fact] void should_attach_both_behaviors() => _result.Behaviors.Count.ShouldEqual(2);
    [Fact] void should_resolve_the_named_one() => _result.Behaviors[0].Name.ShouldEqual("ConfirmThenCancel");
    [Fact] void should_keep_the_inline_one_anonymous() => _result.Behaviors[1].Name.ShouldBeNull();
    [Fact] void should_not_treat_a_behavior_as_content() => _result.SlotContent[DefaultLayout.ContentSlotName].Count.ShouldEqual(1);
    [Fact] void should_report_nothing() => _findings.ShouldBeEmpty();
}
