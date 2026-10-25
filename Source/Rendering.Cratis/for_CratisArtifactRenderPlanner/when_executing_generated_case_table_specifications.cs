// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_case_table_specifications : a_generated_application
{
    string _build = null!;
    string _test = null!;
    string[] _cases = [];

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(
        when_rendering_a_pure_reducer.Load(a_specification_case_table.Source).GetAwaiter().GetResult());

    async Task Because()
    {
        _build = await Run("case-table-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "-p:NoWarn=CS7022", "--nologo");
        _test = await Run("case-table-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--filter", "FullyQualifiedName~when_registering_", "--logger", "trx;LogFileName=cases.trx", "--results-directory", _evidence.FullName);
        var results = XDocument.Load(Path.Combine(_evidence.FullName, "cases.trx"));
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        _cases = [.. results.Descendants(ns + "TestMethod").Select(method => (string)method.Attribute("className")!).Distinct(StringComparer.Ordinal).Select(name => name.Split('.')[^1])];
    }

    [Fact] void should_build_without_warnings() => BuildWarnings(_build).ShouldEqual(string.Empty);
    [Fact] void should_execute_all_three_case_specifications() => _test.ShouldContain("Passed!");
    [Fact] void should_discover_each_case() => _cases.ShouldContainOnly("when_registering_small", "when_registering_medium", "when_registering_large");
}
#endif
