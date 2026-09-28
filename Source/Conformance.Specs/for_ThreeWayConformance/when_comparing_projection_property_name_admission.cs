// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
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
    [InlineData("root", "ID", "ID", false)]
    [InlineData("root", "iD", "ID", false)]
    [InlineData("root", "i_d", "ID", false)]
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

    [Theory]
    [InlineData("display_name")]
    [InlineData("projectUpdated")]
    [InlineData("toString")]
    public async Task should_refuse_an_unreached_projection_event_with_colliding_generated_properties(string propertyName)
    {
        var source = Source.Replace(
                "      specification RegisteringAProject",
                "      event ProjectUpdated\n        projectId String\n        displayName String\n        otherName String\n      specification RegisteringAProject",
                StringComparison.Ordinal)
            .Replace(
                "from ProjectRegistered key $eventSourceId\n          name = name",
                "from ProjectRegistered key $eventSourceId\n          name = name\n        from ProjectUpdated key $eventSourceId\n          name = displayName",
                StringComparison.Ordinal);
        var model = Compile(source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var modified = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Events = [.. slice.Events.Select(@event => @event with
                {
                    Properties = [.. @event.Properties.Select(property => @event.Name == "ProjectUpdated" && property.Name == "otherName"
                        ? property with { Name = propertyName } : property)]
                })]
            })]
        };
        model = ExecutableSemanticModel.Create(
            model.LanguageVersion,
            model.SemanticVersion,
            model.Application with { Modules = [module with { Features = [modified] }] });
        var compiled = SemanticExecutionPlan.Compile(model);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var plan = compiled.Plan!;
        var specification = plan.Specifications.Values.Single(value => value.Name == "RegisteringAProject");
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        var executed = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);

        Assert.False(rendered.Success);
        Assert.Empty(rendered.Artifacts);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" &&
            diagnostic.Artifact == plan.Projections.Values.Single().Id);
        Assert.Equal(SemanticSpecificationOutcome.Unsupported, executed.Outcome);
        Assert.Equal(StageExecutionCapability.Projection, executed.Unsupported?.Capability);
        Assert.Contains("generated C#", executed.Unsupported?.Details ?? "", StringComparison.Ordinal);
    }

    // These rows compile the source emitted by the actual slice renderers, including rejected models
    // (the planner deliberately returns no artifacts once admission finds a collision).
    [Theory]
    [InlineData("event", "project_id", true)]
    [InlineData("event", "projectRegistered", true)]
    [InlineData("event", "toString", true)]
    [InlineData("event", "equalityContract", true)]
    [InlineData("event", "deconstruct", true)]
    [InlineData("event", "printMembers", true)]
    [InlineData("event", "getHashCode", true)]
    [InlineData("event", "equals", true)]
    [InlineData("event", "clone", true)]
    [InlineData("event", "getType", true)]
    [InlineData("event", "memberwiseClone", true)]
    [InlineData("event", "referenceEquals", true)]
    [InlineData("event", "finalize", false)]
    [InlineData("event", "Project_Id", true)]
    [InlineData("command", "project_id", true)]
    [InlineData("command", "registerProject", true)]
    [InlineData("command", "handle", true)]
    [InlineData("command", "getEventSourceId", true)]
    [InlineData("command", "clone", true)]
    [InlineData("command", "getType", true)]
    [InlineData("command", "finalize", false)]
    [InlineData("readmodel", "projectSummary", true)]
    [InlineData("readmodel", "projectById", true)]
    [InlineData("readmodel", "toString", true)]
    [InlineData("readmodel", "equalityContract", true)]
    [InlineData("readmodel", "deconstruct", true)]
    [InlineData("readmodel", "printMembers", true)]
    [InlineData("readmodel", "getHashCode", true)]
    [InlineData("readmodel", "equals", true)]
    [InlineData("readmodel", "clone", true)]
    [InlineData("readmodel", "getType", true)]
    [InlineData("readmodel", "memberwiseClone", true)]
    [InlineData("readmodel", "referenceEquals", true)]
    [InlineData("readmodel", "finalize", false)]
    [InlineData("readmodel", "id_", false, true)]
    [InlineData("readmodel", "ID", false, true)]
    [InlineData("readmodel", "i_d", false, true)]
    [InlineData("query", "name", true)]
    [InlineData("query", "projectSummary", true)]
    [InlineData("query", "clone", true)]
    [InlineData("query", "equalityContract", true)]
    [InlineData("query", "toString", false)]
    [InlineData("query", "getType", false)]
    [InlineData("query", "equals", false)]
    [InlineData("query", "getHashCode", false)]
    [InlineData("query", "deconstruct", false)]
    [InlineData("query", "printMembers", false)]
    [InlineData("query", "memberwiseClone", false)]
    [InlineData("query", "referenceEquals", false)]
    [InlineData("query", "finalize", false)]
    [InlineData("query", "ProjectBy_ID", false)]
    public async Task should_admit_exactly_the_generated_members_that_compile_and_run(
        string kind,
        string authored,
        bool collision,
        bool chronicleOnly = false)
    {
        var original = Compile(Source);
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Commands = [.. slice.Commands.Select(command => command with
                {
                    Properties = [.. command.Properties.Select(property => kind == "command" && property.Name == "name" ? property with { Name = authored } : property)]
                })],
                Events = [.. slice.Events.Select(@event => @event with
                {
                    Properties = [.. @event.Properties.Select(property => kind == "event" && property.Name == "name" ? property with { Name = authored } : property)]
                })],
                ReadModels = [.. slice.ReadModels.Select(readModel => readModel with
                {
                    Properties = [.. readModel.Properties.Select(property => kind == "readmodel" && property.Name == "name" ? property with { Name = authored } : property)]
                })],
                Queries = [.. slice.Queries.Select(query => kind == "query" ? query with { Name = authored } : query)]
            })]
        };
        var model = ExecutableSemanticModel.Create(
            original.LanguageVersion,
            original.SemanticVersion,
            original.Application with { Modules = [module with { Features = [changed] }] });
        var compiled = SemanticExecutionPlan.Compile(model);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var plan = compiled.Plan!;
        var specification = plan.Specifications.Values.Single();
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        var executed = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);

        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(
            model,
            plan,
            CratisRendering.CreateProfile("Projects", options),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var files = context.SelectedSlices().Select(slice => slice.Slice.Kind == SemanticSliceKind.StateChange
            ? SemanticStateChangeArtifactRenderer.Render(slice, context)
            : SemanticStateViewArtifactRenderer.Render(slice, context)).ToArray();
        var errors = RenderedOutput.Errors(files);
        Assert.Equal(collision, errors.Count > 0);
        if (!collision)
        {
            Assert.Empty(RenderedOutput.Warnings(files));
        }

        Assert.Equal(!(collision || chronicleOnly), rendered.Success);
        Assert.Equal(!(collision || chronicleOnly), executed.Outcome == SemanticSpecificationOutcome.Passed);
        if (collision || chronicleOnly)
        {
            Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == (chronicleOnly ? "STAGE-ESM-017" : "STAGE-ESM-012"));
            Assert.Equal(SemanticSpecificationOutcome.Unsupported, executed.Outcome);
        }
    }

    [Theory]
    [InlineData("Value", false, false)]
    [InlineData("NotSet", true, false)]
    [InlineData("Badge", false, true)]
    [InlineData("Badge_", false, true)]
    [InlineData("Status", false, true)]
    [InlineData("Duplicate", true, true)]
    public async Task should_match_roslyn_for_generated_concept_members(string authored, bool collision, bool isEnum)
    {
        var original = Compile("concept Badge : String\n" + Source);
        var changed = original.Application.Concepts.Single() with
        {
            Name = authored,
            Values = (isEnum, authored) switch
            {
                (false, _) => [],
                (true, "Duplicate") => ["Badge", "Badge_"],
                _ => ["Badge"]
            }
        };
        var model = ExecutableSemanticModel.Create(
            original.LanguageVersion,
            original.SemanticVersion,
            original.Application with { Concepts = [changed] });
        var compiled = SemanticExecutionPlan.Compile(model);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var plan = compiled.Plan!;
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        var executed = Assert.Single((await new SemanticSpecificationExecutor().Run(
            plan,
            new([plan.Specifications.Values.Single().Id]),
            new())).Results);
        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(
            model,
            plan,
            CratisRendering.CreateProfile("Projects", options),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        Assert.Equal(collision, RenderedOutput.Errors([SemanticCommonArtifactRenderer.Render(changed, context)]).Count > 0);
        Assert.Equal(!collision, rendered.Success);
        Assert.Equal(!collision, executed.Outcome == SemanticSpecificationOutcome.Passed);
        if (collision)
        {
            Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == changed.Id);
        }
    }

    [Theory]
    [InlineData("projectNote")]
    [InlineData("clone")]
    [InlineData("getType")]
    public async Task should_agree_on_child_and_nested_record_type_members(string authored)
    {
        var original = Compile("type ProjectNote\n  id String\n  name String\n" + Source);
        var type = original.Application.Types.Single();
        var changed = type with
        {
            Properties = [.. type.Properties.Select(property => property.Name == "name" ? property with { Name = authored } : property)]
        };
        var model = ExecutableSemanticModel.Create(
            original.LanguageVersion,
            original.SemanticVersion,
            original.Application with { Types = [changed] });
        var compiled = SemanticExecutionPlan.Compile(model);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var plan = compiled.Plan!;
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        var executed = Assert.Single((await new SemanticSpecificationExecutor().Run(
            plan,
            new([plan.Specifications.Values.Single().Id]),
            new())).Results);
        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(
            model,
            plan,
            CratisRendering.CreateProfile("Projects", options),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        Assert.NotEmpty(RenderedOutput.Errors([SemanticCommonArtifactRenderer.Render(changed, context)]));
        Assert.False(rendered.Success);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == changed.Id);
        Assert.Equal(SemanticSpecificationOutcome.Unsupported, executed.Outcome);
    }

    public static TheoryData<string, string> NestedEventMembers
    {
        get
        {
            var rows = new TheoryData<string, string>();
            foreach (var scope in new[] { "children", "nested" })
            {
                foreach (var name in new[]
                {
                    "project_id", "toString", "equals", "getHashCode", "equalityContract", "deconstruct", "printMembers",
                    "title"
                })
                {
                    rows.Add(scope, name);
                }
            }
            return rows;
        }
    }

    [Theory]
    [MemberData(nameof(NestedEventMembers))]
    public async Task should_reject_cross_slice_events_referenced_only_below_the_root(string scope, string propertyName)
    {
        var source = """
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
                    info ProjectNote?
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
        if (scope == "nested")
        {
            source = source.Replace(
                "        children notes identified by id\n          from ProjectNoted key id\n            parent projectId\n            name = name",
                "        from ProjectNoted key $eventSourceId\n          name = name\n        nested info\n          from ProjectNoted key $eventSourceId\n            name = name",
                StringComparison.Ordinal);
        }
        var original = Compile(source);
        var declaredScope = original.Application.Modules.Single().Features.Single().Slices
            .Single(slice => slice.Kind == SemanticSliceKind.StateView).Projections.Single().Scope!;
        Assert.Equal(scope == "nested", !declaredScope.Nested.IsEmpty);
        Assert.Equal(scope == "children", !declaredScope.Children.IsEmpty);
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Events = [.. slice.Events.Select(@event => @event with
                {
                    Properties = [.. @event.Properties.Select(property => @event.Name == "ProjectNoted" && property.Name == "name"
                        ? property with { Name = propertyName } : property)]
                })]
            })]
        };
        var model = ExecutableSemanticModel.Create(
            original.LanguageVersion,
            original.SemanticVersion,
            original.Application with { Modules = [module with { Features = [changed] }] });
        var compiled = SemanticExecutionPlan.Compile(model);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        var plan = compiled.Plan!;
        var view = changed.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView);
        var rendered = CratisRendering.Plan(model, plan, new(ArtifactRenderScopeKind.Slice, view.Id), new("Projects", "Projects"));
        var executed = Assert.Single((await new SemanticSpecificationExecutor().Run(
            plan,
            new([plan.Specifications.Values.Single().Id]),
            new())).Results);

        var collides = propertyName != "title";
        Assert.True(!collides == rendered.Success, $"{scope}/{propertyName}: {string.Join("; ", rendered.Diagnostics)}");
        if (collides)
        {
            Assert.Empty(rendered.Artifacts);
            Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" &&
                diagnostic.Artifact == plan.Projections.Values.Single().Id);
            Assert.Contains("generated C#", executed.Unsupported?.Details ?? string.Empty, StringComparison.Ordinal);
        }

        // Child/nested execution is not admitted even when its generated C# compiles.
        Assert.Equal(SemanticSpecificationOutcome.Unsupported, executed.Outcome);

        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(
            model,
            plan,
            CratisRendering.CreateProfile("Projects", options),
            new(ArtifactRenderScopeKind.Slice, view.Id));
        var context = new SemanticApplicationContext(request, options);
        var rawEventSource = SemanticStateChangeArtifactRenderer.Render(
            context.DeclaringSlice(context.Events.Values.Single(@event => @event.Name == "ProjectNoted").Id), context);
        Assert.Equal(collides, RenderedOutput.Errors([rawEventSource]).Count > 0);
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
        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(
            model,
            plan,
            CratisRendering.CreateProfile("Projects", options),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var files = context.SelectedSlices().Select(slice => slice.Slice.Kind == SemanticSliceKind.StateChange
            ? SemanticStateChangeArtifactRenderer.Render(slice, context)
            : SemanticStateViewArtifactRenderer.Render(slice, context))
            .Concat(model.Application.Types.Select(type => SemanticCommonArtifactRenderer.Render(type, context)));
        Assert.Empty(RenderedOutput.Errors(files));
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
