// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Testing;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.for_ProjectionConverter.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_root_joins_with_explicit_keys;

public class and_the_key_is_an_event_property : Specification
{
    object _generated = null!;
    object _oracle = null!;
    Task<string[]> _execution = null!;

    void Because()
    {
        var model = a_root_join_model.Create();
        var execution = SemanticExecutionPlan.Compile(model);
        var plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        var assembly = RenderedOutput.Load(plan.Artifacts.Where(file => file.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && file.RelativePath != "Program.cs")
            .Select(file => new RenderedFile(file.RelativePath, System.Text.Encoding.UTF8.GetString(file.Bytes.AsSpan())))
            .Append(new RenderedFile("RootJoinProbe.cs", ExecutionSource)));
        var projection = assembly.GetTypes().Single(type => type.Name == "ProjectSummaryProjection");
        var readModel = assembly.GetTypes().Single(type => type.Name == "ProjectSummary");
        var events = assembly.GetTypes().Where(type => Attribute.IsDefined(type, typeof(EventTypeAttribute))).ToImmutableList();
        var artifacts = new only_rendered_events(events);
        _execution = (Task<string[]>)assembly.GetType("RootJoinProbe")!.GetMethod("Run")!.Invoke(null, [new Defaults(artifacts)])!;
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.AllClrTypes.Returns(events);
        foreach (var @event in events) eventTypes.GetEventTypeFor(@event).Returns(@event.GetEventType());
        var builderType = typeof(ProjectionBuilderFor<>).MakeGenericType(readModel);
        var builder = Activator.CreateInstance(builderType, new ProjectionId("root-join"), projection, new CamelCaseNamingPolicy(), eventTypes, new JsonSerializerOptions())!;
        projection.GetMethod("Define")!.Invoke(Activator.CreateInstance(projection), [builder]);
        _generated = Join(builderType.GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(builder, null)!);
        var syntax = new ScreenplayCompiler().Compile(a_root_join_model.Source);
        Assert.True(syntax.Success, string.Join(';', syntax.Diagnostics));
        _oracle = Join(ChronicleProjectionOracle.Visit(syntax.Value!.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "ProjectLookup")
            .Projections.Single(projection => projection.Name == "ProjectSummaryProjection")));
    }

    [Fact] void should_keep_the_read_model_correlation_property() => Property(_generated, "On").ShouldEqual("projectId");
    [Fact] void should_use_the_event_property_instead_of_the_source_identity() => Property(_generated, "Key").ShouldEqual("correlationId");
    [Fact] void should_match_the_chronicle_visitor_on_property() => Property(_generated, "On").ShouldEqual(Property(_oracle, "On"));
    [Fact] void should_match_the_visitor_join_mappings() => Mappings(_generated).ShouldContainOnly(Mappings(_oracle));

    // The pinned visitor has no keyed Join syntax; only the key expression is compared to the native fluent builder.
    [Fact]
    void should_match_the_native_fluent_key_expression()
    {
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.GetEventTypeFor(typeof(key_event)).Returns(new EventType("key-event", 1));
        var builder = new ProjectionBuilderFor<key_model>(new ProjectionId("key-oracle"), GetType(), new CamelCaseNamingPolicy(), eventTypes, new JsonSerializerOptions());
        builder.Join<key_event>(join => join.On(model => model.ProjectId).UsingKey(@event => @event.CorrelationId));
        var definition = typeof(ProjectionBuilderFor<key_model>).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(builder, null)!;
        Property(_generated, "Key").ShouldEqual(Property(Join(definition), "Key"));
    }

    static string[] Mappings(object definition)
    {
        var properties = (IDictionary)definition.GetType().GetProperty("Properties")!.GetValue(definition)!;
        return [.. properties.Keys.Cast<object>().Select(key => $"{key}={properties[key]}")];
    }

    public record key_model(string ProjectId);
    [EventType("root-join-key-oracle")]
    public record key_event(string CorrelationId);

    [Fact]
    async Task should_execute_the_generated_key_against_existing_roots_not_the_source_matched_decoy() =>
        (await _execution).ShouldContainOnly("after", "after", "decoy", "missing");

    const string ExecutionSource = """
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.ReadModels;
        using Projects.Common;
        using Projects.Projects.Registration.RegisterProject;
        using Projects.Projects.Registration.ProjectLookup;
        public static class RootJoinProbe
        {
            public static async System.Threading.Tasks.Task<string[]> Run(Cratis.Chronicle.Testing.Defaults defaults)
            {
                const string First = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01";
                const string Second = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f02";
                const string Decoy = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f03";
                const string Foreign = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f04";
                const string Source = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f05";
                using var scenario = new ReadModelScenario<ProjectSummary>(null, defaults).WithProjection(builder => new ProjectSummaryProjection().Define(builder));
                var foreign = new ProjectId(System.Guid.Parse(Foreign));
                await scenario.Given.ForEventSource(new EventSourceId(First)).Events(new ProjectRegistered(foreign, new ProjectName("before")));
                await scenario.Given.ForEventSource(new EventSourceId(Second)).Events(new ProjectRegistered(foreign, new ProjectName("before")));
                await scenario.Given.ForEventSource(new EventSourceId(Decoy)).Events(new ProjectRegistered(new ProjectId(System.Guid.Parse(Source)), new ProjectName("decoy")));
                await scenario.Given.ForEventSource(new EventSourceId(Source)).Events(new ProjectNamed(foreign, new ProjectName("after")));
                return [scenario.InstanceForEventSourceId(new EventSourceId(First))!.Name.Value,
                    scenario.InstanceForEventSourceId(new EventSourceId(Second))!.Name.Value,
                    scenario.InstanceForEventSourceId(new EventSourceId(Decoy))!.Name.Value,
                    scenario.InstanceForEventSourceId(new EventSourceId(Source)) is null ? "missing" : "inserted"];
            }
        }
        """;

    static object Join(object definition) => ((IDictionary)definition.GetType().GetProperty("Join")!.GetValue(definition)!).Values.Cast<object>().Single();
    static string Property(object definition, string name) => definition.GetType().GetProperty(name)!.GetValue(definition)!.ToString()!;
}
