// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_a_handler_reads_a_provided_value : Specification
{
    const string Source = """
        module Orders
          provide region String from "Nordics"
          feature Ordering
            slice StateChange Place
              command PlaceOrder
                orderId Uuid identifier
                name String
                handler
                  implementation
                    hint "Normalize the name"
                produces event OrderPlaced
                  name String = name
        """;

    ArtifactRenderPlan _ignoring = null!;
    ArtifactRenderPlan _reading = null!;

    void Because()
    {
        _ignoring = handler_model.Plan(handler_model.Request("return new OrderPlaced(context.Command.Name);", Source));
        _reading = handler_model.Plan(handler_model.Request("return new OrderPlaced(context.Command.Name + context.Region);", Source));
    }

    [Fact] void should_render_a_handler_that_ignores_the_provided_values() => Assert.True(_ignoring.Success, string.Join("; ", _ignoring.Diagnostics));
    [Fact] void should_refuse_a_handler_that_reads_one_as_ambient_context() => _reading.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-015").ShouldBeTrue();
}
#endif
