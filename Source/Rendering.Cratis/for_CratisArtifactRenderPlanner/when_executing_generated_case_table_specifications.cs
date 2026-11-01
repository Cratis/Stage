// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_case_table_specifications(when_executing_generated_case_table_specifications.context fixture) : IClassFixture<when_executing_generated_case_table_specifications.context>
{
    [Fact] void should_build_without_warnings() => fixture.BuildWarningLines.ShouldEqual(string.Empty);
    [Fact] void should_execute_all_three_case_specifications() => fixture.Test.ShouldContain("Passed!");
    [Fact] void should_discover_each_case() => fixture.Cases.ShouldContainOnly("when_registering_small", "when_registering_medium", "when_registering_large");

    public class context : a_generated_application
    {
        protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(
            when_rendering_a_pure_reducer.Load(a_specification_case_table.Source).GetAwaiter().GetResult());

        async Task Because()
        {
            Build = await Run("case-table-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "-p:NoWarn=CS7022", "--nologo");
            Test = await Run("case-table-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--filter", "FullyQualifiedName~when_registering_", "--logger", "trx;LogFileName=cases.trx", "--results-directory", _evidence.FullName);
            var results = XDocument.Load(Path.Combine(_evidence.FullName, "cases.trx"));
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            Cases = [.. results.Descendants(ns + "TestMethod").Select(method => (string)method.Attribute("className")!).Distinct(StringComparer.Ordinal).Select(name => name.Split('.')[^1])];
        }

        public string Build { get; private set; } = null!;
        public string Test { get; private set; } = null!;
        public string[] Cases { get; private set; } = [];
        public string BuildWarningLines => BuildWarnings(Build);
    }
}
#endif
