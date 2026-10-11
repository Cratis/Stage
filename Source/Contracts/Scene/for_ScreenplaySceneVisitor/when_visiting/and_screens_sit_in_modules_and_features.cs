// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_visiting;

public class and_screens_sit_in_modules_and_features : Specification
{
    SceneApplication _result = null!;

    void Establish()
    {
        var listing = new ScreenSyntax("InvoiceList", null, [new ScreenTitleSyntax("Invoices", SourceLocation.Start)], SourceLocation.Start);
        var audit = new ScreenSyntax("InvoiceAudit", null, [new ScreenTitleSyntax("Audit", SourceLocation.Start)], SourceLocation.Start)
        {
            Contributions = [new ScreenContributionSyntax("Navigation", 1, [new ScreenTitleSyntax("Audit entry", SourceLocation.Start)], SourceLocation.Start)]
        };
        var listingSlice = new SliceSyntax(Cratis.Screenplay.Syntax.SliceType.StateView, "Listing", [], [], [], [], [], [], [listing], [], [], SourceLocation.Start);
        var auditSlice = new SliceSyntax(Cratis.Screenplay.Syntax.SliceType.StateView, "Auditing", [], [], [], [], [], [], [audit], [], [], SourceLocation.Start);

        var inner = new FeatureSyntax("Auditing", [], [auditSlice], SourceLocation.Start);
        var outer = new FeatureSyntax("Browsing", [inner], [listingSlice], SourceLocation.Start);
        var moduleContribution = new ContributionSyntax("Navigation", new ScreenNavigateSyntax("InvoiceList", null, SourceLocation.Start), "Invoicing home", 0, SourceLocation.Start)
        {
            Id = "nav:invoicing",
            Icon = "receipt",
            Presentation = "primary",
            Group = "Billing"
        };
        var module = new ModuleSyntax("Invoicing", [], [outer], SourceLocation.Start, null, null, [moduleContribution]);

        _result = new ScreenplaySceneVisitor().Visit(new([], [], [], [module], SourceLocation.Start));
    }

    [Fact] void should_record_the_module_and_feature_a_screen_was_declared_in() => _result.Screens.Single(_ => _.Name == "InvoiceList").Path.ToArray().ShouldEqual(["Invoicing", "Browsing"]);
    [Fact] void should_record_every_level_of_a_nested_feature() => _result.Screens.Single(_ => _.Name == "InvoiceAudit").Path.ToArray().ShouldEqual(["Invoicing", "Browsing", "Auditing"]);
    [Fact] void should_attach_what_the_module_contributes_to_the_screens_beneath_it() => _result.Screens.All(_ => _.Contributions.Any(contribution => (string?)contribution.Content.Properties!["label"] == "Invoicing home")).ShouldBeTrue();
    [Fact] void should_record_the_module_alone_as_where_a_module_contribution_was_declared() => ((string[])ModuleEntry("InvoiceList").Properties![SceneElementProperties.Hierarchy]!).ShouldEqual(["Invoicing"]);
    [Fact] void should_carry_the_authored_navigation_metadata_of_a_module_contribution() => string.Join('|', new[] { "id", "icon", "presentation", "group" }.Select(name => ModuleEntry("InvoiceList").Properties![name])).ShouldEqual("nav:invoicing|receipt|primary|Billing");
    [Fact] void should_record_where_a_screen_contributed_from() => ((string[])_result.Screens.Single(_ => _.Name == "InvoiceAudit").Contributions.Last().Content.Properties![SceneElementProperties.Hierarchy]!).ShouldEqual(["Invoicing", "Browsing", "Auditing"]);

    Cratis.Scene.Model.Elements.ExternalComponent ModuleEntry(string screen) =>
        (Cratis.Scene.Model.Elements.ExternalComponent)_result.Screens.Single(_ => _.Name == screen).Contributions.First().Content;
}
