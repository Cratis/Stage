// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConstraintRenderer.when_placing_uniqueness_on_an_event;

/// <summary>
/// A slice declaring an event, a property of it kept unique, and the whole event kept unique per source.
/// </summary>
/// <remarks>
/// Uniqueness is the one invariant a rendered application cannot enforce for itself - a read-then-write check
/// loses the race the constraint exists to win - so it either reaches Chronicle's append-time mechanism or the
/// document states something the application does not do.
/// </remarks>
public class and_the_slice_declares_the_event_it_names : Specification
{
    static readonly PropertySyntax _number = new("invoiceNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start);
    static readonly EventSyntax _event = new("InvoiceRegistered", [_number], SourceLocation.Start);

    static readonly ConstraintSyntax _onProperty = new UniquePropertyConstraintSyntax("UniqueInvoiceNumber", "invoiceNumber", "InvoiceRegistered", SourceLocation.Start);
    static readonly ConstraintSyntax _onEvent = new UniqueEventConstraintSyntax("OneRegistrationPerInvoice", "InvoiceRegistered", SourceLocation.Start);

    [Fact] void should_name_the_constraint_on_the_property_it_keeps_unique() =>
        ConstraintRenderer.ForProperty([_onProperty], "InvoiceRegistered", "invoiceNumber")
            .ShouldEqual("[property: Unique(\"UniqueInvoiceNumber\")]");

    [Fact] void should_target_the_property_rather_than_the_positional_parameter() =>
        ConstraintRenderer.ForProperty([_onProperty], "InvoiceRegistered", "invoiceNumber")!
            .StartsWith("[property:", StringComparison.Ordinal).ShouldBeTrue();

    [Fact] void should_leave_a_property_the_constraint_does_not_name_alone() =>
        ConstraintRenderer.ForProperty([_onProperty], "InvoiceRegistered", "amount").ShouldBeNull();

    [Fact] void should_leave_an_event_the_constraint_does_not_name_alone() =>
        ConstraintRenderer.ForProperty([_onProperty], "PaymentReceived", "invoiceNumber").ShouldBeNull();

    [Fact] void should_name_the_constraint_on_the_event_it_keeps_unique() =>
        ConstraintRenderer.ForEvent([_onEvent], "InvoiceRegistered").ShouldEqual("Unique(\"OneRegistrationPerInvoice\")");

    [Fact] void should_not_place_a_property_constraint_on_the_event_itself() =>
        ConstraintRenderer.ForEvent([_onProperty], "InvoiceRegistered").ShouldBeNull();

    [Fact] void should_count_both_as_rendered() =>
        new[] { _onProperty, _onEvent }.All(constraint => ConstraintRenderer.IsRendered(constraint, [_event])).ShouldBeTrue();
}
