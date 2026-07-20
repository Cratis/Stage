// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Events;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_events : given.a_compiled_invoicing_model
{
    EventDefinition _event = null!;

    void Because() => _event = _model.Collections[0].Modules[0].Features[0].Slices
        .Single(slice => slice.Name == "RegisterInvoice").Events.Single();

    [Fact] void should_name_the_event() => _event.Name.ShouldEqual("InvoiceRegistered");
    [Fact] void should_mark_the_event_as_owned() => _event.SourceEventId.ShouldEqual(string.Empty);
    [Fact] void should_synthesize_a_valid_schema() => Status().GetProperty("type").GetString().ShouldEqual("string");
    [Fact] void should_synthesize_the_enum_values() => Enum().ShouldContain("draft");
    [Fact] void should_attach_the_property_uniqueness_constraint() => _event.UniqueConstraint!.Name.ShouldEqual("UniqueInvoiceNumber");
    [Fact] void should_track_the_unique_property() => _event.UniqueConstraint!.EventsWithProperties.Single().Properties.ShouldContain("invoiceNumber");
    [Fact] void should_attach_the_unique_event_type_constraint() => _event.UniqueEventTypeConstraint!.Name.ShouldEqual("OneRegistrationPerInvoice");
    [Fact] void should_target_the_event_for_the_unique_event_type_constraint() => _event.UniqueEventTypeConstraint!.EventType.ShouldEqual("InvoiceRegistered");

    JsonElement Status()
    {
        using var schema = JsonDocument.Parse(_event.Schema);

        return schema.RootElement.GetProperty("properties").GetProperty("status").Clone();
    }

    IReadOnlyList<string> Enum() => [.. Status().GetProperty("enum").EnumerateArray().Select(element => element.GetString()!)];
}
