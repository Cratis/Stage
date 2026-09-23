// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_projection_with_unsupported_conversion : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => EventModelLoader.LoadFromSource("""
        module Catalog
          feature Items
            slice StateView Summary
              projection Summary => SummaryModel
                sequence outbox
                from ItemRegistered
                  name = name
        """));

    [Fact] void should_report_invalid_event_model_with_slice_context() =>
        _error.ShouldBeOfExactType<InvalidEventModel>();

    [Fact] void should_name_the_failed_slice_and_expression() =>
        _error!.Message.ShouldContain("Catalog.Items.Summary");
}
