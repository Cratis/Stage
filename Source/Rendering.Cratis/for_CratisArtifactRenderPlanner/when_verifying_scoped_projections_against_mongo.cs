// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Runs a generated application's projection probes against a real Chronicle kernel and MongoDB when explicitly configured.
/// The probe definitions add the two blocked projection blocks to the admitted scoped fixture without relaxing admission.
/// </summary>
public class when_verifying_scoped_projections_against_mongo : a_generated_application
{
    const string Probe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Chronicle.Testing.ReadModels;
        using Cratis.Chronicle.Registrations;
        using Projects.Common;
        using Projects.Projects.Registration.RegisterProject;
        using Projects.Projects.Registration.ProjectLookup;
        using Xunit;

        namespace Projects.MongoProbe;

        [ReadModel]
        public record NestedProbe([Key] ProjectId ProjectId, ProjectName Name, ProjectInfo? Info);

        public class NestedProbeProjection : IProjectionFor<NestedProbe>
        {
            public void Define(IProjectionBuilderFor<NestedProbe> builder)
            {
                builder.NoAutoMap();
                builder.From<ProjectRegistered>(from =>
                {
                    from.UsingKey(evt => evt.ProjectId);
                    from.Set(model => model.Name).To(evt => evt.Name);
                });
                builder.From<ProjectRenamed>(from =>
                {
                    from.UsingKey(evt => evt.ProjectId);
                    from.Set(model => model.Name).To(evt => evt.Name);
                });
                builder.Nested<ProjectInfo>(model => model.Info, nested =>
                {
                    nested.NoAutoMap();
                    nested.From<ProjectRegistered>(from =>
                    {
                        from.UsingKey(evt => evt.ProjectId);
                        from.Set(model => model.Name).To(evt => evt.Name);
                    });
                    nested.RemovedWith<ProjectRenamed>(removed => removed.UsingKey(evt => evt.ProjectId));
                });
            }
        }

        [ReadModel]
        public record ChildrenProbe([Key] ProjectId ProjectId, ProjectName Name, ProjectNote[] Notes);

        public class ChildrenProbeProjection : IProjectionFor<ChildrenProbe>
        {
            public void Define(IProjectionBuilderFor<ChildrenProbe> builder)
            {
                builder.NoAutoMap();
                builder.From<ProjectRegistered>(from =>
                {
                    from.UsingKey(evt => evt.ProjectId);
                    from.Set(model => model.Name).To(evt => evt.Name);
                });
                builder.Children<ProjectNote>(model => model.Notes, children =>
                {
                    children.NoAutoMap();
                    children.IdentifiedBy(note => note.NoteId);
                    children.From<ProjectNoted>(from =>
                    {
                        from.UsingKey(evt => evt.NoteId);
                        from.UsingParentKey(evt => evt.ProjectId);
                        from.Set(note => note.Name).To(evt => evt.Name);
                    });
                    children.RemovedWithJoin<ProjectNoteRemovedViaJoin>(removed => removed.UsingKey(evt => evt.NoteId));
                });
            }
        }

        public class when_probing_mongo
        {
            const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
            const string Second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
            const string Note = "5fa85f64-5717-4562-b3fc-2c963f66afa8";

            [Fact]
            public async Task should_report_persistent_snapshots()
            {
                var address = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                Assert.False(string.IsNullOrWhiteSpace(address));
                using var client = new ChronicleClient(address!);
                var store = await client.GetEventStore("Stage117", Guid.NewGuid().ToString("N"));
                var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(90));
                Assert.True(registration.IsSuccess, $"Registration failed: {registration.Failure}; {string.Join("; ", registration.Failures)}");
                var first = new ProjectId(Guid.Parse(First));
                var second = new ProjectId(Guid.Parse(Second));
                var note = new ProjectId(Guid.Parse(Note));
                var memoryNested = new ReadModelScenario<NestedProbe>();
                await memoryNested.Given.ForEventSource((EventSourceId)first).Events(new ProjectRegistered(first, new ProjectName("First")));
                await memoryNested.Given.ForEventSource((EventSourceId)first).Events(new ProjectRenamed(first, new ProjectName("Updated")));
                await memoryNested.Given.ForEventSource((EventSourceId)first).Events(new ProjectRegistered(first, new ProjectName("Again")));
                var inMemoryFailure = Record.Exception(() => memoryNested.InstanceForEventSourceId((EventSourceId)first));
                Console.WriteLine($"NESTED_IN_MEMORY={inMemoryFailure?.GetType().Name ?? "NoException"}");
                var memoryChildren = new ReadModelScenario<ChildrenProbe>();
                await memoryChildren.Given.ForEventSource((EventSourceId)first).Events(new ProjectRegistered(first, new ProjectName("First")));
                await memoryChildren.Given.ForEventSource((EventSourceId)second).Events(new ProjectRegistered(second, new ProjectName("Second")));
                await memoryChildren.Given.ForEventSource((EventSourceId)note).Events(new ProjectNoted(note, first, new ProjectName("A")));
                await memoryChildren.Given.ForEventSource((EventSourceId)note).Events(new ProjectNoted(note, second, new ProjectName("B")));
                Console.WriteLine($"JOIN_IN_MEMORY_BEFORE_FIRST={string.Join(",", memoryChildren.InstanceForEventSourceId((EventSourceId)first)?.Notes?.Select(item => item.Name.Value) ?? [])}");
                Console.WriteLine($"JOIN_IN_MEMORY_BEFORE_SECOND={string.Join(",", memoryChildren.InstanceForEventSourceId((EventSourceId)second)?.Notes?.Select(item => item.Name.Value) ?? [])}");
                await memoryChildren.Given.ForEventSource((EventSourceId)note).Events(new ProjectNoteRemovedViaJoin(note));
                Console.WriteLine($"JOIN_IN_MEMORY_PARENT_FIRST={string.Join(",", memoryChildren.InstanceForEventSourceId((EventSourceId)first)?.Notes?.Select(item => item.Name.Value) ?? [])}");
                Console.WriteLine($"JOIN_IN_MEMORY_PARENT_SECOND={string.Join(",", memoryChildren.InstanceForEventSourceId((EventSourceId)second)?.Notes?.Select(item => item.Name.Value) ?? [])}");
                await Append((EventSourceId)first, new ProjectRegistered(first, new ProjectName("First")));
                await Append((EventSourceId)second, new ProjectRegistered(second, new ProjectName("Second")));
                await Append((EventSourceId)note, new ProjectNoted(note, first, new ProjectName("A")));
                await Append((EventSourceId)note, new ProjectNoted(note, second, new ProjectName("B")));
                Console.WriteLine($"JOIN_BEFORE_PARENT_FIRST={string.Join(",", (await store.ReadModels.GetInstanceById<ChildrenProbe>((EventSourceId)first))?.Notes?.Select(item => item.Name.Value) ?? [])}");
                Console.WriteLine($"JOIN_BEFORE_PARENT_SECOND={string.Join(",", (await store.ReadModels.GetInstanceById<ChildrenProbe>((EventSourceId)second))?.Notes?.Select(item => item.Name.Value) ?? [])}");
                await Append((EventSourceId)note, new ProjectNoteRemovedViaJoin(note));
                var firstResult = await store.ReadModels.GetInstanceById<ChildrenProbe>((EventSourceId)first);
                var secondResult = await store.ReadModels.GetInstanceById<ChildrenProbe>((EventSourceId)second);
                Console.WriteLine($"JOIN_REMOVAL_PARENT_FIRST={string.Join(",", firstResult?.Notes?.Select(item => item.Name.Value) ?? [])}");
                Console.WriteLine($"JOIN_REMOVAL_PARENT_SECOND={string.Join(",", secondResult?.Notes?.Select(item => item.Name.Value) ?? [])}");
                await Append((EventSourceId)first, new ProjectRenamed(first, new ProjectName("Updated")));
                Console.WriteLine($"NESTED_AFTER_CLEAR={ (await store.ReadModels.GetInstanceById<NestedProbe>((EventSourceId)first))?.Info?.Name.Value ?? "null" }");
                var recreation = await store.EventLog.Append((EventSourceId)first, new ProjectRegistered(first, new ProjectName("Again")));
                Assert.True(recreation.IsSuccess);
                var failures = await store.Projections.WaitForThereToBeFailedPartitions<NestedProbeProjection>(TimeSpan.FromSeconds(30));
                Console.WriteLine($"NESTED_MONGO_FAILURE={string.Join("; ", failures.SelectMany(failure => failure.Attempts).SelectMany(attempt => attempt.Messages))}");
                Console.WriteLine($"NESTED_AFTER_RECREATION={ (await store.ReadModels.GetInstanceById<NestedProbe>((EventSourceId)first))?.Info?.Name.Value ?? "null" }");
                async Task Append(EventSourceId id, object fact)
                {
                    var append = await store.EventLog.Append(id, fact);
                    Assert.True(append.IsSuccess, $"Append failed: {append}");
                    if (fact is ProjectRegistered or ProjectRenamed)
                    {
                        await store.Projections.WaitTillReachesEventSequenceNumber<NestedProbeProjection>(append.SequenceNumber, TimeSpan.FromSeconds(30));
                    }

                    if (fact is ProjectRegistered or ProjectNoted or ProjectNoteRemovedViaJoin)
                    {
                        await store.Projections.WaitTillReachesEventSequenceNumber<ChildrenProbeProjection>(append.SequenceNumber, TimeSpan.FromSeconds(30));
                    }
                    Assert.Empty(await store.Projections.GetFailedPartitionsFor<NestedProbeProjection>());
                    Assert.Empty(await store.Projections.GetFailedPartitionsFor<ChildrenProbeProjection>());
                }
            }
        }
        """;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("mongo"), "mongo", "Scopes.play", when_rendering_scoped_projections.ScopedSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        return CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
    }

    string _output = string.Empty;

    async Task Because()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION")))
        {
            return;
        }

        AddGeneratedSpecification("MongoProbe.cs", Probe);
        await Run("mongo-debug.log", "build", "-c", "Debug", "-warnaserror");
        _output = await Run("mongo-probe.log", "test", "-c", "Debug", "--no-build", "--filter", "FullyQualifiedName~when_probing_mongo", "--logger", "console;verbosity=normal");
    }

    [Fact] void should_report_both_mongo_shapes_when_configured()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION")))
        {
            return;
        }

        Assert.Contains("JOIN_BEFORE_PARENT_FIRST=A", _output, StringComparison.Ordinal);
        Assert.Contains("JOIN_BEFORE_PARENT_SECOND=B", _output, StringComparison.Ordinal);
        Assert.Contains("JOIN_REMOVAL_PARENT_FIRST=\n", _output, StringComparison.Ordinal);
        Assert.Contains("JOIN_REMOVAL_PARENT_SECOND=\n", _output, StringComparison.Ordinal);
        Assert.Contains("NESTED_IN_MEMORY=NullReferenceException", _output, StringComparison.Ordinal);
        Assert.Contains("JOIN_IN_MEMORY_BEFORE_FIRST=A", _output, StringComparison.Ordinal);
        Assert.Contains("JOIN_IN_MEMORY_BEFORE_SECOND=B", _output, StringComparison.Ordinal);
        Assert.Contains("JOIN_IN_MEMORY_PARENT_FIRST=A", _output, StringComparison.Ordinal);
        Assert.Contains("JOIN_IN_MEMORY_PARENT_SECOND=\n", _output, StringComparison.Ordinal);
        Assert.Contains("NESTED_AFTER_CLEAR=null", _output, StringComparison.Ordinal);
        Assert.Contains("NESTED_MONGO_FAILURE=", _output, StringComparison.Ordinal);
        Assert.Contains("Cannot create field 'Name' in element {Info: null}", _output, StringComparison.Ordinal);
        Assert.Contains("NESTED_AFTER_RECREATION=null", _output, StringComparison.Ordinal);
    }

    [Fact] void should_confirm_screenplay_reference_for_both_sequences()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("reference"), "reference", "Scopes.play", when_rendering_scoped_projections.ScopedSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        var model = compilation.Value!.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var view = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateView);
        var projection = view.Projections.Single(candidate => candidate.Name == "ProjectSummaryProjection");
        var scope = projection.Scope!;
        var events = feature.Slices.SelectMany(slice => slice.Events).ToDictionary(@event => @event.Name);
        scope = scope with
        {
            Nested = [.. scope.Nested.Select(nested => nested with { Scope = nested.Scope with
            {
                Removals = [new SemanticProjectionRemoval(events["ProjectRenamed"].Id, scope.From[1].Key, null)]
            } })],
            Children = [.. scope.Children.Select(child => child with { Scope = child.Scope with
            {
                JoinRemovals = [new SemanticProjectionJoinRemoval(events["ProjectNoteRemovedViaJoin"].Id, SemanticProjectionKey.EventSourceIdentity)]
            } })]
        };
        var modifiedView = view with { Projections = [.. view.Projections.Select(candidate => candidate.Id == projection.Id ? candidate with { Scope = scope } : candidate)] };
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == view.Id ? modifiedView : slice)] }] }]
        });
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        var source = feature.Slices.Single(slice => slice.Commands.Any());
        var original = source.Specifications.Single(spec => spec.Name == "RegisteringAProject");
        var identity = source.Commands.Single().Properties.Single(property => property.IsIdentifier).Type;
        const string first = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
        const string second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
        const string note = "5fa85f64-5717-4562-b3fc-2c963f66afa8";
        var facts = new (string Name, string Stream, string? Project, string? Label, string? Note)[]
        {
            ("ProjectRegistered", first, first, "First", null),
            ("ProjectRegistered", second, second, "Second", null),
            ("ProjectNoted", note, first, "A", note),
            ("ProjectNoted", note, second, "B", note),
            ("ProjectNoteRemovedViaJoin", note, null, null, note),
            ("ProjectRenamed", first, first, "Updated", null),
            ("ProjectRegistered", first, first, "Again", null)
        };
        var givens = facts.Select(fact => new SemanticSpecificationEvent(events[fact.Name].Id, [.. events[fact.Name].Properties.Select(property => new SemanticPropertyValue(property.Id, new SemanticTextValue(property.Name switch
        {
            "projectId" => fact.Project!, "noteId" => fact.Note!, "name" => fact.Label!,
            _ => throw new UnknownReferenceProperty(property.Name)
        })))]) { EventSource = new(identity, new SemanticTextValue(fact.Stream)) }).ToArray();
        var marker = events["ProjectRegistered"];
        const string third = "7fa85f64-5717-4562-b3fc-2c963f66afaa";
        var specification = original with
        {
            GivenEvents = [.. givens], When = null,
            WhenAppended = new SemanticSpecificationAppend(marker.Id,
                [.. marker.Properties.Select(property => new SemanticPropertyValue(property.Id,
                    new SemanticTextValue(property.Name == "projectId" ? third : "Unused")))])
            { EventSource = new(identity, new SemanticTextValue(third)) },
            ThenEvents = [], ThenReadModels = [], ThenQueries = [], ThenErrors = []
        };
        var constructor = typeof(SemanticExecutionPlan).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var plan = execution.Plan!;
        var reference = (SemanticExecutionPlan)constructor.Invoke([
            model, plan.Commands, plan.Events, plan.Projections, plan.ReadModels, plan.Queries,
            plan.Specifications.SetItem(specification.Id, specification), plan.Constraints
        ]);
        var accepted = Assert.IsType<SemanticAccepted>(new SemanticSpecificationRunner().Run(reference, specification.Id).Execution);
        var readModel = modifiedView.ReadModels.Single(candidate => candidate.Name == "ProjectSummary");
        var info = model.Application.Types.Single(type => type.Name == "ProjectInfo");
        var firstInstance = accepted.World.ReadModels.Single(instance => instance.ReadModel == readModel.Id && instance.Key is SemanticTextValue key && key.Value == first);
        var secondInstance = accepted.World.ReadModels.Single(instance => instance.ReadModel == readModel.Id && instance.Key is SemanticTextValue key && key.Value == second);
        var firstValues = firstInstance.Values.ToDictionary(value => readModel.Properties.Single(property => property.Id == value.TargetProperty).Name, value => value.Value);
        var secondValues = secondInstance.Values.ToDictionary(value => readModel.Properties.Single(property => property.Id == value.TargetProperty).Name, value => value.Value);
        var nested = Assert.IsType<SemanticCompositeValue>(firstValues["info"]);
        Assert.Equal("Again", Assert.IsType<SemanticTextValue>(nested.Properties.Single(value => value.TargetProperty == info.Properties.Single().Id).Value).Value);
        Assert.Empty(Assert.IsType<SemanticArrayValue>(firstValues["notes"]).Values);
        Assert.Empty(Assert.IsType<SemanticArrayValue>(secondValues["notes"]).Values);
    }

    sealed class UnknownReferenceProperty(string name) : Exception($"Unknown reference property '{name}'.");
}
#endif
