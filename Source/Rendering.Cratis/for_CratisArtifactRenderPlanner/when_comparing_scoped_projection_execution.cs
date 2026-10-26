// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Compares Chronicle's in-memory projection engine against Screenplay's reference execution over the same facts.
/// </summary>
public class when_comparing_scoped_projection_execution : a_generated_application
{
    const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    const string Second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
    const string Third = "7fa85f64-5717-4562-b3fc-2c963f66afaa";
    const string Note = "5fa85f64-5717-4562-b3fc-2c963f66afa8";
    const string OtherNote = "6fa85f64-5717-4562-b3fc-2c963f66afa9";
    readonly (string Name, Fact[] Facts)[] _cases =
    [
        ("nested_from_then_from", [new("ProjectRegistered", First, First, "First"), new("ProjectRegistered", First, First, "Again")]),
        ("nested_clear", [new("ProjectRegistered", First, First, "First"), new("ProjectRenamed", First, First, "Updated")]),
        ("nested_clear_and_recreation", [new("ProjectRegistered", First, First, "First"), new("ProjectRenamed", First, First, "Updated"), new("ProjectRegistered", First, First, "Again")]),
        ("nested_repeated_clear_and_recreation", [new("ProjectRegistered", First, First, "First"), new("ProjectRenamed", First, First, "Updated"), new("ProjectRenamed", First, First, "Updated again"), new("ProjectRegistered", First, First, "Again")]),
        ("root_removal_and_recreation", [new("ProjectRegistered", First, First, "First"), new("ProjectRemoved", First), new("ProjectRegistered", First, First, "Second")]),
        ("child_removal_existing_and_missing", [new("ProjectRegistered", First, First, "First"), new("ProjectNoted", Note, First, "A", Note), new("ProjectNoteRemoved", Note, First, null, Note), new("ProjectNoteRemoved", OtherNote, First, null, OtherNote)]),
        ("child_removal_via_join", [new("ProjectRegistered", First, First, "First"), new("ProjectRegistered", Second, Second, "Second"), new("ProjectNoted", Note, First, "A", Note), new("ProjectNoted", Note, Second, "B", Note), new("ProjectNoteRemovedViaJoin", Note, null, null, Note)]),
        ("two_parents_with_distinct_children", [new("ProjectRegistered", First, First, "First"), new("ProjectRegistered", Second, Second, "Second"), new("ProjectNoted", Note, First, "A", Note), new("ProjectNoted", OtherNote, Second, "B", OtherNote)]),
        ("every_on_from", [new("ProjectRegistered", First, First, "First"), new("ProjectRenamed", First, First, "Renamed")]),
        ("every_literal_does_not_recreate_removed_root", [new("ProjectRegistered", First, First, "First"), new("ProjectRemoved", First)]),
        ("later_from_keeps_every_literal", [new("ProjectRegistered", First, First, "First"), new("ProjectRenamed", First, First, "Renamed")])
    ];
    ExecutableSemanticModel _model = null!;
    SemanticExecutionPlan _execution = null!;
    string _testOutput = null!;
    string _laterFromExpected = null!;
    string _allExpected = null!;
    string _allLiteralExpected = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));

        // ClearWith binds to the event-source key, so its root from must use the same key.
        var source = every_literal_join_projection.Source;
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("differential"), "differential", "Scopes.play", source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics));
        _model = compiled.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(_model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _execution = execution.Plan!;
        var plan = CratisRendering.Plan(_model, _execution, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Projects", "Projects"));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    async Task Because()
    {
        foreach (var (name, facts) in _cases)
        {
            var expected = Reference(facts);
            if (name == "later_from_keeps_every_literal")
            {
                _laterFromExpected = expected;
            }

            AddGeneratedSpecification($"Projects/Registration/ProjectLookup/when_{name}.cs", GeneratedSpecification(name, facts, expected));
        }

        Fact[] allFacts = [new("ProjectRegistered", First, First, "First"), new("ProjectNamed", Second, Name: "Unrelated")];
        _allExpected = Reference(allFacts, "AllSummary");
        AddGeneratedSpecification("Projects/Registration/AllLookup/when_observing_an_unrelated_event.cs", GeneratedAllSpecification(allFacts, _allExpected));

        _allLiteralExpected = Reference(allFacts, "AllLabelSummary");
        AddGeneratedSpecification("Projects/Registration/AllLabelLookup/when_observing_an_unrelated_event_with_all_literals.cs", GeneratedLiteralSpecification(allFacts, _allLiteralExpected, "observing_an_unrelated_event_with_all_literals", LiteralView.AllLabelSummary));

        // CI-only integration: both orders must retain the nonidentifier from target used for join backfill.
        foreach (var joinedBeforeFrom in new[] { true, false })
        {
            Fact registered = new("ProjectRegistered", First, First, "Local");
            Fact joined = new("ProjectNamed", Second, Name: "Joined");
            Fact[] facts = joinedBeforeFrom ? [joined, registered] : [registered, joined];
            var name = joinedBeforeFrom ? "joined_before_from_with_every_key_collision" : "joined_after_from_with_every_key_collision";
            var expected = Reference(facts, "CompanySummary");
            var expectedSnapshot = JsonSerializer.Serialize(new object?[]
            {
                new { company = Second, joined = "Joined", label = "fixed", score = "12.5" }, null
            });
            Assert.Equal(expectedSnapshot, expected);
            AddGeneratedSpecification($"Projects/Registration/CompanyLookup/when_{name}.cs", GeneratedLiteralSpecification(facts, expected, name, LiteralView.CompanySummary));
        }

        Fact[] removedCompanyFacts =
        [
            new("ProjectRegistered", First, First, "Local"), new("ProjectRemoved", First), new("ProjectNamed", Second, Name: "Joined")
        ];
        var removedCompany = Reference(removedCompanyFacts, "CompanySummary");
        Assert.Equal("[null,null]", removedCompany);
        AddGeneratedSpecification("Projects/Registration/CompanyLookup/when_a_join_does_not_recreate_the_removed_root.cs",
            GeneratedLiteralSpecification(removedCompanyFacts, removedCompany, "a_join_does_not_recreate_the_removed_root", LiteralView.CompanySummary));

        await Run("differential-debug.log", "build", "-c", "Debug", "-warnaserror");
        _testOutput = await Run("differential-tests.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_match_all_reference_snapshots() => _testOutput.ShouldContain("Passed!");
    [Fact] void should_admit_identity_only_all() => when_rejecting_unsupported_scoped_projections.VerifyFromAllAdmission();
    [Fact] void should_materialize_an_unrelated_source_in_the_reference() => _allExpected.ShouldEqual(JsonSerializer.Serialize(new[] { new { projectId = First, lastSeen = First }, new { projectId = Second, lastSeen = Second } }));

    [Fact] void should_apply_the_all_literals_to_an_unrelated_instance_in_the_reference() => _allLiteralExpected.ShouldEqual(JsonSerializer.Serialize(new[]
    {
        new { projectId = First, label = "fixed", title = "named", score = "12.5" },
        new { projectId = Second, label = "fixed", title = "named", score = "12.5" }
    }));

    [Fact] void should_keep_the_literal_after_a_later_local_write() => _laterFromExpected.ShouldContain("\"label\":\"fixed\"");

    string Reference(Fact[] facts, string readModelName = "ProjectSummary")
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
        var referencePlan = SemanticExecutionPlan.Compile(referenceModel);
        Assert.True(referencePlan.Success, string.Join(Environment.NewLine, referencePlan.Issues));
        var compiled = referencePlan.Plan!;
        var constructor = typeof(SemanticExecutionPlan).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var reference = (SemanticExecutionPlan)constructor.Invoke(
        [
            referenceModel,
            compiled.Commands,
            compiled.Events,
            compiled.Projections,
            compiled.ReadModels,
            compiled.Queries,
            compiled.Specifications.SetItem(spec.Id, spec),
            compiled.Constraints,
            compiled.Reactions,
            compiled.Captures,
            UnadmittedArtifacts(compiled)
        ]);
        var result = new SemanticSpecificationRunner().Run(reference, spec.Id);
        var accepted = Assert.IsType<SemanticAccepted>(result.Execution);
        var readModel = feature.Slices.SelectMany(slice => slice.ReadModels).Single(candidate => candidate.Name == readModelName);
        return JsonSerializer.Serialize(new[] { First, Second }.Select(key => Snapshot(
            accepted.World.ReadModels.SingleOrDefault(instance => instance.ReadModel == readModel.Id && instance.Key is SemanticTextValue value && value.Value == key), readModel, _model)));
    }

    // The execution plan keeps what it did not admit internally; a reference plan assembled for a probe carries the same.
    static object? UnadmittedArtifacts(SemanticExecutionPlan plan) =>
        typeof(SemanticExecutionPlan).GetProperty("UnadmittedArtifacts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(plan);

    static object? Snapshot(SemanticReadModelInstance? instance, SemanticReadModel readModel, ExecutableSemanticModel model)
    {
        if (instance is null) return null;
        var values = instance.Values.ToDictionary(value => readModel.Properties.Single(property => property.Id == value.TargetProperty).Name, value => value.Value);
        if (readModel.Name == "AllSummary") return new { projectId = values.TryGetValue("projectId", out var projectId) ? Text(projectId) : null, lastSeen = Text(values["lastSeen"]) };
        if (readModel.Name == "AllLabelSummary")
        {
            return new { projectId = values.TryGetValue("projectId", out var labelProject) ? Text(labelProject) : null, label = Text(values["label"]), title = Text(values["title"]), score = Number(values["score"]) };
        }

        if (readModel.Name == "CompanySummary")
        {
            return new { company = Text(values["company"]), joined = Text(values["joined"]), label = Text(values["label"]), score = Number(values["score"]) };
        }

        var noteType = model.Application.Types.Single(type => type.Name == "ProjectNote");
        var infoType = model.Application.Types.Single(type => type.Name == "ProjectInfo");
        var notes = values["notes"] is SemanticArrayValue array ? array.Values.Select(value =>
        {
            var members = ((SemanticCompositeValue)value).Properties.ToDictionary(member => noteType.Properties.Single(property => property.Id == member.TargetProperty).Name, member => member.Value);
            return new { id = Text(members["noteId"]), name = Text(members["name"]) };
        }).ToArray() : null;
        var info = values["info"] is SemanticCompositeValue composite ? Text(composite.Properties.Single(member => member.TargetProperty == infoType.Properties.Single().Id).Value) : null;
        return new { name = Text(values["name"]), label = Text(values["label"]), visits = Number(values["visits"]), lastSeen = Text(values["lastSeen"]), info, notes };
    }

    static string? Text(SemanticValue value) => value is SemanticTextValue text ? text.Value : null;
    static string? Number(SemanticValue value) => value is SemanticNumberValue number ? number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;

    static string GeneratedSpecification(string name, Fact[] facts, string expected)
    {
        var seeds = string.Join('\n', facts.Select(fact => $"await scenario.Given.ForEventSource(new EventSourceId(\"{fact.Source}\")).Events({fact.CSharpEvent()});"));
        return $$"""
            // Copyright (c) Cratis. All rights reserved.
            // Licensed under the MIT license. See LICENSE file in the project root for full license information.

            #if DEBUG
            using System.Globalization;
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
                        return new
                        {
                            name = instance.Name.Value,
                            label = instance.Label,
                            visits = instance.Visits?.ToString(CultureInfo.InvariantCulture),
                            lastSeen = instance.LastSeen?.Value.ToString(),
                            info = instance.Info?.Name.Value,
                            notes = instance.Notes?.Select(note => new { id = note.NoteId.Value.ToString(), name = note.Name.Value }).ToArray()
                        };
                    });
                    Assert.Equal({{JsonSerializer.Serialize(expected)}}, JsonSerializer.Serialize(actual));
                }
            }
            #endif
            """;
    }

    static string GeneratedAllSpecification(Fact[] facts, string expected)
    {
        var seeds = string.Join('\n', facts.Select(fact => $"await scenario.Given.ForEventSource(new EventSourceId(\"{fact.Source}\")).Events({fact.CSharpEvent()});"));
        return $$"""
            // Copyright (c) Cratis. All rights reserved.
            // Licensed under the MIT license. See LICENSE file in the project root for full license information.

            #if DEBUG
            using System.Text.Json;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Testing.ReadModels;
            using Projects.Common;
            using Projects.Projects.Registration.RegisterProject;
            using Xunit;

            namespace Projects.Projects.Registration.AllLookup;

            public class when_observing_an_unrelated_event
            {
                static readonly string[] _keys = ["{{First}}", "{{Second}}"];

                [Fact]
                public async Task should_match_screenplay()
                {
                    var scenario = new ReadModelScenario<AllSummary>();
            {{seeds}}
                    var actual = _keys.Select(key =>
                    {
                        var instance = scenario.InstanceForEventSourceId(new EventSourceId(key));
                        Assert.NotNull(instance);
                        return new { projectId = instance.ProjectId?.Value.ToString(), lastSeen = instance.LastSeen?.Value.ToString() };
                    });
                    Assert.Equal({{JsonSerializer.Serialize(expected)}}, JsonSerializer.Serialize(actual));
                }
            }
            #endif
            """;
    }

    static string GeneratedLiteralSpecification(Fact[] facts, string expected, string name, LiteralView view)
    {
        var model = view.ToString();
        var slice = model.Replace("Summary", "Lookup", StringComparison.Ordinal);
        var code = new CSharpCodeBuilder()
            .Line("// Copyright (c) Cratis. All rights reserved.")
            .Line("// Licensed under the MIT license. See LICENSE file in the project root for full license information.")
            .Line("#if DEBUG")
            .Line("using System.Globalization;")
            .Line("using System.Text.Json;")
            .Line("using Cratis.Chronicle.Events;")
            .Line("using Cratis.Chronicle.Testing.ReadModels;")
            .Line("using Projects.Common;")
            .Line("using Projects.Projects.Registration.RegisterProject;")
            .Line("using Xunit;")
            .Line($"namespace Projects.Projects.Registration.{slice};")
            .OpenBlock($"public class when_{name}")
            .Line($"static readonly string[] _keys = [\"{First}\", \"{Second}\"];")
            .Attribute("Fact")
            .OpenBlock("public async Task should_match_screenplay()")
            .Line($"var scenario = new ReadModelScenario<{model}>();");
        foreach (var fact in facts)
        {
            code.Line($"await scenario.Given.ForEventSource(new EventSourceId(\"{fact.Source}\")).Events({fact.CSharpEvent()});");
        }

        code.OpenBlock("var actual = _keys.Select(key =>")
            .Line("var instance = scenario.InstanceForEventSourceId(new EventSourceId(key));")
            .Line(view == LiteralView.AllLabelSummary ? "Assert.NotNull(instance);" : "if (instance is null) return null;")
            .Line(view == LiteralView.AllLabelSummary
                ? "return new { projectId = instance.ProjectId.Value.ToString(), label = instance.Label, title = instance.Title.Value, score = instance.Score?.ToString(CultureInfo.InvariantCulture) };"
                : "return new { company = instance.Company.Value.ToString(), joined = instance.Joined?.Value, label = instance.Label, score = instance.Score?.ToString(CultureInfo.InvariantCulture) };")
            .EndBlock().Line(");")
            .Line($"Assert.Equal({JsonSerializer.Serialize(expected)}, JsonSerializer.Serialize(actual));")
            .EndBlock().EndBlock().Line("#endif");
        return code.ToString();
    }

    enum LiteralView
    {
        AllLabelSummary,
        CompanySummary
    }

    sealed record Fact(string Event, string Source, string? Id = null, string? Name = null, string? NoteId = null)
    {
        public string ValueFor(string property) => property switch
        {
            "projectId" => Id!,
            "noteId" => NoteId!,
            "name" => Name!,
            _ => throw new UnknownProjectionProperty(property)
        };

        public string CSharpEvent() => Event switch
        {
            "ProjectRegistered" or "ProjectRenamed" => $"new {Event}(new ProjectId(Guid.Parse(\"{Id}\")), new ProjectName(\"{Name}\"))",
            "ProjectNoted" => $"new ProjectNoted(new ProjectId(Guid.Parse(\"{NoteId}\")), new ProjectId(Guid.Parse(\"{Id}\")), new ProjectName(\"{Name}\"))",
            "ProjectNoteRemoved" => $"new ProjectNoteRemoved(new ProjectId(Guid.Parse(\"{NoteId}\")), new ProjectId(Guid.Parse(\"{Id}\")))",
            "ProjectNoteRemovedViaJoin" => $"new ProjectNoteRemovedViaJoin(new ProjectId(Guid.Parse(\"{NoteId}\")))",
            "ProjectNamed" => $"new ProjectNamed(new ProjectName(\"{Name}\"))",
            "ProjectRemoved" => "new ProjectRemoved()",
            _ => throw new UnknownProjectionEvent(Event)
        };
    }

    sealed class UnknownProjectionProperty(string name) : Exception($"Unknown projection property '{name}'.");
    sealed class UnknownProjectionEvent(string name) : Exception($"Unknown projection event '{name}'.");
}
#endif
