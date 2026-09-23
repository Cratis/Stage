// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Projections;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_the_billing_invoice_model : Specification
{
    EventModel _model = null!;
    ProjectionDefinition _summary = null!;
    ProjectionDefinition _state = null!;
    CommandDefinition _issue = null!;
    CommandDefinition _register = null!;

    async Task Because()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Samples")))
        {
            directory = directory.Parent;
        }

        _model = await EventModelLoader.LoadFromDirectoryAsync(Path.Combine(directory!.FullName, "Samples", "Billing"));
        var slices = _model.Collections.SelectMany(_ => _.Modules).SelectMany(_ => _.Features)
            .Single(_ => _.Name == "Invoices").Slices;
        _summary = slices.Single(_ => _.Name == "InvoiceList").ReadModel!.Projection!;
        _state = slices.Single(_ => _.Name == "IssueInvoice").ReadModel!.Projection!;
        _issue = slices.Single(_ => _.Name == "IssueInvoice").Command!;
        _register = slices.Single(_ => _.Name == "RegisterInvoice").Command!;
    }

    [Fact] void should_map_the_customer_name_into_the_summary() => Mapping(_summary, "InvoiceRegistered", "customerName").ShouldEqual("customerName");
    [Fact] void should_start_the_summary_as_a_draft() => Mapping(_summary, "InvoiceRegistered", "status").ShouldEqual("\"draft\"");
    [Fact] void should_allow_cancelling_a_draft() => Mapping(_summary, "InvoiceRegistered", "canCancel").ShouldEqual("True");
    [Fact] void should_allow_cancelling_an_issued_invoice() => Mapping(_summary, "InvoiceIssued", "canCancel").ShouldEqual("True");
    [Fact] void should_stop_offering_cancellation_after_cancelling() => Mapping(_summary, "InvoiceCancelled", "canCancel").ShouldEqual("False");
    [Fact] void should_accept_the_customer_name_at_registration() => HasProperty(_register.Schema, "customerName").ShouldBeTrue();
    [Fact] void should_map_the_customer_name_into_the_registration_event() => _register.Produces.Single().Properties.Single(_ => _.Property == "customerName").Expression.ShouldEqual("customerName");
    [Fact] void should_produce_the_issued_event_without_an_unbound_condition() => _issue.Produces.Single().When.ShouldBeNull();
    [Fact] void should_require_a_positive_amount_from_the_read_state() => _issue.Requirements.Select(_ => _.Condition).ShouldContain(new ProducedEventComparison("InvoiceState.amount", ProducedEventComparisonOperator.GreaterThan, "0"));
    [Fact] void should_initialize_the_command_state_as_a_draft() => Mapping(_state, "InvoiceRegistered", "status").ShouldEqual("\"draft\"");
    [Fact] void should_initialize_the_command_state_amount() => Mapping(_state, "InvoiceRegistered", "amount").ShouldEqual("amount");
    [Fact] void should_transition_the_command_state_to_issued() => Mapping(_state, "InvoiceIssued", "status").ShouldEqual("\"issued\"");
    [Fact] void should_update_the_command_state_amount_on_issue() => Mapping(_state, "InvoiceIssued", "amount").ShouldEqual("amount");
    [Fact] void should_transition_the_command_state_to_cancelled() => Mapping(_state, "InvoiceCancelled", "status").ShouldEqual("\"cancelled\"");
    [Fact] void should_preserve_the_amount_on_cancellation() => _state.From["InvoiceCancelled"].Properties.Any(_ => _.Property == "amount").ShouldBeFalse();
    [Fact] void should_key_all_state_transitions_by_invoice() => _state.From.Values.All(_ => _.Key == "invoiceId").ShouldBeTrue();

    static string Mapping(ProjectionDefinition projection, string eventName, string property) =>
        projection.From[eventName].Properties.Single(_ => _.Property == property).Expression;

    static bool HasProperty(string schema, string property)
    {
        using var document = JsonDocument.Parse(schema);
        return document.RootElement.GetProperty("properties").TryGetProperty(property, out _);
    }
}
