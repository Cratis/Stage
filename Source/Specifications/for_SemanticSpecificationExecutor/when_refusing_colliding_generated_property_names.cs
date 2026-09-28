// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Admission;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_colliding_generated_property_names
{
    const string Source = """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                displayName String
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  displayName = displayName
              event ProjectRegistered
                projectId String
                displayName String
              specification RegisteringAProject
                when RegisterProject
                  projectId = "project-1"
                  displayName = "Name"
                then ProjectRegistered
                  projectId = "project-1"
                  displayName = "Name"
        """;

    [Theory]
    [InlineData("event", "project_id")]
    [InlineData("command", "project_id")]
    [InlineData("event", "projectRegistered")]
    [InlineData("command", "registerProject")]
    [InlineData("command", "handle")]
    [InlineData("command", "getEventSourceId")]
    [InlineData("event", "equalityContract")]
    [InlineData("event", "toString")]
    [InlineData("event", "equals")]
    [InlineData("event", "getHashCode")]
    [InlineData("event", "deconstruct")]
    [InlineData("event", "printMembers")]
    public async Task should_refuse_a_colliding_event_or_command_before_running(string kind, string propertyName)
    {
        var model = Compile(Source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var changed = slice with
        {
            Events = kind == "event" ? [.. slice.Events.Select(@event => @event with
            {
                Properties = [.. @event.Properties.Select(property => property.Name == "displayName" ? property with { Name = propertyName } : property)]
            })] : slice.Events,
            Commands = kind == "command" ? [.. slice.Commands.Select(command => command with
            {
                Properties = [.. command.Properties.Select(property => property.Name == "displayName" ? property with { Name = propertyName } : property)]
            })] : slice.Commands
        };
        var changedModel = ExecutableSemanticModel.Create(
            model.LanguageVersion,
            model.SemanticVersion,
            model.Application with { Modules = [module with { Features = [feature with { Slices = [changed] }] }] });
        var execution = SemanticExecutionPlan.Compile(changedModel);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        var specification = execution.Plan!.Specifications.Values.Single();

        var blocker = SemanticRunAdmission.Check(execution.Plan!, specification);
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(execution.Plan!, new([specification.Id]), new())).Results);
        Assert.Equal(StageExecutionCapability.Command, blocker?.Capability);
        Assert.Equal(SemanticSpecificationOutcome.Unsupported, result.Outcome);
        Assert.Contains("generated C#", result.Unsupported?.Details ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_refuse_a_direct_append_with_colliding_event_names()
    {
        var model = Compile(Source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var @event = slice.Events.Single();
        var specification = slice.Specifications.Single();
        var sourceType = slice.Commands.Single().Properties.Single(property => property.IsIdentifier).Type;
        var changed = slice with
        {
            Events = [@event with { Properties = [.. @event.Properties.Select(property => property.Name == "displayName" ? property with { Name = "project_id" } : property)] }],
            Specifications = [specification with
            {
                When = null,
                WhenAppended = new(@event.Id, specification.ThenEvents.Single().Values)
                {
                    EventSource = new(sourceType, SemanticValue.Text("project-1"))
                }
            }]
        };
        var changedModel = ExecutableSemanticModel.Create(
            LanguageVersion.V2,
            SemanticVersion.V2,
            model.Application with { Modules = [module with { Features = [feature with { Slices = [changed] }] }] });
        var execution = SemanticExecutionPlan.Compile(changedModel);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(execution.Plan!, new([specification.Id]), new())).Results);

        Assert.Equal(SemanticSpecificationOutcome.Unsupported, result.Outcome);
        Assert.Equal(StageExecutionCapability.Command, result.Unsupported?.Capability);
        Assert.Contains("generated C#", result.Unsupported?.Details ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void should_admit_distinct_safe_names()
    {
        var model = Compile(Source.Replace("displayName", "project_title", StringComparison.Ordinal));
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        Assert.Null(SemanticRunAdmission.Check(execution.Plan!, execution.Plan!.Specifications.Values.Single()));
    }

    static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("collision-run"), "collision-run", "CollisionRun.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return compilation.Value!.Model;
    }
}
