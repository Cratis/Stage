// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

/// <summary>
/// A filled slot has no node of its own in the Scene model, so an attachment written inside one used to be
/// dropped without a word - the conversion only ever looked at the screen's top-level directives. Translating
/// a sample application is what surfaced it, because nobody writes an interaction at the top of a screen that
/// uses a template; they write it next to the thing it acts on.
/// </summary>
public class and_behaviors_are_written_inside_a_filled_slot : Specification
{
    ScreenSyntax _syntax = null!;
    BehaviorScope _scope = null!;
    Screen _result = null!;

    void Establish()
    {
        _scope = new BehaviorScope(
            BehaviorAttachments.Declared([new BehaviorSyntax("RefreshOnReturn", [], [], SourceLocation.Start)]),
            []);

        _syntax = new(
            "InvoiceList",
            null,
            [
                new ScreenTemplateReferenceSyntax(
                    "InvoiceWorkspace",
                    [
                        new ScreenSlotSyntax(
                            "body",
                            [
                                new ScreenTitleSyntax("Invoices", new SourceLocation(1, 1)),
                                new ScreenUsesBehaviorSyntax(
                                    new UsesBehaviorSyntax("RefreshOnReturn", [], new SourceLocation(2, 1)),
                                    new SourceLocation(2, 1))
                            ],
                            SourceLocation.Start)
                    ],
                    SourceLocation.Start)
            ],
            SourceLocation.Start);
    }

    void Because() => _result = ScreenConverter.Convert(_syntax, "AppShell", [], [], _scope);

    [Fact] void should_fold_the_attachment_onto_the_screen() => _result.Behaviors.Count.ShouldEqual(1);
    [Fact] void should_resolve_it() => _result.Behaviors[0].Name.ShouldEqual("RefreshOnReturn");
    [Fact] void should_still_place_the_slot_content() => _result.SlotContent["body"].Count.ShouldEqual(1);
}
