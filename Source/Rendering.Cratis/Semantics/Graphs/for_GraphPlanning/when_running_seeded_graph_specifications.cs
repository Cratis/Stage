// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_GraphPlanning;

public class when_running_seeded_graph_specifications : Specification
{
    ArtifactRenderPlan _plan = null!;
    RenderedFile[] _files = [];

    void Establish()
    {
        _plan = new CratisArtifactRenderPlanner().Plan(a_seeded_graph_model.Request(a_seeded_graph_model.Source));
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        _files = Files(_plan);
    }

    [Fact] void should_plan_seeded_and_event_only_specifications() => _plan.Artifacts.Count(artifact => artifact.RelativePath.StartsWith("Graphs/Specifications/", StringComparison.Ordinal)).ShouldEqual(6);
    [Fact] void should_not_degrade_seeded_specifications() => _plan.Degradations.Any(degradation => degradation.Diagnostic.Message.Contains("given graph", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_compile_against_the_pinned_graph_scenario() => string.Join(Environment.NewLine, RenderedOutput.Errors(_files)).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, RenderedOutput.Warnings(_files)).ShouldEqual(string.Empty);
    [Fact] void should_keep_the_existing_unconditional_specifications() => _files.Where(file => file.RelativePath.StartsWith("Graphs/Specifications/", StringComparison.Ordinal)).ShouldEachConformTo(file => !file.Content.Contains("#if DEBUG", StringComparison.Ordinal));
    [Fact] async Task should_pass_seeded_and_event_only_assertions() => await Run(_files);
    [Fact] async Task should_pass_the_canonical_v38_assertions() => await Run(Files(new CratisArtifactRenderPlanner().Plan(a_seeded_graph_model.Request())));
    [Fact]
    async Task should_fail_when_a_seeded_property_is_changed()
    {
        var changed = _files.Select(file => file with { Content = file.Content.Replace("[\"name\"] = \"Ada\"", "[\"name\"] = \"Wrong\"", StringComparison.Ordinal) }).ToArray();
        var failure = await Assert.ThrowsAsync<TargetInvocationException>(() => Run(changed));
        failure.InnerException!.Message.ShouldContain("Wrong");
    }

    static RenderedFile[] Files(ArtifactRenderPlan plan)
    {
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return [.. plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && !artifact.RelativePath.EndsWith("Program.cs", StringComparison.Ordinal))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
    }

    static async Task Run(RenderedFile[] files)
    {
        // Dynamically loaded output is outside the default discovery snapshot. Give each scenario its own catalog.
        var assembly = RenderedOutput.Load(files.Select(file => file with
        {
            Content = file.RelativePath.StartsWith("Graphs/Specifications/", StringComparison.Ordinal)
                ? file.Content.Replace(" _scenario = new();", " _scenario = new(new global::Cratis.Chronicle.Testing.Defaults(new SeededGraphArtifacts()));", StringComparison.Ordinal)
                : file.Content
        }).Append(new RenderedFile("SeededGraphArtifacts.cs", Artifacts)));
        var specifications = assembly.GetTypes().Where(type => type.Name.StartsWith("when_", StringComparison.Ordinal) && type.Name.EndsWith("_is_graphed", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(specifications);
        foreach (var type in specifications)
        {
            using var instance = (IDisposable)Activator.CreateInstance(type)!;
            if (type.GetMethod("Establish", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(instance, null) is Task establishment) await establishment;
            await (Task)type.GetMethod("Because", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(instance, null)!;
            var facts = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(method => Attribute.IsDefined(method, typeof(FactAttribute))).ToArray();
            Assert.NotEmpty(facts);
            foreach (var fact in facts) fact.Invoke(instance, null);
        }
    }

    const string Artifacts = """
        sealed class SeededGraphArtifacts : global::Cratis.Chronicle.Testing.InMemoryClientArtifactsProvider, global::Cratis.Chronicle.IClientArtifactsProvider
        {
            public SeededGraphArtifacts()
            {
                EventTypes = typeof(SeededGraphArtifacts).Assembly.GetTypes().Where(type => type.IsDefined(typeof(global::Cratis.Chronicle.Events.EventTypeAttribute), false));
            }

            public global::System.Collections.Generic.IEnumerable<global::System.Type> GraphElements => typeof(SeededGraphArtifacts).Assembly.GetTypes().Where(type =>
                type.IsDefined(typeof(global::Cratis.Chronicle.Projections.Graphs.NodeAttribute), false) || type.IsDefined(typeof(global::Cratis.Chronicle.Projections.Graphs.EdgeAttribute), false));
        }
        """;
}
#endif
