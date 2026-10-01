// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_an_automapped_read_model : Specification
{
    const string Source =
        """
        module Sales
          feature Invoices
            slice StateChange Register
              command RegisterInvoice
                invoiceNumber String
                amount Decimal
                produces InvoiceRegistered
                  invoiceNumber = invoiceNumber
                  amount = amount
              event InvoiceRegistered
                invoiceNumber String
                amount Decimal
            slice StateView Listing
              query AllInvoices => InvoiceListItem
              projection InvoiceListItem => InvoiceListItem
                automap
                from InvoiceRegistered
        """;

    string _directory = null!;
    EventModel _model = null!;

    void Establish()
    {
        _directory = SpecTemporaryRoot.NewPath("stage-specs");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "application.play"), Source);
    }

    async Task Because() => _model = await EventModelLoader.LoadFromDirectoryAsync(_directory);

    void Destroy() => SpecTemporaryRoot.Delete(_directory);

    [Fact]
    void should_take_its_shape_from_the_events_it_maps_from()
    {
        var readModel = _model.Collections.Single().Modules.Single().Features.Single().Slices
            .Select(slice => slice.ReadModel)
            .First(readModel => readModel is not null)!;

        using var schema = JsonDocument.Parse(readModel.Schema);
        var properties = schema.RootElement.GetProperty("properties").EnumerateObject().Select(property => property.Name).ToArray();

        properties.ShouldContain("invoiceNumber");
        properties.ShouldContain("amount");
    }
}
