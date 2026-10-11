// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_visiting;

public class and_nested_features_contribute_navigation : Specification
{
    SceneApplication _result = null!;

    void Establish()
    {
        var listing = new ScreenSyntax("InvoiceList", null, [new ScreenTitleSyntax("Invoices", SourceLocation.Start)], SourceLocation.Start);
        var details = new ScreenSyntax("InvoiceDetails", null, [new ScreenTitleSyntax("Details", SourceLocation.Start)], SourceLocation.Start);
        var listingSlice = new SliceSyntax(Cratis.Screenplay.Syntax.SliceType.StateView, "Listing", [], [], [], [], [], [], [listing], [], [], SourceLocation.Start);
        var detailsSlice = new SliceSyntax(Cratis.Screenplay.Syntax.SliceType.StateView, "Viewing", [], [], [], [], [], [], [details], [], [], SourceLocation.Start);

        var inner = new FeatureSyntax(
            "Details",
            [],
            [detailsSlice],
            SourceLocation.Start,
            null,
            [new ContributionSyntax("Navigation", new ScreenNavigateSyntax("InvoiceDetails", null, SourceLocation.Start), "Details", 1, SourceLocation.Start)]);
        var outer = new FeatureSyntax(
            "Browsing",
            [inner],
            [listingSlice],
            SourceLocation.Start,
            null,
            [new ContributionSyntax("Navigation", new ScreenNavigateSyntax("InvoiceList", null, SourceLocation.Start), "Invoices", 0, SourceLocation.Start)]);
        var module = new ModuleSyntax("Invoicing", [], [outer], SourceLocation.Start);

        _result = new ScreenplaySceneVisitor().Visit(new([], [], [], [module], SourceLocation.Start));
    }

    static string[] HierarchyOf(SceneApplication result, string screen) =>
        (string[])result.Screens.Single(candidate => candidate.Name == screen).Contributions.Single().Content.Properties![SceneElementProperties.Hierarchy]!;

    [Fact] void should_carry_the_module_and_feature_of_the_outer_contribution() => HierarchyOf(_result, "InvoiceList").ShouldEqual(["Invoicing", "Browsing"]);
    [Fact] void should_carry_every_level_of_a_nested_feature() => HierarchyOf(_result, "InvoiceDetails").ShouldEqual(["Invoicing", "Browsing", "Details"]);
}
