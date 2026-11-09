// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_FormConverter.when_converting_with_diagnostics;

public class and_the_form_has_manual_columns : Specification
{
    static readonly SourceLocation _at = new(6, 3, "forms.play");
    static readonly SourceLocation _columnAt = new(9, 5, "forms.play");
    Cratis.Scene.Model.Forms.Form _result = null!;
    ConversionDiagnostics _diagnostics = null!;

    void Because()
    {
        var form = new FormSyntax("MakeBookingForm", "MakeBooking", null, [new FormFieldSyntax("eventTypeId", null, null, null, _at), new FormFieldSyntax("dayKey", null, null, null, _at), new FormFieldSyntax("attendeeName", null, null, "Attendee", _at)], null, _at)
        {
            ColumnMode = FormColumnMode.Manual,
            Columns = [new FormColumnSyntax("attendeeName", "Your name", _at), new FormColumnSyntax("eventTypeId", "Event type", _at), new FormColumnSyntax("missing", "Missing", _columnAt)]
        };
        _diagnostics = new ConversionDiagnostics();
        _result = FormConverter.Convert(form, _diagnostics, "Booking.MakeBookingForm");
    }

    [Fact] void should_follow_the_column_order_then_keep_the_other_fields() => _result.Fields.Select(field => field.Name).ShouldEqual(["attendeeName", "eventTypeId", "dayKey"]);
    [Fact] void should_label_the_fields_with_the_column_labels() => _result.Fields.Take(2).Select(field => field.Label).ShouldEqual(["Your name", "Event type"]);
    [Fact] void should_report_a_column_without_a_field() => _diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-FORM-002");
    [Fact] void should_point_at_the_column() => _diagnostics.All.Single().Location.ShouldEqual(_columnAt);
}
