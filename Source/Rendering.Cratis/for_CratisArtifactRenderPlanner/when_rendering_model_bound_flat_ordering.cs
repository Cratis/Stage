// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_model_bound_flat_ordering : Specification
{
    const string Header = """
        type Header
          sourceKey String
          parent String
          label String
        """;

    const string DottedProjection = """
        from Recorded key header.sourceKey
          label = header.label
        from Changed key header.sourceKey
          label = header.label
        remove with Removed key header.sourceKey
        """;

    const string ChildrenProjection = """
        from Recorded key sourceKey
          label = value
        children entries identified by code
          from Changed key header.sourceKey
            parent header.parent
            label = header.label
          remove via join on Removed key header.sourceKey
        """;

    [Fact]
    public async Task should_execute_dotted_sources_and_keys_without_cross_instance_updates() =>
        await Compare(new(DottedSource(), """
            await Append(new Recorded(new Header("target", "ignored", "one"), "ignored", 1), "transport");
            await Append(new Recorded(new Header("other", "ignored", "other"), "ignored", 1), "transport");
            await Append(new Changed(new Header("target", "ignored", "two"), "ignored", 1), "different");
            Assert.Equal("two", Read("target")!.Label);
            Assert.Equal("other", Read("other")!.Label);
            Assert.Null(Read("transport"));
            await Append(new Removed(new Header("target", "ignored", "ignored")), "different");
            Assert.Null(Read("target"));
            Assert.Equal("other", Read("other")!.Label);
            """));

    [Fact]
    public async Task should_keep_dotted_parent_paths_and_join_removals_fluent_and_isolated() =>
        await Compare(new(
            ChildrenSource(),
            """
            await Append(new Recorded("parent-a", "a", 1), "parent-a");
            await Append(new Recorded("parent-b", "b", 1), "parent-b");
            await Append(new Changed(new Header("child-a", "parent-a", "one"), "ignored", 1), "child-a");
            await Append(new Changed(new Header("child-b", "parent-b", "two"), "ignored", 1), "child-b");
            Assert.Equal("one", Assert.Single(Read("parent-a")!.Entries).Label);
            Assert.Equal("two", Assert.Single(Read("parent-b")!.Entries).Label);
            await Append(new Removed(new Header("child-a", "ignored", "ignored")), "child-a");
            Assert.Empty(Read("parent-a")!.Entries);
            Assert.Equal("two", Assert.Single(Read("parent-b")!.Entries).Label);
            """,
            ModelBound: false));

    [Fact]
    public void should_refuse_dotted_parent_paths_at_the_root()
    {
        var source = DottedSource(DottedProjection.Replace("  label = header.label", "  parent header.parent\n  label = header.label", StringComparison.Ordinal));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("parent"), "parent", "Parent.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.False(compilation.Success);
        Assert.Contains(compilation.Diagnostics, diagnostic => diagnostic.Code == "PLAY0268");
    }

    [Fact]
    public void should_refuse_unverified_root_join_removal()
    {
        var source = DottedSource(DottedProjection.Replace("remove with Removed", "remove via join on Removed", StringComparison.Ordinal));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("removal"), "removal", "Removal.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var execution = SemanticExecutionPlan.Compile(compilation.Value.Model);
        Assert.False(execution.Success);
        Assert.Contains(execution.Issues, issue => issue.Kind == SemanticPlanIssueKind.UnsupportedProjectionBlock && issue.Details.Contains("has no verified meaning", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_preserve_join_backfill_refresh_and_both_arrival_orders(bool joinFirst)
    {
        var beforeRoot = joinFirst ? "await Append(new Changed(\"decoy\", \"early\", 1), \"company\");" : string.Empty;
        var expected = joinFirst ? "\"early\"" : "null";
        var source = PlainSource(
            """
            from Recorded key sourceKey
              label = value
            join details on label
              with Changed
                joined = value
            """,
            "label String\njoined String optional");
        await Compare(new(source, $$"""
            {{beforeRoot}}
            await Append(new Recorded("target", "company", 1), "transport");
            Assert.Equal({{expected}}, Read("target")!.Joined);
            await Append(new Recorded("other", "elsewhere", 1), "transport");
            await Append(new Changed("decoy", "unrelated", 1), "unmatched");
            Assert.Equal({{expected}}, Read("target")!.Joined);
            Assert.Null(Read("other")!.Joined);
            await Append(new Changed("decoy", "fresh", 1), "company");
            Assert.Equal("fresh", Read("target")!.Joined);
            Assert.Null(Read("other")!.Joined);
            await Append(new Changed("decoy", "refreshed", 1), "company");
            Assert.Equal("refreshed", Read("target")!.Joined);
            Assert.Null(Read("other")!.Joined);
            """));
    }

    [Theory]
    [InlineData("every", true)]
    [InlineData("every", false)]
    [InlineData("all", true)]
    [InlineData("all", false)]
    public async Task should_preserve_every_all_ordering_and_unrelated_event_isolation(string subscription, bool changedFirst)
    {
        var source = PlainSource(
            """
            from Recorded
              id = $eventSourceId
              label = value
            from Changed
              id = $eventSourceId
              label = value
            SUBSCRIPTION
              seen = $eventContext.occurred
            """.Replace("SUBSCRIPTION", subscription, StringComparison.Ordinal),
            "label String optional\nseen DateTime optional");
        var first = changedFirst ? "new Changed(\"ignored\", \"first\", 1)" : "new Recorded(\"ignored\", \"first\", 1)";
        var last = changedFirst ? "new Recorded(\"ignored\", \"last\", 1)" : "new Changed(\"ignored\", \"last\", 1)";
        var unrelated = subscription == "all"
            ? "Assert.Equal(start.AddMinutes(4), Read(\"unrelated\")!.Seen);"
            : "Assert.Null(Read(\"unrelated\"));";
        await Compare(new(
            source,
            $$"""
            await Append({{first}}, "target");
            Assert.Equal("first", Read("target")!.Label);
            Assert.Equal(start.AddMinutes(1), Read("target")!.Seen);
            await Append(new Recorded("ignored", "other", 1), "other");
            await Append({{last}}, "target");
            Assert.Equal("last", Read("target")!.Label);
            Assert.Equal(start.AddMinutes(3), Read("target")!.Seen);
            Assert.Equal("other", Read("other")!.Label);
            Assert.Equal(start.AddMinutes(2), Read("other")!.Seen);
            await Append(new Removed("ignored"), "unrelated");
            {{unrelated}}
            Assert.Equal(start.AddMinutes(3), Read("target")!.Seen);
            Assert.Equal(start.AddMinutes(2), Read("other")!.Seen);
            """,
            Contextual: true));
    }

    static string PlainSource(string projection, string fields = "label String optional") =>
        when_rendering_model_bound_flat_scopes.Source(new(fields, projection, string.Empty, string.Empty, false))
            .Split("      specification SharedSnapshot", StringSplitOptions.None)[0];

    static string DottedSource(string projection = DottedProjection) => Header + "\n" + PlainSource(projection)
        .Replace("        sourceKey String", "        header Header", StringComparison.Ordinal);

    static string ChildrenSource() => Header + "\n" + """
        type Entry
          code String
          label String
        """ + "\n" + PlainSource(ChildrenProjection, "label String optional\nentries Entry[]")
        .Replace("      event Changed\n        sourceKey String", "      event Changed\n        header Header", StringComparison.Ordinal)
        .Replace("      event Removed\n        sourceKey String", "      event Removed\n        header Header", StringComparison.Ordinal);

    static async Task Compare(Vector vector)
    {
        foreach (var modern in new[] { false, true })
        {
            var plan = when_rendering_model_bound_projections.Plan(vector.Source, modern);
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            var code = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == "Checks/Facts/Lookup/Lookup.cs").Bytes.AsSpan());
            Assert.Equal(modern && vector.ModelBound, !code.Contains("IProjectionFor<Snapshot>", StringComparison.Ordinal));
            await Execute(plan, vector, modern && vector.ModelBound);
        }
    }

    static async Task Execute(ArtifactRenderPlan plan, Vector vector, bool modelBound)
    {
        var artifacts = modelBound ? "ModelBoundProjections = [typeof(Snapshot)]" : "Projections = [typeof(Snapshots)]";
        var append = vector.Contextual ? """
            var start = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
            ulong sequence = 0;
            async Task Append(object value, string source)
            {
                var number = ++sequence;
                var context = EventContext.From(eventStore: "Projects", @namespace: "default", eventSourceType: EventSourceType.Default,
                    eventStreamType: EventStreamType.All, eventStreamId: EventStreamId.Default, eventSourceId: source,
                    eventType: value.GetType().GetEventType(), sequenceNumber: number, correlationId: Guid.Empty, occurred: start.AddMinutes((int)number));
                await scenario.ProcessAppendedEvents([new AppendedEvent(context, value)]);
            }
            """ : "Task Append(object value, string source) => scenario.Given.ForEventSource(new EventSourceId(source)).Events(value);";
        var script = $$"""
            using System;
            using System.Threading.Tasks;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Testing;
            using Cratis.Chronicle.Testing.ReadModels;
            using Projects.Common;
            using Xunit;
            namespace Projects.Checks.Facts.Lookup;
            public static class FlatOrderingVector
            {
                public static async Task Run()
                {
                    var defaults = new Defaults(new InMemoryClientArtifactsProvider
                    {
                        EventTypes = [typeof(Recorded), typeof(Changed), typeof(Removed)],
                        {{artifacts}}
                    });
                    using var scenario = new ReadModelScenario<Snapshot>(default, defaults);
                    {{append}}
                    Snapshot? Read(string source) => scenario.InstanceForEventSourceId(new EventSourceId(source));
                    {{vector.Body}}
                }
            }
            """;
        var files = when_rendering_read_model_absence.Files(plan).Append(new RenderedFile("FlatOrderingVector.cs", script)).ToArray();
        Assert.Empty(RenderedOutput.Warnings(files));
        var assembly = RenderedOutput.Load(files);
        var method = assembly.GetType("Projects.Checks.Facts.Lookup.FlatOrderingVector")!.GetMethod("Run")!;
        await (Task)method.Invoke(null, null)!;
    }

    record Vector(string Source, string Body, bool ModelBound = true, bool Contextual = false);
}
#endif
