// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Projections;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_slice_without_a_projection : Specification
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateView Listing
              query ListInvoices => InvoiceListReadModel[]
        """;

    ReadModelDefinition _readModel = null!;

    void Because()
    {
        var model = EventModelLoader.LoadFromSource(Source);
        _readModel = model.Collections.Single().Modules.Single().Features.Single().Slices.Single().ReadModel!;
    }

    [Fact] void should_leave_the_selected_projection_name_unknown() => _readModel.SelectedProjectionName.ShouldBeNull();
    [Fact] void should_report_no_ignored_projections() => _readModel.IgnoredProjectionNames.ShouldBeEmpty();
    [Fact] void should_leave_the_projection_empty() => _readModel.Projection.ShouldBeNull();
    [Fact] void should_keep_the_querys_read_model() => _readModel.Name.ShouldEqual("InvoiceListReadModel");
}
