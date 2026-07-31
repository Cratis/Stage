// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Xunit;

namespace Cratis.Stage.Runtime.for_ProducedEventPayloads.when_building;

public class and_every_value_source_is_used : given.a_command_payload
{
    const string EnvironmentVariable = "CRATIS_STAGE_SPEC_SERVICE_NAME";

    IReadOnlyList<ProducedEventPayload> _events = [];

    void Establish() => Environment.SetEnvironmentVariable(EnvironmentVariable, "invoicing-service");

    void Because() => _events = ProducedEventPayloads.Build(
        [
            new ProducedEvent(
                "InvoiceRegistered",
                When: null,
                [
                    new ProducedEventProperty("invoiceId", ProducedValueKind.CommandProperty, "invoiceId"),
                    new ProducedEventProperty("status", ProducedValueKind.Literal, "\"draft\""),
                    new ProducedEventProperty("registeredAt", ProducedValueKind.Occurred, string.Empty),
                    new ProducedEventProperty("registeredBy", ProducedValueKind.Identity, "name"),
                    new ProducedEventProperty("source", ProducedValueKind.Environment, EnvironmentVariable),
                    new ProducedEventProperty("label", ProducedValueKind.Template, "${invoiceNumber} (${currency})"),
                    new ProducedEventProperty("missing", ProducedValueKind.CommandProperty, "notOnThePayload"),
                    new ProducedEventProperty("unknownIdentityValue", ProducedValueKind.Identity, "notAnIdentityPath"),
                    new ProducedEventProperty("unsetVariable", ProducedValueKind.Environment, "CRATIS_STAGE_SPEC_NOT_SET"),
                ],
                ["audit"])
        ],
        _command,
        _occurred,
        _identity);

    void Destroy() => Environment.SetEnvironmentVariable(EnvironmentVariable, null);

    [Fact] void should_build_one_event() => _events.Count.ShouldEqual(1);
    [Fact] void should_name_the_event_type() => _events[0].EventType.ShouldEqual("InvoiceRegistered");
    [Fact] void should_carry_the_tags() => _events[0].Tags.ShouldContainOnly("audit");
    [Fact] void should_copy_a_command_property() => Value("invoiceId").ShouldEqual("8a4d1f7e-3c2b-4a5d-9e6f-0b1c2d3e4f50");
    [Fact] void should_write_a_literal() => Value("status").ShouldEqual("draft");
    [Fact] void should_write_the_occurred_time() => Value("registeredAt").ShouldEqual("2026-07-30T12:00:00.0000000Z");
    [Fact] void should_write_the_identity_value() => Value("registeredBy").ShouldEqual("Some One");
    [Fact] void should_write_the_environment_variable() => Value("source").ShouldEqual("invoicing-service");
    [Fact] void should_interpolate_a_template() => Value("label").ShouldEqual("INV-000001 (USD)");
    [Fact] void should_leave_out_a_property_the_caller_did_not_send() => _events[0].Content.ContainsKey("missing").ShouldBeFalse();

    // The model declares these on the event, so the event's schema requires them - writing an empty value keeps the
    // append schema-valid where omitting the property would have it rejected.
    [Fact] void should_write_an_empty_value_for_an_unresolved_identity_path() => Value("unknownIdentityValue").ShouldEqual(string.Empty);

    [Fact] void should_write_an_empty_value_for_an_unset_environment_variable() => Value("unsetVariable").ShouldEqual(string.Empty);

    string? Value(string property) => _events[0].Content[property]!.GetValue<string>();
}
