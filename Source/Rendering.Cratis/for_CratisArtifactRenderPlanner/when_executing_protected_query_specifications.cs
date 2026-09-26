// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Exercises allowed and denied protected queries through the generated Arc policy and pipeline.
/// </summary>
public class when_executing_protected_query_specifications : a_generated_application
{
    const string StreamingProbe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using System.Reactive.Subjects;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Arc.Testing.Queries;
        using Xunit;

        namespace Projects.Probes;

        public class when_refusing_a_streaming_query_scenario
        {
            [Fact]
            public async Task should_refuse_before_invoking_the_query()
            {
                StreamingQueryProbe.Invocations = 0;
                await using var scenario = new QueryScenario<StreamingQueryProbe>();
                await Assert.ThrowsAsync<StreamingQueryNotSupported>(() => scenario.Perform(nameof(StreamingQueryProbe.Stream)));
                Assert.Equal(0, StreamingQueryProbe.Invocations);
            }
        }

        [ReadModel]
        public record StreamingQueryProbe(string Name)
        {
            public static int Invocations;

            [AllowAnonymous]
            public static ISubject<StreamingQueryProbe> Stream()
            {
                Interlocked.Increment(ref Invocations);
                return new Subject<StreamingQueryProbe>();
            }
        }
        #endif
        """;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = "policy Clerks\n  require authenticated\n" + when_rendering_scoped_projections.ScopedSource
            .Replace("query ProjectById => ProjectSummary?\n", "query ProjectById => ProjectSummary?\n        authorize Clerks\n", StringComparison.Ordinal)
            .Replace("specification RegisteringAProject\n", "specification RegisteringAProject\n        given caller\n          authenticated\n", StringComparison.Ordinal)
            .Replace("specification LookingUpPinnedProject\n", "specification DenyingAnonymousProjectLookup\n        given caller\n          role \"Guest\"\n        then query ProjectById\n          arguments\n            projectId = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n        then denied\n      specification LookingUpPinnedProject\n", StringComparison.Ordinal);

        // Arc's protected query scenario can only replay facts from the queried stream.
        var givenStart = source.LastIndexOf('\n', source.IndexOf("given ProjectRegistered\n", StringComparison.Ordinal)) + 1;
        var whenStart = source.LastIndexOf('\n', source.IndexOf("when RegisterProject\n", givenStart, StringComparison.Ordinal)) + 1;
        source = source.Remove(givenStart, whenStart - givenStart);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("queries"), "queries", "Queries.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        var plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    string _debug = null!;
    string _release = null!;
    string _results = null!;

    async Task Because()
    {
        AddGeneratedSpecification("StreamingQueryProbe.cs", StreamingProbe);
        _debug = await Run("query-debug.log", "build", "-c", "Debug", "-warnaserror");
        _release = await Run("query-release.log", "build", "-c", "Release", "-warnaserror");
        _results = await Run("query-tests.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_build_and_pass_allowed_and_denied_query_specifications()
    {
        BuildWarnings(_debug).ShouldEqual(string.Empty);
        BuildWarnings(_release).ShouldEqual(string.Empty);
        _results.ShouldContain("Passed!");
        ReadGeneratedFile("Projects/Registration/RegisterProject/when_registering_aproject_is_queried.cs").ShouldContain("_scenario.Perform(");
        ReadGeneratedFile("Projects/Registration/RegisterProject/when_denying_anonymous_project_lookup_is_queried.cs").ShouldContain("should_return_no_data");
    }
}
#endif
