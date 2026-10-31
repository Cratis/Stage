// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_an_unprotected_read_model : Specification
{
    EventModel _model = null!;

    void Because() => _model = EventModelLoader.LoadFromSource(given.a_protected_invoice_source.Body);

    [Fact] void should_not_record_the_protected_mode() => _model.ProtectedCommandReads.ShouldBeFalse();
}
