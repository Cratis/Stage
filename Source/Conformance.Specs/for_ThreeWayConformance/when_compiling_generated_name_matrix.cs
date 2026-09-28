// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Constraints;
using Cratis.Stage.Specifications;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Crosses the declarations emitted by the semantic renderers with record-synthesized and
/// inherited members, authored siblings and the namespaces in which the declarations live.
/// Every case compiles the real renderer output, even when admission returns no artifacts.
/// </summary>
public class when_compiling_generated_name_matrix
{
    // Record primary constructors synthesize the first six names; the others are inherited or
    // generated methods whose collision depends on whether the authored name is a type or member.
    static readonly string[] RecordMembers = [.. GeneratedPascalCase.RecordMembers.Select(Identifiers.ToCamelCase), "finalize", "wait"];
    static readonly string[] RecordKinds = ["event", "command", "readmodel", "type", "concept"];
    static readonly string[] PropertyKinds = ["event", "command", "readmodel", "type"];
    static readonly string[] ContextualKeywords = [.. Enum.GetValues<SyntaxKind>()
        .Where(SyntaxFacts.IsContextualKeyword).Select(SyntaxFacts.GetText).Where(name => name.Length > 0)];

    const string Source = """
        concept Badge : String
        type ProjectNote
          id String
          name String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                name String
                validate
                  name not empty
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              constraint UniqueName
                unique name on ProjectRegistered
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

    static readonly ExecutableSemanticModel Original = Compile(Source);
    static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.Web);

    public static TheoryData<string, string> TypeRows
    {
        get
        {
            var rows = new TheoryData<string, string>();
            foreach (var kind in RecordKinds)
            {
                foreach (var member in RecordMembers) rows.Add(kind, member);
            }
            foreach (var member in new[] { "define", "clone", "getType" }) rows.Add("projection", member);
            return rows;
        }
    }

    public static TheoryData<string, string> MemberRows
    {
        get
        {
            var rows = new TheoryData<string, string>();
            foreach (var kind in PropertyKinds)
            {
                // Authored siblings differ ordinally but normalize to the same emitted member.
                var sibling = kind switch
                {
                    "event" or "command" => "project_id",
                    "readmodel" => "key_",
                    _ => "id_"
                };
                var emittedType = kind switch
                {
                    "event" => "projectRegistered",
                    "command" => "registerProject",
                    "readmodel" => "projectSummary",
                    _ => "projectNote"
                };
                string[] methods = kind switch
                {
                    "command" => ["handle", "getEventSourceId"],
                    "readmodel" => ["projectById"],
                    _ => []
                };
                foreach (var member in RecordMembers.Append(sibling).Append(emittedType).Concat(methods).Concat(ContextualKeywords)) rows.Add(kind, member);
            }
            return rows;
        }
    }

    [Theory]
    [MemberData(nameof(TypeRows))]
    public Task record_and_projection_type_names_match_the_emitted_compilation(string kind, string name) =>
        Verify(Mutate(kind, name, typeName: true), $"type/{kind}/{name}");

    [Theory]
    [MemberData(nameof(MemberRows))]
    public Task record_members_and_siblings_match_the_emitted_compilation(string kind, string name) =>
        Verify(Mutate(kind, name, typeName: false), $"member/{kind}/{name}");

    [Theory]
    [InlineData("command", "registerProject")]
    [InlineData("event", "registerProject")]
    [InlineData("event", "registerProjectValidator")]
    [InlineData("projection", "projectSummary")]
    [InlineData("readmodel", "projectSummaryProjection")]
    [InlineData("concept", "projectNote")]
    [InlineData("type", "badge")]
    [InlineData("event", "projectRegistered")]
    [InlineData("concept", "badgeValidator")]
    [InlineData("constraint", "registerProject")]
    [InlineData("constraint", "registerProjectValidator")]
    [InlineData("constraint", "projectRegistered")]
    public Task cross_kind_and_same_namespace_type_names_match_the_emitted_compilation(string kind, string name) =>
        Verify(Mutate(kind, name, typeName: true), $"sibling/{kind}/{name}");

    [Theory]
    [InlineData("define")]
    [InlineData("uniqueName")]
    [InlineData("iConstraint")]
    [InlineData("iConstraintBuilder")]
    public Task constraint_type_names_match_the_emitted_compilation(string name) =>
        Verify(Mutate("constraint", name, typeName: true), $"constraint/{name}");

    [Theory]
    [InlineData("Guid", SemanticPrimitiveType.Uuid)]
    [InlineData("DateOnly", SemanticPrimitiveType.Date)]
    [InlineData("DateTimeOffset", SemanticPrimitiveType.DateTime)]
    [InlineData("String", SemanticPrimitiveType.Text)]
    [InlineData("Task", SemanticPrimitiveType.Text)]
    [InlineData("EventSourceId", SemanticPrimitiveType.Text)]
    public Task bcl_shadowing_concepts_match_the_emitted_compilation(string name, SemanticPrimitiveType primitive)
    {
        var model = Mutate("concept", name, typeName: true);
        var application = model.Application;
        return Verify(
            ExecutableSemanticModel.Create(
                model.LanguageVersion,
                model.SemanticVersion,
                application with { Concepts = [.. application.Concepts.Select(concept => concept with { Primitive = primitive })] }),
            $"bcl/concept/{name}");
    }

    [Theory]
    [InlineData("Guid")]
    [InlineData("String")]
    [InlineData("DateTimeOffset")]
    [InlineData("Task")]
    [InlineData("EventSourceId")]
    public Task bcl_shadowing_composite_types_match_the_emitted_compilation(string name) =>
        Verify(Mutate("type", name, typeName: true), $"bcl/type/{name}");

    [Theory]
    [InlineData("type")]
    [InlineData("event")]
    public Task empty_records_named_deconstruct_are_legal(string kind)
    {
        var model = kind == "event"
            ? Compile(Source.Replace("      event ProjectRegistered", "      event Deconstruct\n      event ProjectRegistered", StringComparison.Ordinal))
            : Mutate(kind, "deconstruct", typeName: true);
        var application = model.Application;
        if (kind == "type")
        {
            model = ExecutableSemanticModel.Create(
                model.LanguageVersion,
                model.SemanticVersion,
                application with { Types = [.. application.Types.Select(type => type with { Properties = [] })] });
        }
        return Verify(model, $"empty/{kind}");
    }

    public static TheoryData<string> QueryNameRows
    {
        get
        {
            var rows = new TheoryData<string>();
            foreach (var name in RecordMembers.Concat(["projectSummary", "projectById", "name"])) rows.Add(name);
            return rows;
        }
    }

    [Theory]
    [MemberData(nameof(QueryNameRows))]
    public Task query_method_names_match_the_emitted_compilation(string name)
    {
        var application = Original.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Queries = [.. slice.Queries.Select(query => query with { Name = name })]
            })]
        };
        var model = ExecutableSemanticModel.Create(
            Original.LanguageVersion,
            Original.SemanticVersion,
            application with { Modules = [module with { Features = [changed] }] });
        return Verify(model, $"queryname/{name}");
    }

    [Theory]
    [MemberData(nameof(ContextualArgumentRows))]
    public Task contextual_keywords_as_properties_and_query_arguments_compile(string name)
    {
        var model = Original;
        var application = model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var updated = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                ReadModels = [.. slice.ReadModels.Select(readModel => readModel with
                {
                    Properties = [.. readModel.Properties.Select(property => property.Name == "key" ? property with { Name = name } : property)]
                })],
                Queries = [.. slice.Queries.Select(query => query with { Argument = query.Argument with { Name = name } })]
            })]
        };
        return Verify(
            ExecutableSemanticModel.Create(
                model.LanguageVersion,
                model.SemanticVersion,
                application with { Modules = [module with { Features = [updated] }] }),
            $"contextual/{name}");
    }

    public static TheoryData<string> ContextualArgumentRows
    {
        get
        {
            var rows = new TheoryData<string>();
            foreach (var name in ContextualKeywords.Distinct(StringComparer.Ordinal)) rows.Add(name);
            return rows;
        }
    }

    [Theory]
    [InlineData("ProjectBy_Id", "key")]
    [InlineData("projectById", "key")]
    [InlineData("AnotherQuery", "readModels")]
    [InlineData("AnotherQuery", "read_models")]
    [InlineData("AnotherQuery", "key")]
    [InlineData("AnotherQuery", "await")]
    public Task query_signatures_and_generated_parameters_match_the_emitted_compilation(
        string name, string argument)
    {
        var application = Original.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Queries = [.. slice.Queries, .. slice.Queries.Select(query => query with
                {
                    Id = SemanticId.Parse("sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                    Name = name,
                    Argument = query.Argument with
                    {
                        Id = SemanticId.Parse("sem1:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                        Name = argument
                    }
                })]
            })]
        };
        var model = ExecutableSemanticModel.Create(
            Original.LanguageVersion,
            Original.SemanticVersion,
            application with { Modules = [module with { Features = [changed] }] });
        return Verify(model, $"query/{name}/{argument}");
    }

    [Theory]
    [InlineData("event", "ProjectId")]
    [InlineData("readmodel", "ProjectId")]
    [InlineData("readmodel", "ProjectRegistered")]
    [InlineData("readmodel", "ProjectLookup")]
    [InlineData("event", "ProjectLookup")]
    public Task cross_namespace_shadowing_matches_the_emitted_compilation(string kind, string name)
    {
        var source = Source.Replace("concept Badge : String", "concept Badge : String\nconcept ProjectId : String", StringComparison.Ordinal)
            .Replace("projectId String", "projectId ProjectId", StringComparison.Ordinal)
            .Replace("key String", "key ProjectId", StringComparison.Ordinal);
        var model = Compile(source);
        var application = model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var updated = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Events = [.. slice.Events.Select(@event => kind == "event" ? @event with { Name = name } : @event)],
                ReadModels = [.. slice.ReadModels.Select(readModel => kind == "readmodel" ? readModel with { Name = name } : readModel)]
            })]
        };
        return Verify(
            ExecutableSemanticModel.Create(
                model.LanguageVersion,
                model.SemanticVersion,
                application with { Modules = [module with { Features = [updated] }] }),
            $"crossnamespace/{kind}/{name}");
    }

    [Fact]
    public Task a_flat_read_model_member_cannot_shadow_its_source_event_in_nameof()
    {
        var source = Source.Replace("concept Badge : String", "concept Badge : String\nconcept ProjectId : String", StringComparison.Ordinal)
            .Replace("projectId String", "projectId ProjectId", StringComparison.Ordinal)
            .Replace("key String", "key ProjectId", StringComparison.Ordinal);
        var original = Compile(source);
        var application = original.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var @event = feature.Slices.SelectMany(slice => slice.Events).Single();
        var eventId = @event.Properties.Single(property => property.Name == "projectId");
        var eventName = @event.Properties.Single(property => property.Name == "name");
        var view = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView);
        var readModel = view.ReadModels.Single();
        var modelId = readModel.Properties.Single(property => property.Name == "key");
        var modelName = readModel.Properties.Single(property => property.Name == "name");
        var key = new SemanticResolvedExpression(SemanticExpressionRootKind.Event, SemanticExpressionSourceKind.Property, eventId.Id);
        var sourceName = new SemanticResolvedExpression(SemanticExpressionRootKind.Event, SemanticExpressionSourceKind.Property, eventName.Id);
        var transition = new SemanticProjectionTransition(
            @event.Id,
            new(AffectedInstanceCardinality.One, key),
            [new(modelId.Id, key), new(modelName.Id, sourceName)]);
        var updated = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice.Id != view.Id ? slice : slice with
            {
                ReadModels = [readModel with
                {
                    Properties = [.. readModel.Properties.Select(property => property.Id == modelName.Id
                        ? property with { Name = "projectRegistered" } : property)]
                }],
                Projections = [view.Projections.Single() with { Scope = null, Transitions = [transition] }]
            })]
        };
        var model = ExecutableSemanticModel.Create(
            original.LanguageVersion,
            original.SemanticVersion,
            application with { Modules = [module with { Features = [updated] }] });
        return Verify(model, "flat/event-shadow");
    }

    [Fact]
    public void occurrence_type_is_qualified_against_a_command_member()
    {
        var source = Source.Replace("        name String\n        validate", "        name String\n        dateTimeOffset String\n        validate", StringComparison.Ordinal)
            .Replace("          name = name\n      constraint", "          name = name\n          occurred = $context.occurred\n      constraint", StringComparison.Ordinal)
            .Replace(
                "      event ProjectRegistered\n        projectId String\n        name String",
                "      event ProjectRegistered\n        projectId String\n        name String\n        occurred DateTime",
                StringComparison.Ordinal);
        var begin = source.IndexOf("      specification RegisteringAProject", StringComparison.Ordinal);
        var end = source.IndexOf("    slice StateView ProjectLookup", StringComparison.Ordinal);
        source = source.Remove(begin, end - begin);
        var model = Compile(source);
        var plan = SemanticExecutionPlan.Compile(model);
        Assert.True(plan.Success, string.Join("; ", plan.Issues));
        var options = new CratisRenderingOptions("Projects", "Projects");
        var request = new ArtifactRenderRequest(
            model,
            plan.Plan!,
            CratisRendering.CreateProfile("Projects", options),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var files = context.SelectedSlices().Select(slice => slice.Slice.Kind == SemanticSliceKind.StateChange
            ? SemanticStateChangeArtifactRenderer.Render(slice, context)
            : SemanticStateViewArtifactRenderer.Render(slice, context)).ToArray();
        Assert.Empty(RenderedOutput.Errors(files));
        Assert.True(CratisRendering.Plan(model, plan.Plan!, request.Scope, options).Success);
        Assert.Contains("global::System.DateTimeOffset.UtcNow", files[0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public Task an_unreferenced_event_in_the_specification_slice_still_blocks_the_run()
    {
        var source = Source.Replace(
            "      specification RegisteringAProject",
            "      event Unreached\n        toString String\n      specification RegisteringAProject",
            StringComparison.Ordinal);
        return Verify(Compile(source), "unreached/event");
    }

    [Theory]
    [InlineData("event")]
    [InlineData("readmodel")]
    public Task case_only_serialization_collisions_fail_even_though_the_generated_records_compile(string kind)
    {
        var application = Original.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var newId = SemanticId.Parse("sem1:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc");
        var sourceName = feature.Slices.SelectMany(slice => slice.Events).Single().Properties.Single(property => property.Name == "name").Id;
        var readModelName = feature.Slices.SelectMany(slice => slice.ReadModels).Single().Properties.Single(property => property.Name == "name").Id;
        SemanticSpecification ChangeSpec(SemanticSpecification specification)
        {
            if (kind == "event")
            {
                return specification with
                {
                    ThenEvents = [.. specification.ThenEvents.Select(assertion => assertion with
                    {
                        Values = [.. assertion.Values, new(newId, assertion.Values.Single(value => value.TargetProperty == sourceName).Value)]
                    })]
                };
            }
            if (kind == "readmodel")
            {
                return specification with
                {
                    ThenReadModels = [.. specification.ThenReadModels.Select(assertion => assertion with
                    {
                        Values = [.. assertion.Values, new(newId, assertion.Values.Single(value => value.TargetProperty == readModelName).Value)]
                    })]
                };
            }
            return specification;
        }
        var changed = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Events = [.. slice.Events.Select(@event => kind != "event" ? @event : @event with
                {
                    Properties = [.. @event.Properties, new(newId, "n_ame", @event.Properties.Single(property => property.Name == "name").Type, false)]
                })],
                Commands = [.. slice.Commands.Select(command => kind != "event" ? command : command with
                {
                    Produces = [.. command.Produces.Select(produced => produced with
                    {
                        Mappings = [.. produced.Mappings, new(newId, produced.Mappings.Single(mapping =>
                            mapping.TargetProperty == sourceName).Source)]
                    })]
                })],
                ReadModels = [.. slice.ReadModels.Select(model => kind != "readmodel" ? model : model with
                {
                    Properties = [.. model.Properties, new(newId, "n_ame", model.Properties.Single(property => property.Name == "name").Type, false)]
                })],
                Specifications = [.. slice.Specifications.Select(ChangeSpec)],
                Projections = [.. slice.Projections.Select(projection => kind != "readmodel" ? projection : projection with
                {
                    Scope = projection.Scope! with
                    {
                        From = [.. projection.Scope.From.Select(block => block with
                        {
                            Mappings = [.. block.Mappings, new(
                                [newId],
                                SemanticProjectionOperation.Set,
                                new SemanticProjectionEventProperty([sourceName]))]
                        })]
                    }
                })]
            })]
        };
        var model = ExecutableSemanticModel.Create(
            Original.LanguageVersion,
            Original.SemanticVersion,
            application with { Modules = [module with { Features = [changed] }] });
        return Verify(model, $"serialization/{kind}");
    }

    static ExecutableSemanticModel Mutate(string kind, string name, bool typeName)
    {
        var application = Original.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        SemanticCommand ChangeCommand(SemanticCommand command)
        {
            if (kind != "command") return command;
            if (typeName) return command with { Name = name };
            return command with { Properties = [.. command.Properties.Select(property => property.Name == "name" ? property with { Name = name } : property)] };
        }
        SemanticEventContract ChangeEvent(SemanticEventContract @event)
        {
            if (kind != "event") return @event;
            if (typeName) return @event with { Name = name };
            return @event with { Properties = [.. @event.Properties.Select(property => property.Name == "name" ? property with { Name = name } : property)] };
        }
        SemanticReadModel ChangeReadModel(SemanticReadModel model)
        {
            if (kind != "readmodel") return model;
            if (typeName) return model with { Name = name };
            return model with { Properties = [.. model.Properties.Select(property => property.Name == "name" ? property with { Name = name } : property)] };
        }
        SemanticCompositeType ChangeType(SemanticCompositeType type)
        {
            if (kind != "type") return type;
            if (typeName) return type with { Name = name };
            return type with { Properties = [.. type.Properties.Select(property => property.Name == "name" ? property with { Name = name } : property)] };
        }
        var updated = feature with
        {
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Commands = [.. slice.Commands.Select(ChangeCommand)],
                Events = [.. slice.Events.Select(ChangeEvent)],
                ReadModels = [.. slice.ReadModels.Select(ChangeReadModel)],
                Projections = [.. slice.Projections.Select(projection => kind == "projection" ? projection with { Name = name } : projection)],
                Constraints = [.. slice.Constraints.Select(constraint => kind == "constraint" ? constraint with { Name = name } : constraint)]
            })]
        };
        application = application with
        {
            Modules = [module with { Features = [updated] }],
            Types = [.. application.Types.Select(ChangeType)],
            Concepts = [.. application.Concepts.Select(concept => kind == "concept" ? concept with { Name = name } : concept)]
        };
        return ExecutableSemanticModel.Create(Original.LanguageVersion, Original.SemanticVersion, application);
    }

    static async Task Verify(ExecutableSemanticModel model, string row)
    {
        var compiled = SemanticExecutionPlan.Compile(model);
        Assert.True(compiled.Success, $"{row}: {string.Join("; ", compiled.Issues)}");
        var plan = compiled.Plan!;
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
            .Concat(model.Application.Types.Select(type => SemanticCommonArtifactRenderer.Render(type, context)))
            .Concat(model.Application.Concepts.Select(concept => SemanticCommonArtifactRenderer.Render(concept, context)))
            .Concat(SemanticCratisAdmission.SelectedConstraints(context, context.SelectedSlices())
                .Select(selected => SemanticConstraintArtifactRenderer.Render(selected.Slice, selected.Constraint, context)))
            .Concat(row.StartsWith("crossnamespace/", StringComparison.Ordinal)
                ? model.Application.Modules.SelectMany(module => module.Features)
                    .SelectMany(feature => feature.Slices)
                    .SelectMany(slice => slice.Specifications)
                    .SelectMany(specification => new[] { SemanticCommandSpecificationRenderer.Render(specification, context) }
                        .Concat(specification.ThenReadModels.Select(expected => SemanticReadModelSpecificationRenderer.Render(specification, expected, context))))
                : [])
            .ToArray();
        var errors = RenderedOutput.Errors(files);
        var serializationFailure = false;
        if (errors.Count == 0 && (row.StartsWith("member/", StringComparison.Ordinal) ||
            row.StartsWith("serialization/", StringComparison.Ordinal)))
        {
            // Serialization uses the same camel-case, case-insensitive names as Chronicle's
            // record materialization. C# accepts Name/NAme, but serialization cannot bind both.
            try
            {
                var assembly = RenderedOutput.Load(files);
                foreach (var type in assembly.GetTypes().Where(type => type.IsClass && type.IsPublic && type.GetConstructors().Length > 0 &&
                    type.GetConstructors()[0].GetParameters().Length > 0 &&
                    type.GetConstructors()[0].GetParameters().All(parameter => parameter.ParameterType == typeof(string))))
                {
                    var instance = Activator.CreateInstance(type, [.. type.GetConstructors()[0].GetParameters().Select(_ => (object)"value")]);
                    _ = JsonSerializer.Serialize(instance, SerializationOptions);
                }
            }
            catch (InvalidOperationException)
            {
                serializationFailure = true;
            }
        }
        if (row.StartsWith("serialization/", StringComparison.Ordinal))
        {
            Assert.Empty(errors);
            Assert.True(serializationFailure, $"{row}: the generated record must be exercised by the serializer");
        }
        var rejected = errors.Count > 0 || serializationFailure;
        var rendered = CratisRendering.Plan(model, plan, request.Scope, options);
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(
            plan, new([plan.Specifications.Values.Single().Id]), new())).Results);
        Assert.True(rejected == !rendered.Success, $"{row}: admission={rendered.Success}; compiler={string.Join("; ", errors)}; serialization={serializationFailure}");
        Assert.Equal(rejected ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed, result.Outcome);
        if (rejected)
        {
            Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" || diagnostic.Code == "STAGE-ESM-017");
        }
    }

    static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("matrix"), "matrix", "Matrix.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return compilation.Value!.Model;
    }
}
#endif
