// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_model_bound_flat_scopes : Specification
{
    const string Prefix = "global::Projects.Checks.Facts.Lookup.";
    const string Base = """
        concept Label : String
        module Checks
          feature Facts
            slice StateView Lookup
              event Recorded
                sourceKey String
                value String
                amount Int
              event Changed
                sourceKey String
                value String
                amount Int
              event Removed
                sourceKey String
              readmodel Snapshot
                id String
        """;

    internal static readonly IReadOnlyDictionary<string, Example> Examples = new Dictionary<string, Example>(StringComparer.Ordinal)
    {
        ["literal"] = new("label Label optional\nnumber Int\nactive Bool", "from Recorded key id\n  label = \"fixed\"\n  number = 7\n  active = true", "label = \"fixed\"\nnumber = 7\nactive = true", "SetValue<Recorded>", false),
        ["constant"] = new("label String optional", "from Recorded key \"aggregate\"\n  label = value", "label = \"one\"", "ConstantKey = \"aggregate\"", false, "aggregate"),
        ["context"] = new("seen DateTime optional", "from Recorded key id\n  seen = $eventContext.occurred", string.Empty, "SetFromContext<Recorded>", false),
        ["once"] = new("label String optional", "from Recorded key id\n  set once label = value", "label = \"one\"", "SetOnce = true", true),
        ["once-context"] = new("seen DateTime optional", "from Recorded key id\n  set once seen = $eventContext.occurred", string.Empty, "SetOnce = true", true),
        ["product"] = new("total Int", "from Recorded key id\n  add total by amount * amount", "total = 8", "AddFrom<Recorded>", true),
        ["product-subtract"] = new("total Int", "from Recorded key id\n  add total by amount * amount\nfrom Changed key id\n  subtract total by amount * amount", "total = -5", "SubtractFrom<Changed>", false, "target", "Changed"),
        ["constant-decrement"] = new("total Int", "from Recorded key \"aggregate\"\n  increment total\nfrom Changed key \"aggregate\"\n  decrement total", "total = 0", "Decrement<Changed>(ConstantKey = \"aggregate\")", false, "aggregate", "Changed", FinalSource: "different-transport"),
        ["join-isolation"] = new("label String optional\njoined String optional", "from Recorded key id\n  label = value\njoin details on id\n  with Changed\n    joined = value", "label = \"one\"\njoined = null", "Join<Changed>", false, "target", "Changed", FinalSource: "unrelated"),
        ["clear"] = new("label String optional", "from Recorded key id\n  label = value\nfrom Changed key id\n  clear label", string.Empty, "ClearWith<Changed>", false, "target", "Changed"),
        ["counter"] = new("total Int", "from Recorded key \"aggregate\"\n  increment total", "total = 2", "Increment<Recorded>(ConstantKey = \"aggregate\")", true, "aggregate"),
        ["null"] = new("label String optional", "from Recorded key id\n  label = value\nfrom Changed key id\n  label = null", string.Empty, "ClearWith<Changed>", false, "target", "Changed"),
        ["zero"] = new("total Int", "from Recorded key id\n  add total by 0", "total = 0", "NotProjected", false),
        ["every"] = new("label String optional", "from Recorded key id\nevery\n  label = $eventContext.eventSourceId", "label = \"transport\"", "FromEvery(", false),
        ["all"] = new("label String optional", "from Recorded\n  id = $eventSourceId\nall\n  label = $eventContext.eventSourceId", "label = \"transport\"", "FromAll(", false, "transport"),
        ["join"] = new("label String optional\njoined String optional", "from Recorded key id\n  label = value\njoin details on id\n  with Changed\n    joined = value", "label = \"one\"\njoined = \"two\"", "Join<Changed>", false, "target", "Changed", FinalSource: "target"),
        ["remove"] = new("label String optional", "from Recorded key id\n  label = value\nremove with Removed key id", string.Empty, "RemovedWith<Removed>", false, "target", "Removed"),
        ["unmapped"] = new("label String optional", "from Recorded key id", string.Empty, string.Empty, false),
        ["clear-only"] = new("label String optional", "from Recorded key id\n  clear label", string.Empty, string.Empty, false),
        ["literal-once"] = new("label String optional", "from Recorded key id\n  set once label = \"fixed\"", "label = \"fixed\"", string.Empty, false),
        ["literal-every"] = new("label String optional", "from Recorded key id\nevery\n  label = \"fixed\"", "label = \"fixed\"", string.Empty, false),
        ["custom-join"] = new("label String optional\njoined String optional", "from Recorded key id\n  label = value\njoin details on id\n  from Changed key id\n    joined = value", "label = \"one\"\njoined = \"two\"", string.Empty, false, "target", "Changed"),
        ["overlap-join"] = new("label String optional", "from Recorded key id\n  label = value\njoin details on id\n  with Changed\n    label = value", "label = \"two\"", string.Empty, false, "target", "Changed", FinalSource: "target"),
        ["remove-only"] = new(string.Empty, "remove with Removed key id", string.Empty, "RemovedWith<Removed>", false, "target", "Removed", true)
    };

    static readonly HashSet<string> _manualVectors = ["join-isolation", "context", "once-context"];

    [Theory]
    [InlineData("literal")]
    [InlineData("constant")]
    [InlineData("context")]
    [InlineData("counter")]
    [InlineData("null")]
    [InlineData("once")]
    [InlineData("product")]
    [InlineData("product-subtract")]
    [InlineData("constant-decrement")]
    [InlineData("join-isolation")]
    [InlineData("clear")]
    [InlineData("every")]
    [InlineData("all")]
    [InlineData("join")]
    [InlineData("remove")]
    async Task should_compile_and_pass_identical_projected_specifications(string name)
    {
        if (_manualVectors.Contains(name))
        {
            await RunManualVector(name);
            return;
        }

        var example = Examples[name];
        var source = Source(example);
        foreach (var modern in new[] { false, true })
        {
            var plan = when_rendering_model_bound_projections.Plan(source, modern);
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            var files = when_rendering_read_model_absence.Files(plan);
            var code = files.Single(file => file.RelativePath.EndsWith("/Lookup/Lookup.cs", StringComparison.Ordinal)).Content;
            if (modern)
            {
                Assert.Contains(example.Attribute, code, StringComparison.Ordinal);
                Assert.DoesNotContain("IProjectionFor", code, StringComparison.Ordinal);
                Assert.DoesNotContain("#pragma", code, StringComparison.Ordinal);
                Assert.DoesNotContain("NoAutoMap()", code, StringComparison.Ordinal);
            }
            Assert.Empty(RenderedOutput.Warnings(files));
            await when_rendering_read_model_absence.RunProjected(when_rendering_read_model_absence.LoadWithArtifacts(files, Artifacts(modern)));
        }
    }

    [Fact]
    async Task should_keep_context_set_once_fluent_when_baseline_materialization_is_unproven() =>
        await RunManualVector("once-context");

    [Fact]
    void should_refuse_custom_join_key_syntax_without_rendering_a_different_join()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("custom-join"), "custom-join", "CustomJoin.play", Source(Examples["custom-join"]));
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.False(compilation.Success);
        Assert.Contains(compilation.Diagnostics, diagnostic => diagnostic.Code == "PLAY0064");
    }

    [Theory]
    [InlineData("zero")]
    [InlineData("remove-only")]
    [InlineData("unmapped")]
    [InlineData("clear-only")]
    [InlineData("literal-once")]
    [InlineData("literal-every")]
    [InlineData("overlap-join")]
    void should_keep_an_unrepresentable_whole_scope_fluent(string name)
    {
        var plan = when_rendering_model_bound_projections.Plan(Source(Examples[name]), true);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var code = when_rendering_read_model_absence.Files(plan).Single(file => file.RelativePath.EndsWith("/Lookup/Lookup.cs", StringComparison.Ordinal)).Content;
        Assert.Contains("IProjectionFor<Snapshot>", code, StringComparison.Ordinal);
        Assert.Contains("builder.NoAutoMap()", code, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("literal", "label = \"fixed\"", "label = \"wrong\"")]
    [InlineData("once", "label = \"one\"", "label = \"two\"")]
    async Task should_fail_a_wrong_shared_snapshot(string name, string before, string after)
    {
        var source = Source(Examples[name]);
        var index = source.LastIndexOf(before, StringComparison.Ordinal);
        Assert.True(index >= 0);
        source = source[..index] + after + source[(index + before.Length)..];
        foreach (var modern in new[] { false, true })
        {
            var plan = when_rendering_model_bound_projections.Plan(source, modern);
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            var assembly = when_rendering_read_model_absence.LoadWithArtifacts(when_rendering_read_model_absence.Files(plan), Artifacts(modern));
            var failure = await Catch.Exception(() => when_rendering_read_model_absence.RunProjected(assembly));
            Assert.NotNull(failure);
        }
    }

    internal static string Source(Example example)
    {
        const string identity = """
                  readmodel EventIdentity
                    id String
                  query IdentityById => EventIdentity?
                    by id String
                  projection EventIdentities => EventIdentity
                    from Recorded
                      id = $eventSourceId
                    from Changed
                      id = $eventSourceId
                    from Removed
                      id = $eventSourceId
            """;
        var source = Base + "\n" + Indent(example.Fields, 8) + "\n      query SnapshotById => Snapshot?\n        by id String\n      projection Snapshots => Snapshot\n" + Indent(example.Projection.Replace(" key id", " key sourceKey", StringComparison.Ordinal), 8) + "\n" + identity + "\n      specification SharedSnapshot\n";
        if (example.Seed) source += "        given readmodel Snapshot\n          id = \"target\"\n";
        else source += $"        {(example.Final is not null || example.Repeat ? "given" : "when append")} Recorded\n          for \"transport\"\n          sourceKey = \"target\"\n          value = \"one\"\n          amount = 2\n";
        if (example.Repeat) source += "        when append Recorded\n          for \"second-transport\"\n          sourceKey = \"target\"\n          value = \"two\"\n          amount = 2\n";
        if (example.Final is { } final)
        {
            source += $"        when append {final}\n          for \"{example.FinalSource}\"\n          sourceKey = \"target\"\n";
            if (final == "Changed") source += "          value = \"two\"\n          amount = 3\n";
        }
        source += example.Final == "Removed"
            ? "        then no readmodel Snapshot for \"target\"\n"
            : $"        then readmodel Snapshot\n          id = \"{example.Key}\"\n" + Indent(example.Expected, 10) + "\n";
        return source;
    }

    static async Task RunManualVector(string name)
    {
        var source = Source(Examples[name]);
        source = source[..source.IndexOf("      specification SharedSnapshot", StringComparison.Ordinal)];
        foreach (var modern in new[] { false, true })
        {
            var plan = when_rendering_model_bound_projections.Plan(source, modern);
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            var modelBound = modern && name != "once-context";
            var rendered = when_rendering_read_model_absence.Files(plan);
            if (name == "once-context")
            {
                Assert.Contains("IProjectionFor<Snapshot>", rendered.Single(file => file.RelativePath.EndsWith("/Lookup/Lookup.cs", StringComparison.Ordinal)).Content, StringComparison.Ordinal);
            }
            var files = rendered.Append(new RenderedFile("JoinIsolation.cs", $$"""
                using System;
                using System.Threading.Tasks;
                using Cratis.Chronicle.Events;
                using Cratis.Chronicle.EventSequences;
                using Cratis.Chronicle.Testing;
                using Cratis.Chronicle.Testing.ReadModels;
                using Xunit;

                namespace Projects.Checks.Facts.Lookup;

                public class JoinIsolation
                {
                    [Fact]
                    public async Task should_match_the_expected_snapshot()
                    {
                        var defaults = new Defaults(new InMemoryClientArtifactsProvider
                        {
                            EventTypes = [typeof(Recorded), typeof(Changed), typeof(Removed)],
                            {{(modelBound ? "ModelBoundProjections" : "Projections")}} = [typeof({{(modelBound ? "Snapshot" : "Snapshots")}})]
                        });
                        using var scenario = new ReadModelScenario<Snapshot>(default, defaults);
                        {{VectorBody(name)}}
                    }
                }
                """)).ToArray();
            Assert.Empty(RenderedOutput.Warnings(files));
            var assembly = RenderedOutput.Load(files);
            var fixture = assembly.GetType("Projects.Checks.Facts.Lookup.JoinIsolation", throwOnError: true)!;
            var execute = fixture.GetMethod("should_match_the_expected_snapshot")!;
            await (Task)execute.Invoke(Activator.CreateInstance(fixture), null)!;
        }
    }

    static string VectorBody(string name) => name == "join-isolation" ? """
        await scenario.Given.ForEventSource(new EventSourceId("transport")).Events(new Recorded("target", "one", 2));
        await scenario.Given.ForEventSource(new EventSourceId("unrelated")).Events(new Changed("target", "two", 3));
        var actual = scenario.InstanceForEventSourceId(new EventSourceId("target"));
        Assert.NotNull(actual);
        Assert.Equal("one", actual.Label);
        Assert.Null(actual.Joined);
        Assert.Null(scenario.InstanceForEventSourceId(new EventSourceId("unrelated")));
        """ : $$"""
        AppendedEvent Fact(ulong sequence, DateTimeOffset at) => new(EventContext.From(
            eventStore: "Projects", @namespace: "default", eventSourceType: EventSourceType.Default,
            eventStreamType: EventStreamType.All, eventStreamId: EventStreamId.Default,
            eventSourceId: "transport", eventType: typeof(Recorded).GetEventType(), sequenceNumber: sequence,
            correlationId: Guid.Empty, occurred: at), new Recorded("target", "one", 2));
        var first = new DateTimeOffset(2027, 3, 4, 10, 0, 0, TimeSpan.Zero);
        var second = first.AddMinutes(1);
        await scenario.ProcessAppendedEvents([Fact(42, first)]);
        var expectedFirst = {{(name == "once-context" ? "(DateTimeOffset?)null" : "first")}};
        Assert.Equal(expectedFirst, Assert.IsType<Snapshot>(scenario.InstanceForEventSourceId(new EventSourceId("target"))).Seen);
        await scenario.ProcessAppendedEvents([Fact(43, second)]);
        var expectedSecond = {{(name == "once-context" ? "(DateTimeOffset?)null" : "second")}};
        Assert.Equal(expectedSecond, Assert.IsType<Snapshot>(scenario.InstanceForEventSourceId(new EventSourceId("target"))).Seen);
        """;

    static string Artifacts(bool modern) => $$"""
         _scenario = new(default, new global::Cratis.Chronicle.Testing.Defaults(new global::Cratis.Chronicle.Testing.InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof({{Prefix}}Recorded), typeof({{Prefix}}Changed), typeof({{Prefix}}Removed)],
            {{(modern ? "ModelBoundProjections" : "Projections")}} = [typeof({{Prefix}}{{(modern ? "Snapshot" : "Snapshots")}})]
        }));
        """;

    static string Indent(string text, int spaces) => string.Join('\n', text.Split('\n').Where(line => line.Length > 0).Select(line => new string(' ', spaces) + line));

    internal sealed record Example(string Fields, string Projection, string Expected, string Attribute, bool Repeat, string Key = "target", string? Final = null, bool Seed = false, string FinalSource = "transport");
}
#endif
