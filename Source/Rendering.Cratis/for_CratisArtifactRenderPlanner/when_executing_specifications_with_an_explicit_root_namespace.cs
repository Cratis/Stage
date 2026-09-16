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
    [Fact] void should_execute_every_generated_fact() => fixture.Results.Length.ShouldEqual(7);
    [Fact] void should_pass_every_generated_fact() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_execute_only_the_explicit_namespace() => fixture.Results.All(_ => _.Name.StartsWith("Acme.projectAPI.", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_execute_command_acceptance() => fixture.Results.Any(_ => _.Name.EndsWith(".should_succeed", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_execute_command_rejection() => new[] { "should_not_succeed", "should_have_validation_errors" }.All(fact => fixture.Results.Any(_ => _.Name.EndsWith($".{fact}", StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_execute_projection_assertions() => fixture.Results.Any(_ => _.Name.Contains(".should_project_", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_execute_the_query_assertion() => fixture.Results.Any(_ => _.Name.EndsWith(".should_return_the_expected_read_model", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_generated_application
    {
        public string DebugWarnings { get; private set; } = null!;
        public string ReleaseWarnings { get; private set; } = null!;
        public ImmutableArray<(string Name, string Outcome)> Results { get; private set; }

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
                XNamespace testNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
                Results = [.. results.Descendants(testNamespace + "UnitTestResult").Select(_ => (_.Attribute("testName")!.Value, _.Attribute("outcome")!.Value))];
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
