// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ContributionConverter.when_converting;

public class and_the_item_declares_where_it_opens : Specification
{
    DestinationReference _outlet = null!;
    DestinationReference _dialog = null!;
    DestinationReference _external = null!;
    DestinationReference _plain = null!;

    static DestinationReference Convert(ContributionSyntax contribution) =>
        (DestinationReference)ContributionConverter.Convert(contribution, "item").Content.Properties![SceneElementProperties.Destination]!;

    static ContributionSyntax Item(ContributionDestinationSyntax? destination) =>
        new("Navigation", new ScreenNavigateSyntax("InvoiceDetails", "invoiceId", SourceLocation.Start), "Details", null, SourceLocation.Start) { Destination = destination };

    void Because()
    {
        _outlet = Convert(Item(new ContributionDestinationSyntax(ContributionDestinationKind.Outlet, "detail", SourceLocation.Start)));
        _dialog = Convert(Item(new ContributionDestinationSyntax(ContributionDestinationKind.Dialog, "HelpDialog", SourceLocation.Start)));
        _external = Convert(Item(new ContributionDestinationSyntax(ContributionDestinationKind.External, "https://example.com/help", SourceLocation.Start)));
        _plain = Convert(Item(null));
    }

    [Fact] void should_open_in_the_named_outlet() => string.Join('|', new object?[] { _outlet.Outlet, _outlet.Kind }).ShouldEqual("detail|Outlet");
    [Fact] void should_keep_the_screen_and_parameter_the_item_navigates_to_beside_the_outlet() => string.Join('|', new object?[] { _outlet.Screen, _outlet.RouteParameterBindings!.Keys.Single() }).ShouldEqual("InvoiceDetails|invoiceId");
    [Fact] void should_open_the_named_dialog() => string.Join('|', new object?[] { _dialog.Dialog, _dialog.Kind }).ShouldEqual("HelpDialog|Dialog");
    [Fact] void should_leave_the_application_for_an_external_route() => string.Join('|', new object?[] { _external.Route, _external.Kind }).ShouldEqual("https://example.com/help|External");
    [Fact] void should_only_navigate_to_the_screen_when_it_names_no_destination() => string.Join('|', new object?[] { _plain.Screen, _plain.Outlet, _plain.Dialog, _plain.Kind }).ShouldEqual("InvoiceDetails|||");
}
