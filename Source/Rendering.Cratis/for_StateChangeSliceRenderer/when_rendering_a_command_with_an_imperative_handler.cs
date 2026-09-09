// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_a_command_with_an_imperative_handler : state_change_slices
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => new StateChangeSliceRenderer().Render(_processBatch, _applicationSet, "CratisApp"));

    [Fact] void should_reject_the_historical_screenplay_identity_handler() => _error.ShouldBeOfExactType<UnsupportedInlineCommandHandler>();
    [Fact] void should_identify_the_context_binding() => ((UnsupportedInlineCommandHandler)_error).Reason.ShouldEqual(InlineCommandHandlerRejectionReason.ContextBinding);
    [Fact] void should_preserve_the_code_source_location() => ((UnsupportedInlineCommandHandler)_error).Location.ShouldEqual(_processBatch.Slice.Commands.Single().Handler!.Code!.Location);
    [Fact] void should_report_the_exact_diagnostic() => _error.Message.ShouldEqual(
        "STAGE-CRATIS-INLINE-001: Command 'ProcessBatch' in slice 'Billing.Invoices.ProcessBatch' at fixtures/Batch.play(17,9) " +
        "has an unsupported inline handler: ContextBinding. Only C# bodies proven independent of the generated context parameter can be rendered.");
}
