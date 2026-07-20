// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_specification : given.a_compiled_invoicing_model
{
    Slice _slice = null!;
    Cratis.Stage.Contracts.Specifications.Specification _specification = null!;

    void Because()
    {
        _slice = _model.Collections[0].Modules[0].Features[0].Slices.Single(slice => slice.Name == "RegisterInvoice");
        _specification = _slice.Specifications.Single();
    }

    [Fact] void should_name_the_specification() => _specification.Name.ShouldEqual("RegistersAnInvoice");
    [Fact] void should_have_no_given_events() => _specification.Given.Count.ShouldEqual(0);
    [Fact] void should_capture_the_when_command() => _specification.When!.Name.ShouldEqual("RegisterInvoice");
    [Fact] void should_link_the_when_to_the_command() => _specification.When!.CommandId.ShouldEqual(_slice.Command!.Id);
    [Fact] void should_render_the_when_values() => WhenValue("invoiceNumber").ShouldEqual("INV-000001");
    [Fact] void should_have_a_single_then_event() => _specification.ThenEvents.Count.ShouldEqual(1);
    [Fact] void should_name_the_then_event() => _specification.ThenEvents[0].Name.ShouldEqual("InvoiceRegistered");
    [Fact] void should_link_the_then_event_to_the_event() => _specification.ThenEvents[0].EventId.ShouldEqual(_slice.Events.Single().Id);
    [Fact] void should_have_no_then_errors() => _specification.ThenErrors.Count.ShouldEqual(0);

    string? WhenValue(string property)
    {
        using var values = JsonDocument.Parse(_specification.When!.Values);

        return values.RootElement.GetProperty(property).GetString();
    }
}
