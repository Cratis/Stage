// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_visiting;

public class and_a_dialog_is_declared_before_the_page_it_belongs_to : Specification
{
    SceneApplication _result = null!;

    void Establish()
    {
        ScreenSyntax Screen(string name, string template) =>
            new(name, null, [new ScreenTemplateReferenceSyntax(template, [new ScreenSlotSyntax("main", [new ScreenTitleSyntax(name, SourceLocation.Start)], SourceLocation.Start)], SourceLocation.Start)], SourceLocation.Start);

        SliceSyntax Slice(string name, ScreenSyntax screen) =>
            new(Cratis.Screenplay.Syntax.SliceType.StateView, name, [], [], [], [], [], [], [screen], [], [], SourceLocation.Start);

        // The board puts the command's slice, and its dialog, before the list that shows its effect.
        SliceSyntax[] slices =
        [
            Slice("ApproveProduct", Screen("ApproveProduct", "Cratis.Blueprint.Default ConfirmDialog")),
            Slice("Price", Screen("PriceSheet", "SideSheet")),
            Slice("Ready", Screen("ReadyForApproval", "ListScreen")),
            Slice("Details", Screen("ProductPage", "RecordPage"))
        ];
        var feature = new FeatureSyntax("Publication", [], slices, SourceLocation.Start);

        _result = new ScreenplaySceneVisitor().Visit(new([], [], [], [new ModuleSyntax("Merchandising", [], [feature], SourceLocation.Start)], SourceLocation.Start));
    }

    [Fact] void should_start_the_application_on_a_page() => _result.Screens[0].Name.ShouldEqual("ReadyForApproval");
    [Fact] void should_keep_the_pages_in_the_order_they_were_declared() => _result.Screens.Take(2).Select(_ => _.Name).ToArray().ShouldEqual(["ReadyForApproval", "ProductPage"]);
    [Fact] void should_keep_the_dialogs_in_the_order_they_were_declared() => _result.Screens.Skip(2).Select(_ => _.Name).ToArray().ShouldEqual(["ApproveProduct", "PriceSheet"]);
    [Fact] void should_still_know_every_screen() => _result.Screens.Count.ShouldEqual(4);
}
