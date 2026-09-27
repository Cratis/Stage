// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_an_expression_named_event_property
{
    const string Source = """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                flag Bool
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  eventFlag = flag
              event ProjectRegistered
                projectId String
                eventFlag Bool
              specification RegisteringAProject
                when RegisterProject
                  projectId = "project-1"
                  flag = INPUT
                then ProjectRegistered
                  projectId = "project-1"
                  eventFlag = INPUT
                then readmodel ProjectSummary
                  key = "project-1"
                  flag = EXPECTED
            slice StateView ProjectLookup
              readmodel ProjectSummary
                key String
                flag Bool
              query ProjectById => ProjectSummary?
                by key String
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key $eventSourceId
                  flag = eventFlag
        """;

    [Theory]
    [InlineData("True", "false", "true")]
    [InlineData("False", "true", "false")]
    public async Task should_refuse_a_boolean_literal_instead_of_passing_a_false_assertion(string property, string input, string expected)
    {
        var plan = Compile(property, input, expected);
        var specification = plan.Specifications.Values.Single(value => value.Name == "RegisteringAProject");
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var stage = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);

        Assert.False(reference.Passed);
        Assert.Equal(SemanticSpecificationOutcome.Unsupported, stage.Outcome);
        Assert.Equal(StageExecutionCapability.Projection, stage.Unsupported?.Capability);
        Assert.Contains("Chronicle expression", stage.Unsupported?.Details ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_refuse_a_derived_property_even_when_an_empty_query_would_pass()
    {
        var plan = Compile("Week()", "false", "true");
        var query = plan.Queries.Values.Single(value => value.Name == "ProjectById");
        var original = plan.Specifications.Values.Single(value => value.Name == "RegisteringAProject");
        var specification = original with
        {
            ThenReadModels = [],
            ThenQueries = [new(query.Id, SemanticValue.Text("project-1"), [])]
        };
        var model = plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Specifications = [.. slice.Specifications.Select(value => value.Id == original.Id ? specification : value)]
            })]
        };
        var compiled = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(
            model.LanguageVersion,
            model.SemanticVersion,
            model.Application with { Modules = [module with { Features = [changed] }] }));
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var reference = new SemanticSpecificationRunner().Run(compiled.Plan!, specification.Id);
        var stage = Assert.Single((await new SemanticSpecificationExecutor().Run(compiled.Plan!, new([specification.Id]), new())).Results);

        Assert.False(reference.Passed);
        Assert.True(stage.Outcome == SemanticSpecificationOutcome.Unsupported,
            $"Stage: {stage.Outcome}; {string.Join("; ", stage.Failures)}; reference: {string.Join("; ", reference.Failures)}");
        Assert.Equal(StageExecutionCapability.Projection, stage.Unsupported?.Capability);
    }

    [Fact]
    public async Task should_refuse_the_same_property_in_a_flat_transition()
    {
        var plan = Compile("True", "false", "true");
        var model = plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var @event = feature.Slices.SelectMany(slice => slice.Events).Single();
        var readModel = feature.Slices.SelectMany(slice => slice.ReadModels).Single();
        var key = new SemanticResolvedExpression(
            SemanticExpressionRootKind.Event,
            SemanticExpressionSourceKind.Property,
            @event.Properties.Single(property => property.Name == "projectId").Id);
        var value = new SemanticResolvedExpression(
            SemanticExpressionRootKind.Event,
            SemanticExpressionSourceKind.Property,
            @event.Properties.Single(property => property.Name == "True").Id);
        var transition = new SemanticProjectionTransition(
            @event.Id,
            new(AffectedInstanceCardinality.One, key),
            [new(readModel.Properties.Single(property => property.IsIdentifier).Id, key),
             new(readModel.Properties.Single(property => !property.IsIdentifier).Id, value)]);
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
        {
            Projections = [.. slice.Projections.Select(projection => projection with { Scope = null, Transitions = [transition] })]
        })]
        };
        var updated = ExecutableSemanticModel.Create(
            model.LanguageVersion,
            model.SemanticVersion,
            model.Application with { Modules = [module with { Features = [changed] }] });
        var compiled = SemanticExecutionPlan.Compile(updated);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var specification = compiled.Plan!.Specifications.Values.Single(value => value.Name == "RegisteringAProject");
        var stage = Assert.Single((await new SemanticSpecificationExecutor().Run(compiled.Plan, new([specification.Id]), new())).Results);

        Assert.Equal(SemanticSpecificationOutcome.Unsupported, stage.Outcome);
        Assert.Equal(StageExecutionCapability.Projection, stage.Unsupported?.Capability);
        Assert.Contains("Chronicle expression", stage.Unsupported?.Details ?? "", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("$value(0)")]
    [InlineData("prefix$value(0)")]
    [InlineData("a.b")]
    [InlineData("Week")]
    [InlineData("123")]
    public async Task should_refuse_a_direct_semantic_expression_property(string property)
    {
        var plan = Compile(property, "false", "true");
        var specification = plan.Specifications.Values.Single(value => value.Name == "RegisteringAProject");
        var stage = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);

        Assert.Equal(SemanticSpecificationOutcome.Unsupported, stage.Outcome);
        Assert.Equal(StageExecutionCapability.Projection, stage.Unsupported?.Capability);
        Assert.Contains("Chronicle expression", stage.Unsupported?.Details ?? "", StringComparison.Ordinal);
    }

    static SemanticExecutionPlan Compile(string property, string input, string expected)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = Source.Replace("INPUT", input, StringComparison.Ordinal).Replace("EXPECTED", expected, StringComparison.Ordinal);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("literal"), "literal", "Literal.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));

        // The current .play parser requires a lowercase initial letter in declarations, whereas
        // a direct semantic model can still carry names that Chronicle reads as expressions.
        var model = compilation.Value!.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
        {
            Events = [.. slice.Events.Select(@event => @event with
            {
                Properties = [.. @event.Properties.Select(value => value.Name == "eventFlag" ? value with { Name = property } : value)]
            })]
        })]
        };
        var updated = ExecutableSemanticModel.Create(
            model.LanguageVersion,
            model.SemanticVersion,
            model.Application with { Modules = [module with { Features = [changed] }] });
        var execution = SemanticExecutionPlan.Compile(updated);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        return execution.Plan!;
    }
}
