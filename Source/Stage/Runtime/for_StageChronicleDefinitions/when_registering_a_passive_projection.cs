// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Xunit;

namespace Cratis.Stage.Runtime.for_StageChronicleDefinitions;

public class when_registering_a_passive_projection : Specification
{
    [Theory]
    [InlineData(true, "None")]
    [InlineData(false, "InMemory")]
    public void should_choose_the_sink_and_activation_together(bool passive, string sink)
    {
        var source = """
            module Stock
              feature Decisions
                slice StateView Balance
                  readmodel Balance
                    PASSIVE
                    id String key
                    quantity Int
                  projection BuildBalance => Balance
                    from Adjusted
                      id = $eventSourceId
                      quantity = quantity
            """.Replace("PASSIVE", passive ? "passive" : string.Empty, StringComparison.Ordinal);
        var definitions = StageChronicleDefinitions.Build(EventModelLoader.LoadFromSource(source), "event-log");
        definitions.Projections.Single().IsActive.ShouldEqual(!passive);
        definitions.ReadModels.Single().Sink.TypeId.ShouldEqual(sink);
        definitions.ReadModels.Single().Sink.ConfigurationId.ShouldEqual(Guid.Empty);
    }
}
#endif
