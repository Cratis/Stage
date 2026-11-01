// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_specifications_with_an_explicit_root_namespace.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

// xUnit initializes the Specification context once per class, not once per Fact. No evidence is shared across runs.
public class when_executing_specifications_with_an_explicit_root_namespace(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_the_debug_application_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_the_release_application_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_execute_every_generated_fact() => fixture.Results.Length.ShouldEqual(13);
    [Fact] void should_pass_every_generated_fact() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_execute_only_the_explicit_namespace() => fixture.Results.All(_ => _.Matches(className: "Acme.projectAPI.", match: GeneratedTestNameMatch.Prefix)).ShouldBeTrue();
    [Fact] void should_execute_command_acceptance() => fixture.Results.Any(_ => _.Matches(methodName: "should_succeed")).ShouldBeTrue();
    [Fact] void should_execute_command_rejection() => new[] { "should_not_succeed", "should_have_validation_errors", "should_report_the_expected_first_error" }.All(fact => fixture.Results.Any(_ => _.Matches(methodName: fact))).ShouldBeTrue();
    [Fact] void should_execute_projection_assertions() => fixture.Results.Any(_ => _.Matches(methodName: "should_project_", match: GeneratedTestNameMatch.Prefix)).ShouldBeTrue();
    [Fact] void should_execute_the_query_assertion() => fixture.Results.Any(_ => _.Matches(methodName: "should_return_the_expected_read_model")).ShouldBeTrue();

    public class context : a_generated_application
    {
        public string DebugWarnings { get; private set; } = null!;
        public string ReleaseWarnings { get; private set; } = null!;
        public ImmutableArray<GeneratedTestResult> Results { get; private set; }

        async Task Because()
        {
            try
            {
                // The actual generated project runs package build targets and discovery generators. Loading a bare
                // Roslyn assembly does not reproduce that initialization and cannot verify CommandScenario behavior.
                DebugWarnings = BuildWarnings(await Run("debug-build.log", "build", "BackendHost.csproj", "-c", "Debug", "--nologo"));
                await Run("debug-test.log", "test", "BackendHost.csproj", "-c", "Debug", "--no-build", "--nologo", "--logger", "trx;LogFileName=generated.trx", "--results-directory", _evidence.FullName);
                ReleaseWarnings = BuildWarnings(await Run("release-build.log", "build", "BackendHost.csproj", "-c", "Release", "--nologo"));
                var results = XDocument.Load(Path.Combine(_evidence.FullName, "generated.trx"));
                Results = GeneratedTestResults.Read(results);
            }
            finally
            {
                // Assertions need only immutable results; even failed builds/tests must not retain their build tree.
                Cleanup();
            }
        }
    }
}
#endif
