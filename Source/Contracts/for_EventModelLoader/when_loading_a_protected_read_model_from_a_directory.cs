// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_protected_read_model_from_a_directory : Specification
{
    string _directory = null!;
    EventModel _model = null!;

    void Establish()
    {
        _directory = SpecTemporaryRoot.NewPath("stage-specs");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "application.play"), given.a_protected_invoice_source.Preamble + "module Billing\n  feature Invoices\n    slice StateChange RegisterInvoice\n      command RegisterInvoice\n        invoiceId String identifier\n        produces InvoiceRegistered\n          for invoiceId\n      event InvoiceRegistered\n");
        File.WriteAllText(Path.Combine(_directory, "other.play"), "module Other\n  feature Things\n    slice StateChange DoThing\n      command DoThing\n        id String identifier\n        produces ThingDone\n          for id\n      event ThingDone\n");
    }

    async Task Because() => _model = await EventModelLoader.LoadFromDirectoryAsync(_directory);

    [Fact] void should_record_the_protected_mode() => _model.ProtectedCommandReads.ShouldBeTrue();
    [Fact] void should_merge_every_file() => _model.Collections[0].Modules.Count.ShouldEqual(2);

    void Destroy() => SpecTemporaryRoot.Delete(_directory);
}
