// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_native_event_context_mappings(when_executing_native_event_context_mappings.context fixture) : IClassFixture<when_executing_native_event_context_mappings.context>
{
    [Fact] void should_project_metadata_from_real_contexts() => Assert.Contains("Passed!", fixture.TestOutput, StringComparison.Ordinal);

    public class context : a_generated_application
    {
        public string TestOutput { get; private set; } = null!;

        protected override ArtifactRenderPlan CreatePlan()
        {
            var plan = event_context_projection_model.Plan(a_native_context_model.Source, strict: true);
            event_context_projection_model.AssertSucceeds(plan);
            return plan;
        }

        async Task Because()
        {
            AddGeneratedSpecification("when_projecting_context.cs", """
                using Cratis.Chronicle.Events;
                using Cratis.Chronicle.EventSequences;
                using Cratis.Chronicle.Testing.ReadModels;
                using Context.Home.Ctx.ContextTotals;
                using Xunit;

                public class when_projecting_context
                {
                    static readonly DateTimeOffset Instant = DateTimeOffset.Parse("2027-03-04T10:00:00.123Z");
                    static readonly Guid Correlation = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

                    static AppendedEvent Fact() => new(EventContext.From(
                        eventStore: "Context",
                        @namespace: "default",
                        eventSourceType: EventSourceType.Default,
                        eventStreamType: EventStreamType.All,
                        eventStreamId: EventStreamId.Default,
                        eventSourceId: "context-source",
                        eventType: typeof(ContextRecorded).GetEventType(),
                        sequenceNumber: 42,
                        correlationId: Correlation,
                        occurred: Instant), new ContextRecorded("payload-is-not-metadata"));

                    [Fact]
                    public async Task from_and_nested()
                    {
                        using var scenario = new ReadModelScenario<FromTotal>();
                        await scenario.ProcessAppendedEvents([Fact()]);
                        Assert.Single(scenario.Instances);
                        var instance = Assert.IsType<FromTotal>(scenario.Instance);
                        Assert.Equal(Instant, instance.Occurred);
                        Assert.Equal(Correlation, instance.Correlation);
                        Assert.Equal(42, instance.Sequence);
                        Assert.Equal("context-source", instance.Source);
                        Assert.Equal(Instant, instance.Details.Seen);
                    }

                    [Fact]
                    public async Task every()
                    {
                        using var scenario = new ReadModelScenario<EveryTotal>();
                        await scenario.ProcessAppendedEvents([Fact()]);
                        Assert.Single(scenario.Instances);
                        var instance = Assert.IsType<EveryTotal>(scenario.Instance);
                        Assert.Equal(Instant, instance.Occurred);
                        Assert.Equal(Correlation, instance.Correlation);
                        Assert.Equal(42, instance.Sequence);
                        Assert.Equal("context-source", instance.Source);
                    }

                    [Fact]
                    public async Task all()
                    {
                        using var scenario = new ReadModelScenario<AllTotal>();
                        await scenario.ProcessAppendedEvents([Fact()]);
                        Assert.Single(scenario.Instances);
                        Assert.Equal(Instant, Assert.IsType<AllTotal>(scenario.Instance).Occurred);
                    }
                }
                """);
            TestOutput = await Run("native-context-tests.log", "test", "-c", "Debug", "--filter", "FullyQualifiedName~when_projecting_context");
        }
    }
}
