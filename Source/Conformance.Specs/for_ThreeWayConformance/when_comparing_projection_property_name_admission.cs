// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Exercises name admission through both independent consumers, not merely their shared normalization helper.
/// </summary>
public class when_comparing_projection_property_name_admission
{
    const string Source = """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                name String
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId String
                name String
              specification RegisteringAProject
                when RegisterProject
                  projectId = "project-1"
                  name = "hello"
                then ProjectRegistered
                  projectId = "project-1"
                  name = "hello"
                then readmodel ProjectSummary
                  key = "project-1"
                  name = "hello"
            slice StateView ProjectLookup
              readmodel ProjectSummary
                key String
                name String
              query ProjectById => ProjectSummary?
                by key String
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key $eventSourceId
                  name = name
        """;

    [Theory]
    [InlineData("event", "True", "True", false)]
    [InlineData("event", "true_", "True", false)]
    [InlineData("event", "_true", "True", false)]
    [InlineData("event", "False", "False", false)]
    [InlineData("event", "_false", "False", false)]
    [InlineData("event", "week_", "Week", false)]
    [InlineData("event", "Week", "Week", false)]
    [InlineData("event", "Week()", "Week()", false)]
    [InlineData("event", "projectName", "ProjectName", true)]
    [InlineData("root", "id_", "Id", false)]
    [InlineData("root", "_id", "Id", false)]
    [InlineData("root", "id-", "Id", false)]
    [InlineData("root", "Id", "Id", false)]
    [InlineData("root", "key_", "Key", false)]
    [InlineData("root", "projectId", "ProjectId", true)]
    [InlineData("child", "id", "Id", true)]
    public async Task should_agree_on_generated_names_and_refuse_chronicle_expressions_or_the_document_key(
        string path, string authored, string generated, bool safe)
    {
        if (path == "child")
        {
            Assert.Equal("id", authored);
            Assert.Equal("Id", generated);
            Assert.True(safe);
            await VerifyChildId();
            return;
        }

        var declaredInPlay = (path, authored) is ("event", "true_") or ("root", "id_");
        var source = Source;
        if (declaredInPlay && path == "event")
        {
            source = source.Replace("name String\n      specification", "true_ String\n      specification", StringComparison.Ordinal)
                .Replace("name = name\n      event", "true_ = name\n      event", StringComparison.Ordinal)
                .Replace("then ProjectRegistered\n          projectId = \"project-1\"\n          name =", "then ProjectRegistered\n          projectId = \"project-1\"\n          true_ =", StringComparison.Ordinal)
                .Replace("from ProjectRegistered key $eventSourceId\n          name = name", "from ProjectRegistered key $eventSourceId\n          name = true_", StringComparison.Ordinal);
        }
        else if (declaredInPlay)
        {
            source = source.Replace("key = \"project-1\"\n          name = \"hello\"", "key = \"project-1\"\n          id_ = \"hello\"", StringComparison.Ordinal)
                .Replace("key String\n        name String", "key String\n        id_ String", StringComparison.Ordinal)
                .Replace("from ProjectRegistered key $eventSourceId\n          name = name", "from ProjectRegistered key $eventSourceId\n          id_ = name", StringComparison.Ordinal);
        }
        var model = Compile(source);
        if (!declaredInPlay)
        {
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var modified = feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Events = [.. slice.Events.Select(@event => @event with
                    {
                        Properties = [.. @event.Properties.Select(property => path == "event" && property.Name == "name" ? property with { Name = authored } : property)]
                    })],
                    ReadModels = [.. slice.ReadModels.Select(readModel => readModel with
                    {
                        Properties = [.. readModel.Properties.Select(property => path == "root" && property.Name == "name" ? property with { Name = authored } : property)]
                    })]
                })]
            };
            model = ExecutableSemanticModel.Create(
                model.LanguageVersion,
                model.SemanticVersion,
                model.Application with { Modules = [module with { Features = [modified] }] });
        }

        Assert.Equal(generated, Identifiers.ToPascalCase(authored));
        var view = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView);
        var eventContract = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single();
        Assert.True(path == "event" ? eventContract.Properties.Any(property => property.Name == authored) :
            view.ReadModels.Single().Properties.Any(property => property.Name == authored && !property.IsIdentifier),
            $"{path}/{authored}: event [{string.Join(", ", eventContract.Properties.Select(property => property.Name))}], model [{string.Join(", ", view.ReadModels.Single().Properties.Select(property => $"{property.Name}/{property.IsIdentifier}"))}]");
        var compiled = SemanticExecutionPlan.Compile(model);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var plan = compiled.Plan!;
        var specification = plan.Specifications.Values.Single(value => value.Name == "RegisteringAProject");
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        var executed = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);

        Assert.Equal(safe, rendered.Success);
        Assert.Equal(safe, executed.Outcome == SemanticSpecificationOutcome.Passed);
        if (!safe)
        {
            Assert.Empty(rendered.Artifacts);
            Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017");
            Assert.Equal(SemanticSpecificationOutcome.Unsupported, executed.Outcome);
            Assert.Equal(StageExecutionCapability.Projection, executed.Unsupported?.Capability);
        }
    }

    static async Task VerifyChildId()
    {
        const string childSource = """
            type ProjectNote
              id String
              name String
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId String identifier
                    name String
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      name = name
                  event ProjectRegistered
                    projectId String
                    name String
                  event ProjectNoted
                    id String
                    projectId String
                    name String
                  specification RegisteringAProject
                    when RegisterProject
                      projectId = "project-1"
                      name = "hello"
                    then ProjectRegistered
                      projectId = "project-1"
                      name = "hello"
                    then readmodel ProjectSummary
                      key = "project-1"
                      name = "hello"
                slice StateView ProjectLookup
                  readmodel ProjectSummary
                    key String
                    name String
                    notes ProjectNote[]
                  query ProjectById => ProjectSummary?
                    by key String
                  projection ProjectSummaryProjection => ProjectSummary
                    from ProjectRegistered key $eventSourceId
                      name = name
                    children notes identified by id
                      from ProjectNoted key id
                        parent projectId
                        name = name
            """;
        var model = Compile(childSource);
        var compiled = SemanticExecutionPlan.Compile(model);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var plan = compiled.Plan!;
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        var specification = plan.Specifications.Values.Single(value => value.Name == "RegisteringAProject");
        var executed = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);

        // Child projections are renderable, but per-run execution currently admits only scalar read models.
        // The renderer's generated child-id integration spec exercises the actual Chronicle projection.
        Assert.True(rendered.Success, string.Join("; ", rendered.Diagnostics));
        Assert.Equal(SemanticSpecificationOutcome.Unsupported, executed.Outcome);
        Assert.Equal(StageExecutionCapability.Projection, executed.Unsupported?.Capability);
        Assert.Contains("scalar read model", executed.Unsupported?.Details ?? "", StringComparison.Ordinal);
    }

    static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("names"), "names", "Names.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return compilation.Value!.Model;
    }
}
#endif
