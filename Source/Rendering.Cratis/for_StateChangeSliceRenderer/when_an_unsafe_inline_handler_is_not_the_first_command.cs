// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_an_unsafe_inline_handler_is_not_the_first_command : state_change_slices
{
    Exception _error = null!;

    void Establish() => _registerInvoice = _registerInvoice with
    {
        Slice = _registerInvoice.Slice with { Commands = [.. _registerInvoice.Slice.Commands, .. _processBatch.Slice.Commands] },
    };

    void Because() => _error = Catch.Exception(() => new StateChangeSliceRenderer().Render(_registerInvoice, _applicationSet, "CratisApp"));

    [Fact] void should_block_the_entire_selected_slice() => _error.ShouldBeOfExactType<UnsupportedInlineCommandHandler>();
    [Fact] void should_identify_the_later_command() => ((UnsupportedInlineCommandHandler)_error).CommandName.ShouldEqual("ProcessBatch");
    [Fact] void should_identify_the_selected_slice() => ((UnsupportedInlineCommandHandler)_error).FullSlicePath.ShouldEqual("Billing.Invoices.RegisterInvoice");
}
