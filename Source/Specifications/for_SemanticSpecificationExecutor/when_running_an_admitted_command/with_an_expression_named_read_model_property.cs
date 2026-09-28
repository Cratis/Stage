// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_an_expression_named_read_model_property
{
    const string Source = """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                name String
                count Decimal
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
                  count = count
              event ProjectRegistered
                projectId String
                name String
                count Decimal
              specification RegisteringAProject
                when RegisterProject
                  projectId = "project-1"
                  name = "hello"
                  count = 5
                then ProjectRegistered
                  projectId = "project-1"
                  name = "hello"
                  count = 5
                then readmodel ProjectSummary
                  key = "project-1"
                  name = "hello"
                  count = 0
            slice StateView ProjectLookup
              readmodel ProjectSummary
                key String
                name String
                count Decimal
              query ProjectById => ProjectSummary?
                by key String
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key $eventSourceId
                  name = name
                  count = count
        """;

    [Theory]
    [InlineData("name.Length")]
    [InlineData("Name")]
    [InlineData("count()")]
    [InlineData("name[0]")]
    [InlineData("$this")]
    public async Task should_refuse_an_ambiguous_target_instead_of_passing_an_incorrect_value(string targetName)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("target"), "target", "Target.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compilation.Value!.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                ReadModels = [.. slice.ReadModels.Select(readModel => readModel with
                {
                    Properties = [.. readModel.Properties.Select(property => property.Name == "count" ? property with { Name = targetName } : property)]
                })]
            })]
        };
        var compiled = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(
            model.LanguageVersion,
            model.SemanticVersion,
            model.Application with { Modules = [module with { Features = [changed] }] }));
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var specification = compiled.Plan!.Specifications.Values.Single(value => value.Name == "RegisteringAProject");
        var reference = new SemanticSpecificationRunner().Run(compiled.Plan, specification.Id);
        var stage = Assert.Single((await new SemanticSpecificationExecutor().Run(compiled.Plan, new([specification.Id]), new())).Results);

        Assert.False(reference.Passed);
        Assert.True(stage.Outcome == SemanticSpecificationOutcome.Unsupported,
            $"Stage: {stage.Outcome}; {string.Join("; ", stage.Failures)}; reference: {string.Join("; ", reference.Failures)}");
        Assert.Equal(StageExecutionCapability.Projection, stage.Unsupported?.Capability);
        Assert.Contains("read-model property name", stage.Unsupported?.Details ?? "", StringComparison.Ordinal);
    }
}
