// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_model_bound_projection_specifications : a_generated_application
{
    protected override bool DeferPlan => true;

    [Theory]
    [InlineData("classic", true)]
    [InlineData("model-bound", true)]
    [InlineData("classic", false)]
    [InlineData("model-bound", false)]
    public async Task should_preserve_routing_updates_removal_and_recreation(string convention, bool payloadKeys)
    {
        try
        {
            var source = payloadKeys ? when_selecting_model_bound_projections.PayloadSource : when_selecting_model_bound_projections.Source;
            var plan = when_selecting_model_bound_projections.Plan(source, convention);
            Assert.Equal(convention == "classic" || payloadKeys, when_selecting_model_bound_projections.Code(plan).Contains("IProjectionFor", StringComparison.Ordinal));
            RenderApplication(plan);
            var specification = """
                using Cratis.Chronicle.Events;
                using Cratis.Chronicle.Testing.ReadModels;
                using Projects.Common;
                using Projects.Projects.Registration.CreateProject;
                using Projects.Projects.Registration.Lookup;
                using Xunit;

                namespace Projects.ProjectionEquivalence;

                public class when_projecting_root_events
                {
                    static readonly ProjectId Key = new(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                    static readonly ProjectId Other = new(Guid.Parse("4fa85f64-5717-4562-b3fc-2c963f66afa7"));
                    static readonly EventSourceId Source = new("5fa85f64-5717-4562-b3fc-2c963f66afa8");
                    static readonly bool PayloadKeys = __PAYLOAD_KEYS__;

                    static EventSourceId SourceFor(ProjectId key) => PayloadKeys ? Source : key;

                    [Fact]
                    public async Task should_update_one_key_without_crossing_another_key()
                    {
                        var scenario = new ReadModelScenario<Summary>();
                        await scenario.Given.ForEventSource(SourceFor(Key)).Events(new ProjectCreated(Key, "First"));
                        await scenario.Given.ForEventSource(SourceFor(Other)).Events(new ProjectCreated(Other, "Other"));
                        await scenario.Given.ForEventSource(PayloadKeys ? new EventSourceId("6fa85f64-5717-4562-b3fc-2c963f66afa9") : SourceFor(Key)).Events(new ProjectChanged(Key, "Renamed"));
                        var actual = Assert.IsType<Summary>(scenario.InstanceForEventSourceId(Key));
                        Assert.Equal(Key, actual.Id);
                        Assert.Equal("Renamed", actual.Name);
                        Assert.Equal(0, actual.Visits);
                        var other = Assert.IsType<Summary>(scenario.InstanceForEventSourceId(Other));
                        Assert.Equal("Other", other.Name);
                        Assert.Equal(1, other.Visits);
                    }

                    [Fact]
                    public async Task should_remove_and_recreate_only_the_payload_key()
                    {
                        var scenario = new ReadModelScenario<Summary>();
                        await scenario.Given.ForEventSource(SourceFor(Key)).Events(new ProjectCreated(Key, "First"));
                        await scenario.Given.ForEventSource(SourceFor(Other)).Events(new ProjectCreated(Other, "Other"));
                        await scenario.Given.ForEventSource(SourceFor(Key)).Events(new ProjectRemoved(Key));
                        Assert.Null(scenario.InstanceForEventSourceId(Key));
                        Assert.NotNull(scenario.InstanceForEventSourceId(Other));
                        await scenario.Given.ForEventSource(SourceFor(Key)).Events(new ProjectCreated(Key, "Again"));
                        var recreated = Assert.IsType<Summary>(scenario.InstanceForEventSourceId(Key));
                        Assert.Equal("Again", recreated.Name);
                        Assert.Equal(1, recreated.Visits);
                    }
                }
                """.Replace("__PAYLOAD_KEYS__", payloadKeys ? "true" : "false", StringComparison.Ordinal);
            if (!payloadKeys)
            {
                specification = specification.Replace("new ProjectCreated(Key, ", "new ProjectCreated(", StringComparison.Ordinal)
                    .Replace("new ProjectCreated(Other, ", "new ProjectCreated(", StringComparison.Ordinal)
                    .Replace("new ProjectChanged(Key, ", "new ProjectChanged(", StringComparison.Ordinal)
                    .Replace("new ProjectRemoved(Key)", "new ProjectRemoved()", StringComparison.Ordinal);
            }

            AddGeneratedSpecification("ProjectionEquivalence.cs", specification);
            var build = await Run($"{convention}-projection-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var tests = await Run($"{convention}-projection-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", tests, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
