// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Projections;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_slice_with_multiple_projections : Specification
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange Register
              event InvoiceRegistered
                invoiceNumber String
            slice StateView Listing
              projection InvoiceList => InvoiceListReadModel
                from InvoiceRegistered
                  invoiceNumber = invoiceNumber
              projection InvoiceSummary => InvoiceSummaryReadModel
                from InvoiceRegistered
                  summaryNumber = invoiceNumber
              projection InvoiceReport => InvoiceReportReadModel
                from InvoiceRegistered
                  reportNumber = invoiceNumber
        """;

    ReadModelDefinition _readModel = null!;
    JsonElement _properties;

    void Because()
    {
        var model = EventModelLoader.LoadFromSource(Source);
        _readModel = model.Collections.Single().Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Listing").ReadModel!;
        using var schema = JsonDocument.Parse(_readModel.Schema);
        _properties = schema.RootElement.GetProperty("properties").Clone();
    }

    [Fact] void should_report_the_first_projection_as_selected() => _readModel.SelectedProjectionName.ShouldEqual("InvoiceList");
    [Fact] void should_report_the_remaining_projections_in_declaration_order() => _readModel.IgnoredProjectionNames.SequenceEqual(["InvoiceSummary", "InvoiceReport"]).ShouldBeTrue();
    [Fact] void should_keep_the_first_projections_read_model() => _readModel.Name.ShouldEqual("InvoiceListReadModel");
    [Fact] void should_convert_the_first_projections_mapping() => _readModel.Projection!.From["InvoiceRegistered"].Properties.Single().Property.ShouldEqual("invoiceNumber");
    [Fact] void should_keep_the_first_projections_schema() => _properties.TryGetProperty("invoiceNumber", out _).ShouldBeTrue();
    [Fact] void should_not_merge_the_second_projections_schema() => _properties.TryGetProperty("summaryNumber", out _).ShouldBeFalse();
    [Fact] void should_not_merge_the_third_projections_schema() => _properties.TryGetProperty("reportNumber", out _).ShouldBeFalse();
}
