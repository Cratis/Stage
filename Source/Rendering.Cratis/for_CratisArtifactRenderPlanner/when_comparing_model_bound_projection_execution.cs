// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Compares Chronicle's in-memory engine for a model-bound projection against Screenplay's reference execution over the same facts.
/// </summary>
public class when_comparing_model_bound_projection_execution : a_generated_application
{
    const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    const string Second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
    const string Third = "7fa85f64-5717-4562-b3fc-2c963f66afaa";
    readonly (string Name, Fact[] Facts)[] _cases =
    [
        ("create_then_rename", [new("ProjectRegistered", First, First) { Name = "First" }, new("ProjectRenamed", First, First) { Name = "Renamed" }]),
        ("create_then_name_through_the_event_source", [new("ProjectRegistered", First, First) { Name = "First" }, new("ProjectNamed", First) { Name = "Named" }]),
        ("name_before_create", [new("ProjectNamed", First) { Name = "Early" }, new("ProjectRegistered", First, First) { Name = "First" }]),
        ("two_instances", [new("ProjectRegistered", First, First) { Name = "First" }, new("ProjectRegistered", Second, Second) { Name = "Second" }, new("ProjectRenamed", Second, Second) { Name = "Again" }]),
        ("payload_key_differs_from_source", [new("ProjectRegistered", First, First) { Name = "First" }, new("ProjectRegistered", Second, Second) { Name = "Second" }, new("ProjectRenamed", First, Second) { Name = "Target second" }])
    ];
    ExecutableSemanticModel _model = null!;
    string _testOutput = null!;
    string _projectionSource = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var plan = when_rendering_model_bound_projections.Plan(when_rendering_model_bound_projections.Source, true);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model-bound"), "model-bound", "Scopes.play", when_rendering_model_bound_projections.Source);
        _model = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog)).Value!.Model;
        return plan;
    }

    async Task Because()
    {
        _projectionSource = ReadGeneratedFile("Projects/Registration/ProjectLookup/ProjectLookup.cs");
        var fluent = when_rendering_model_bound_projections.Code(when_rendering_model_bound_projections.Plan(when_rendering_model_bound_projections.Source, false))
            .Replace("ProjectSummary", "FluentSummary", StringComparison.Ordinal)
            .Replace("ProjectDetails", "FluentDetails", StringComparison.Ordinal);
        AddGeneratedSpecification("Projects/Registration/ProjectLookup/FluentSummary.cs", fluent);
        foreach (var (name, facts) in _cases)
        {
            var expected = Reference(facts);
            AddGeneratedSpecification($"Projects/Registration/ProjectLookup/when_{name}.cs", GeneratedSpecification((name, facts), expected));
            AddGeneratedSpecification($"Projects/Registration/ProjectLookup/when_fluent_{name}.cs", GeneratedSpecification(("fluent_" + name, facts), expected).Replace("ReadModelScenario<ProjectSummary>", "ReadModelScenario<FluentSummary>", StringComparison.Ordinal));
        }

        await Run("model-bound-debug.log", "build", "-c", "Debug", "-warnaserror");
        _testOutput = await Run("model-bound-tests.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_match_all_reference_snapshots() => _testOutput.ShouldContain("Passed!");
    [Fact] void should_generate_no_suppressions() => _projectionSource.ShouldNotContain("#pragma");
    [Fact] void should_generate_model_bound_attributes() => _projectionSource.ShouldContain("[FromEvent<ProjectRegistered>]");

    string Reference(Fact[] facts)
    {
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var source = feature.Slices.Single(slice => slice.Commands.Any());
        var originalSpec = source.Specifications.Single(specification => specification.Name == "RegisteringAProject");
        var type = source.Commands.Single().Properties.Single(property => property.IsIdentifier).Type;
        var events = feature.Slices.SelectMany(slice => slice.Events).ToDictionary(@event => @event.Name);
        var givens = facts.Select(fact => new SemanticSpecificationEvent(
            events[fact.Event].Id,
            [.. events[fact.Event].Properties.Select(property => new SemanticPropertyValue(property.Id, new SemanticTextValue(fact.ValueFor(property.Name))))])
        {
            EventSource = new(type, new SemanticTextValue(fact.Source))
        }).ToArray();
        var marker = events["ProjectRegistered"];
        var spec = originalSpec with
        {
            GivenEvents = [.. givens],
            When = null,
            WhenAppended = new SemanticSpecificationAppend(marker.Id, [.. marker.Properties.Select(property => new SemanticPropertyValue(property.Id, new SemanticTextValue(property.Name == "projectId" ? Third : "Unused")))])
            {
                EventSource = new(type, new SemanticTextValue(Third))
            },
            ThenEvents = [],
            ThenReadModels = [],
            ThenQueries = [],
            ThenErrors = []
        };

        // The reference now validates typed Given sources against declared producer destinations.
        // These synthetic projection events have no source commands, so declare test-only producers
        // without changing the application rendered for Chronicle's projection scenario.
        var command = source.Commands.Single();
        var sourceId = command.Properties.Single(property => property.IsIdentifier).Id;
        var sourceName = command.Properties.Single(property => property.Name == "name").Id;
        var probeProduces = facts.Select(fact => fact.Event).Distinct().Where(name => name != "ProjectRegistered")
            .Select(name => new SemanticProducedEvent(
                events[name].Id,
                null,
                null,
                [.. events[name].Properties.Select(property => new SemanticPropertyMapping(
                    property.Id,
                    new SemanticResolvedExpression(
                        SemanticExpressionRootKind.Command,
                        SemanticExpressionSourceKind.Property,
                        property.Name == "name" ? sourceName : sourceId)))]));
        var referenceSource = source with { Commands = [command with { Produces = [.. command.Produces, .. probeProduces] }] };
        var referenceModel = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == source.Id ? referenceSource : slice)] }] }]
        });
        var result = new SemanticSpecificationRunner().Run(ReferencePlan(referenceModel, spec), spec.Id);
        var accepted = Assert.IsType<SemanticAccepted>(result.Execution);
        var readModel = feature.Slices.SelectMany(slice => slice.ReadModels).Single(candidate => candidate.Name == "ProjectSummary");
        return JsonSerializer.Serialize(new[] { First, Second }.Select(key => Snapshot(
            accepted.World.ReadModels.SingleOrDefault(instance => instance.ReadModel == readModel.Id && instance.Key is SemanticTextValue value && value.Value == key), readModel)));
    }

    static SemanticExecutionPlan ReferencePlan(ExecutableSemanticModel model, SemanticSpecification specification)
    {
        var referencePlan = SemanticExecutionPlan.Compile(model);
        Assert.True(referencePlan.Success, string.Join(Environment.NewLine, referencePlan.Issues));
        var compiled = referencePlan.Plan!;
        var constructor = typeof(SemanticExecutionPlan).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        return (SemanticExecutionPlan)constructor.Invoke(
        [
            model,
            compiled.Commands,
            compiled.Events,
            compiled.Projections,
            compiled.ReadModels,
            compiled.Queries,
            compiled.Specifications.SetItem(specification.Id, specification),
            compiled.Constraints,
            compiled.Reactions,
            compiled.Captures,
            UnadmittedArtifacts(compiled)
        ]);
    }

    // The execution plan keeps what it did not admit internally; a reference plan assembled for a probe carries the same.
    static object? UnadmittedArtifacts(SemanticExecutionPlan plan) =>
        typeof(SemanticExecutionPlan).GetProperty("UnadmittedArtifacts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(plan);

    static object? Snapshot(SemanticReadModelInstance? instance, SemanticReadModel readModel)
    {
        if (instance is null) return null;
        var values = instance.Values.ToDictionary(value => readModel.Properties.Single(property => property.Id == value.TargetProperty).Name, value => value.Value);
        return new { projectId = Text(values["projectId"]), name = Text(values["name"]) };
    }

    static string? Text(SemanticValue value) => value is SemanticTextValue text ? text.Value : null;

    static string GeneratedSpecification((string Name, Fact[] Facts) scenario, string expected)
    {
        var (name, facts) = scenario;
        var seeds = string.Join('\n', facts.Select(fact => $"await scenario.Given.ForEventSource(new EventSourceId(\"{fact.Source}\")).Events({fact.CSharpEvent()});"));
        return $$"""
            // Copyright (c) Cratis. All rights reserved.
            // Licensed under the MIT license. See LICENSE file in the project root for full license information.

            #if DEBUG
            using System.Text.Json;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Testing.ReadModels;
            using Xunit;
            using Projects.Common;
            using Projects.Projects.Registration.RegisterProject;

            namespace Projects.Projects.Registration.ProjectLookup;

            public class when_{{name}}
            {
                static readonly string[] _keys = ["{{First}}", "{{Second}}"];

                [Fact]
                public async Task should_match_screenplay()
                {
                    var scenario = new ReadModelScenario<ProjectSummary>();
            {{seeds}}
                    await scenario.Given.ForEventSource(new EventSourceId("{{Third}}")).Events(new ProjectRegistered(new ProjectId(Guid.Parse("{{Third}}")), new ProjectName("Unused")));
                    var actual = _keys.Select(key =>
                    {
                        var instance = scenario.InstanceForEventSourceId(new EventSourceId(key));
                        if (instance is null) return null;
                        return new { projectId = instance.ProjectId.Value.ToString(), name = instance.Name.Value };
                    });
                    Assert.Equal({{JsonSerializer.Serialize(expected)}}, JsonSerializer.Serialize(actual));
                }
            }
            #endif
            """;
    }

    sealed record Fact(string Event, string Source, string? Id = null)
    {
        public string? Name { get; init; }

        public string ValueFor(string property) => property switch
        {
            "projectId" => Id!,
            "name" => Name!,
            _ => throw new UnknownProjectionProperty(property)
        };

        public string CSharpEvent() => Event switch
        {
            "ProjectRegistered" or "ProjectRenamed" => $"new {Event}(new ProjectId(Guid.Parse(\"{Id}\")), new ProjectName(\"{Name}\"))",
            "ProjectNamed" => $"new ProjectNamed(new ProjectName(\"{Name}\"))",
            _ => throw new UnknownProjectionEvent(Event)
        };
    }

    sealed class UnknownProjectionProperty(string name) : Exception($"Unknown projection property '{name}'.");
    sealed class UnknownProjectionEvent(string name) : Exception($"Unknown projection event '{name}'.");
}
#endif
