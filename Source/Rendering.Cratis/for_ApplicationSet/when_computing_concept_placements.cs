// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ApplicationSet;

public class when_computing_concept_placements : Specification
{
    ApplicationSet _applicationSet = null!;

    static CommandSyntax CommandUsing(string commandName, string propertyName, string conceptName) =>
        new(
            commandName,
            [new PropertySyntax(propertyName, new TypeRefSyntax(conceptName, false, false, SourceLocation.Start), SourceLocation.Start)],
            null,
            [],
            [],
            null,
            SourceLocation.Start);

    static SliceSyntax SliceWith(string name, CommandSyntax command) =>
        new(SliceType.StateChange, name, [], [command], [], [], [], [], [], [], [], SourceLocation.Start);

    void Establish()
    {
        var concepts = new[]
        {
            new ConceptSyntax("SliceOnly", "String", [], [], SourceLocation.Start),
            new ConceptSyntax("FeatureShared", "String", [], [], SourceLocation.Start),
            new ConceptSyntax("ModuleShared", "String", [], [], SourceLocation.Start),
            new ConceptSyntax("AppWide", "String", [], [], SourceLocation.Start),
            new ConceptSyntax("Unused", "String", [], [], SourceLocation.Start),
        };

        var registerSlice = SliceWith("Register", CommandUsing("Register", "value", "SliceOnly"));
        var cancelSlice = SliceWith("Cancel", CommandUsing("Cancel", "value", "FeatureShared"));
        var reviewSlice = SliceWith("Review", CommandUsing("Review", "value", "FeatureShared"));
        var refundSlice = SliceWith("Refund", CommandUsing("Refund", "value", "ModuleShared"));
        var archiveSlice = SliceWith("Archive", CommandUsing("Archive", "value", "AppWide"));
        var payslice = SliceWith("Pay", CommandUsing("Pay", "value", "ModuleShared"));
        var openTicketSlice = SliceWith("OpenTicket", CommandUsing("OpenTicket", "value", "AppWide"));
        var closeTicketSlice = SliceWith("CloseTicket", CommandUsing("CloseTicket", "value", "TicketOnly"));

        var invoicesFeature = new FeatureSyntax(
            "Invoices", [], [registerSlice, cancelSlice, reviewSlice, refundSlice, archiveSlice], SourceLocation.Start);
        var paymentsFeature = new FeatureSyntax("Payments", [], [payslice], SourceLocation.Start);
        var billingModule = new ModuleSyntax("Billing", [], [invoicesFeature, paymentsFeature], SourceLocation.Start);

        var ticketsFeature = new FeatureSyntax("Tickets", [], [openTicketSlice, closeTicketSlice], SourceLocation.Start);
        var supportModule = new ModuleSyntax("Support", [], [ticketsFeature], SourceLocation.Start);

        var application = new ApplicationSyntax([], concepts, [], [billingModule, supportModule], SourceLocation.Start);
        _applicationSet = new ApplicationSet([application]);
    }

    [Fact] void should_place_a_slice_specific_concept_in_its_own_slice_folder() =>
        _applicationSet.ConceptPlacements["SliceOnly"].ShouldContainOnly("Billing", "Invoices", "Register");
    [Fact] void should_place_a_feature_shared_concept_in_the_feature_folder() =>
        _applicationSet.ConceptPlacements["FeatureShared"].ShouldContainOnly("Billing", "Invoices");
    [Fact] void should_place_a_module_shared_concept_in_the_module_folder() =>
        _applicationSet.ConceptPlacements["ModuleShared"].ShouldContainOnly("Billing");
    [Fact] void should_place_a_concept_shared_across_modules_at_the_root() => _applicationSet.ConceptPlacements["AppWide"].ShouldBeEmpty();
    [Fact] void should_place_an_unused_concept_at_the_root() => _applicationSet.ConceptPlacements["Unused"].ShouldBeEmpty();
}
