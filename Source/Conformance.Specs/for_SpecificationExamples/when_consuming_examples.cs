// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Specifications;
using Cratis.Stage.Running;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationExamples;

public class when_consuming_examples : Specification
{
    const string Declarations = """
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                projectId String identifier
                name String
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name String
        """;

    const string Examples = """
              example Input : RegisterProject
                projectId = "project-1"
                name = "original"
              example Fact : ProjectRegistered
                name = "original"
              specification RegisteringAProject
                when Input name = "changed"
                then Fact
                  name = "changed"
              specification RegisteringAnotherProject
                given Fact name = "prior"
                  for "project-0"
                when Input
                  projectId = "project-2"
                then Fact
        """;

    const string Authored = """
              specification RegisteringAProject
                when RegisterProject
                  projectId = "project-1"
                  name = "changed"
                then ProjectRegistered
                  name = "changed"
              specification RegisteringAnotherProject
                given ProjectRegistered
                  name = "prior"
                  for "project-0"
                when RegisterProject
                  name = "original"
                  projectId = "project-2"
                then ProjectRegistered
                  name = "original"
        """;

    EventModel _examples = null!;
    EventModel _authored = null!;
    string _legacyExampleSpec = null!;
    string _legacyAuthoredSpec = null!;
    ArtifactRenderPlan _examplePlan = null!;
    ArtifactRenderPlan _authoredPlan = null!;
    SemanticSpecificationRunReport _exampleRun = null!;
    SemanticSpecificationRunReport _authoredRun = null!;

    async Task Because()
    {
        var exampleSource = Declarations + "\n" + Examples;
        var authoredSource = Declarations + "\n" + Authored;
        _examples = EventModelLoader.LoadFromSource(exampleSource);
        _authored = EventModelLoader.LoadFromSource(authoredSource);
        _legacyExampleSpec = LegacySpec(exampleSource);
        _legacyAuthoredSpec = LegacySpec(authoredSource);
        var exampleModel = Compile(exampleSource);
        var authoredModel = Compile(authoredSource);
        _examplePlan = Render(exampleModel);
        _authoredPlan = Render(authoredModel);
        _exampleRun = await Run(exampleModel);
        _authoredRun = await Run(authoredModel);
    }

    [Fact] void should_convert_the_same_legacy_model() => JsonSerializer.Serialize(_examples).ShouldEqual(JsonSerializer.Serialize(_authored));
    [Fact] void should_render_the_same_legacy_specification() => _legacyExampleSpec.ShouldEqual(_legacyAuthoredSpec);
    [Fact] void should_render_semantic_specifications_byte_for_byte() => Assert.Equal(_authoredPlan.Artifacts.Where(IsSpec).Select(artifact => artifact.Sha256), _examplePlan.Artifacts.Where(IsSpec).Select(artifact => artifact.Sha256));
    [Fact] void should_render_two_semantic_specifications() => _examplePlan.Artifacts.Count(IsSpec).ShouldEqual(2);
    [Fact] void should_pass_the_semantic_specifications() => _exampleRun.Results.Select(result => result.Outcome).ShouldContainOnly(SemanticSpecificationOutcome.Passed, SemanticSpecificationOutcome.Passed);
    [Fact] void should_run_with_the_same_semantic_outcomes() => Assert.Equal(_authoredRun.Results.Select(result => result.Outcome), _exampleRun.Results.Select(result => result.Outcome));

    [Fact]
    void should_run_with_the_same_legacy_outcomes()
    {
        var examples = Slice(_examples);
        var authored = Slice(_authored);
        var strategy = new StateChangeRunStrategy();
        Assert.Equal(authored.Specifications.Select(specification => strategy.Run(authored, specification).Outcome), examples.Specifications.Select(specification => strategy.Run(examples, specification).Outcome));
    }

    static bool IsSpec(PlannedArtifact artifact) => artifact.RelativePath.Contains("when_", StringComparison.Ordinal);

    static Slice Slice(EventModel model) => model.Collections.Single().Modules.Single().Features.Single().Slices.Single();

    static string LegacySpec(string source)
    {
        var result = new ScreenplayCompiler().Compile(source);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        var context = new ApplicationSet([result.Value!]);
        var slice = context.Slices.Single();
        return SpecificationRenderer.Render(slice.Slice.Specifications.First(), slice.Slice.Commands.Single(), slice, context, "Projects").Content;
    }

    static SemanticCompilation Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("examples"), "examples", "Projects.play", source);
        var result = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        return result.Value!;
    }

    static ArtifactRenderPlan Render(SemanticCompilation compilation)
    {
        var result = CratisRendering.Plan(compilation.Model, SemanticExecutionPlan.Compile(compilation.Model).Plan!,
            new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id), new("Projects", "Projects"));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        return result;
    }

    static async Task<SemanticSpecificationRunReport> Run(SemanticCompilation compilation) =>
        await new SemanticSpecificationExecutor().Run(SemanticExecutionPlan.Compile(compilation.Model).Plan!, new([]), new());
}
