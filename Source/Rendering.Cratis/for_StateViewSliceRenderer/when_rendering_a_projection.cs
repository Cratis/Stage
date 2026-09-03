// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

public class when_rendering_a_projection : a_projection_slice
{
    RenderedFile _file = null!;
    StateViewSliceRenderer _renderer = null!;

    void Establish() => _renderer = new StateViewSliceRenderer();

    void Because() => _file = _renderer.Render(_summarySlice, _applicationSet, "CratisApp");

    [Fact] void should_place_the_file_under_its_own_slice_folder() =>
        _file.RelativePath.ShouldEqual(Path.Combine("Billing", "Invoices", "InvoiceSummary", "InvoiceSummary.cs"));
    [Fact] void should_emit_the_read_model_attribute() => _file.Content.ShouldContain("[ReadModel]");
    [Fact] void should_emit_a_from_event_attribute_per_distinct_event() => _file.Content.ShouldContain("[FromEvent<InvoiceRegistered>]");
    [Fact] void should_emit_a_from_event_attribute_for_the_second_event() => _file.Content.ShouldContain("[FromEvent<InvoiceSent>]");
    [Fact] void should_mark_the_key_property_with_its_set_from_mapping() =>
        _file.Content.ShouldContain("[Key] [SetFrom<InvoiceRegistered>(nameof(InvoiceRegistered.InvoiceNumber))] string InvoiceNumber");
    [Fact] void should_emit_an_increment_attribute() => _file.Content.ShouldContain("[Increment<InvoiceRegistered>] int TotalCount");
    [Fact] void should_emit_a_decrement_attribute() => _file.Content.ShouldContain("[Decrement<InvoiceSent>] int DraftCount");
    [Fact] void should_render_the_join_onto_the_property_it_contributes() =>
        _file.Content.ShouldContain(
            "[Join<CustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(CustomerRegistered.Name))] string CustomerName");
    [Fact] void should_not_flag_the_join_block_as_unrendered() => _file.Content.ShouldNotContain("TODO:");

    // Asserted against every diagnostic rather than one retired sentence, so it still fails if the join ever
    // starts reporting again under different wording.
    [Fact] void should_not_report_the_join_block_as_a_diagnostic() =>
        _file.Diagnostics.Any(diagnostic => diagnostic.Contains("join", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_reference_the_joined_event() => _file.Content.ShouldContain("public record CustomerRegistered(");
    [Fact] void should_declare_the_events_the_slice_owns() => _file.Content.ShouldContain("public record InvoiceRegistered(");
    [Fact] void should_emit_the_all_query_method() =>
        _file.Content.ShouldContain("public static IQueryable<InvoiceSummary> AllInvoiceSummaries(IMongoCollection<InvoiceSummary> collection) => collection.AsQueryable();");
    [Fact] void should_emit_the_by_id_query_method() =>
        _file.Content.ShouldContain("public static async Task<InvoiceSummary?> InvoiceSummaryById(IReadModels readModels, string invoiceNumber) => " +
            "await readModels.GetInstanceById<InvoiceSummary>((EventSourceId)invoiceNumber);");
}
