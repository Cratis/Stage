// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_calling_a_renamed_capture_handler : given.a_translation_specification_application
{
    async Task Because()
    {
        _model = Transform(_model, slice => slice with { Captures = [.. slice.Captures.Select(capture => capture with { Name = "OnShipmentDispatched" })] });
        Render(clientOnly: true);
        await RunGenerated("when_receiving_a_shipment");
    }

    [Fact] void should_compile_and_execute_a_generated_specification_calling_a_renamed_handler() => Assert.Empty(RenderedOutput.Warnings(_sources));
}
#endif
