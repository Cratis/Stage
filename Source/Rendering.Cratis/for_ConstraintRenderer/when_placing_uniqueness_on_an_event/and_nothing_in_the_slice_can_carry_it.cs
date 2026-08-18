// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConstraintRenderer.when_placing_uniqueness_on_an_event;

/// <summary>
/// Constraints with nowhere to land - one backed by a file, and ones naming an event or property the slice
/// does not declare.
/// </summary>
/// <remarks>
/// An attribute can only go on an event the slice renders, so these have no rendered form and must keep being
/// reported. Counting them as rendered would be worse than not rendering them: the report is the only thing
/// telling a reader that the application does not enforce what the document states.
/// </remarks>
public class and_nothing_in_the_slice_can_carry_it : Specification
{
    static readonly PropertySyntax _number = new("invoiceNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start);
    static readonly EventSyntax _event = new("InvoiceRegistered", [_number], SourceLocation.Start);

    static readonly ConstraintSyntax _backedByAFile = new FileConstraintSyntax("Bespoke", new FileReferenceSyntax("Constraints/Bespoke.cs", SourceLocation.Start), SourceLocation.Start);
    static readonly ConstraintSyntax _namingAnotherEvent = new UniquePropertyConstraintSyntax("Elsewhere", "invoiceNumber", "PaymentReceived", SourceLocation.Start);
    static readonly ConstraintSyntax _namingAnotherProperty = new UniquePropertyConstraintSyntax("Missing", "amount", "InvoiceRegistered", SourceLocation.Start);
    static readonly ConstraintSyntax _namingAnotherEventWholly = new UniqueEventConstraintSyntax("Elsewhere", "PaymentReceived", SourceLocation.Start);

    [Fact] void should_not_render_a_constraint_backed_by_a_file() =>
        ConstraintRenderer.IsRendered(_backedByAFile, [_event]).ShouldBeFalse();

    [Fact] void should_not_render_one_naming_an_event_the_slice_does_not_declare() =>
        ConstraintRenderer.IsRendered(_namingAnotherEvent, [_event]).ShouldBeFalse();

    [Fact] void should_not_render_one_naming_a_property_the_event_does_not_declare() =>
        ConstraintRenderer.IsRendered(_namingAnotherProperty, [_event]).ShouldBeFalse();

    [Fact] void should_not_render_a_whole_event_constraint_for_an_event_the_slice_does_not_declare() =>
        ConstraintRenderer.IsRendered(_namingAnotherEventWholly, [_event]).ShouldBeFalse();
}
