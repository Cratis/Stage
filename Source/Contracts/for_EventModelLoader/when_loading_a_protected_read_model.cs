// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_protected_read_model : Specification
{
    EventModel _model = null!;
    Slice _issue = null!;

    void Because()
    {
        _model = EventModelLoader.LoadFromSource(given.a_protected_invoice_source.Source);
        _issue = _model.Collections[0].Modules[0].Features[0].Slices.Single(slice => slice.Name == "IssueInvoice");
    }

    [Fact] void should_record_the_protected_mode() => _model.ProtectedCommandReads.ShouldBeTrue();
    [Fact] void should_keep_the_slices() => _model.Collections[0].Modules[0].Features[0].Slices.Count.ShouldEqual(2);
    [Fact] void should_keep_the_read() => _issue.Command!.Reads.Single().ReadModel.ShouldEqual("InvoiceState");
    [Fact] void should_keep_the_read_key() => _issue.Command!.Reads.Single().By.ShouldEqual("invoiceId");
    [Fact] void should_keep_the_requirement() => _issue.Command!.Requirements.Count.ShouldEqual(1);
}
