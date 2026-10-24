// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_building_beneath_conflicting_parent_msbuild_files(when_building_beneath_conflicting_parent_msbuild_files.context fixture) : IClassFixture<when_building_beneath_conflicting_parent_msbuild_files.context>
{
    [Fact] void should_test_the_debug_scaffold_successfully() => fixture.Debug.ExitCode.ShouldEqual(0);
    [Fact] void should_discover_the_inline_specification() => fixture.Discovery.Output.ShouldContain("SmokeApp.Specs.when_probing.should_allow_bdd_names");
    [Fact] void should_publish_no_specification_types_or_references() => ReleaseSpecificationMetadata.Verify(fixture.PublishedPath);
    [Fact] void should_build_the_release_scaffold_successfully() => fixture.Release.ExitCode.ShouldEqual(0);
    [Fact] void should_not_import_the_conflicting_parent_build_properties() => $"{fixture.Debug.Output}{fixture.Release.Output}".ShouldNotContain("conflicting parent build properties");
    [Fact] void should_not_import_the_conflicting_parent_build_targets() => $"{fixture.Debug.Output}{fixture.Release.Output}".ShouldNotContain("conflicting parent build targets");
    [Fact] void should_build_without_analyzer_warnings() => $"{fixture.Debug.Output}{fixture.Release.Output}".ShouldNotContain(": warning ");
    [Fact]
    void should_execute_the_specification_with_bdd_names()
    {
        var results = XDocument.Load(fixture.ResultsPath);
        var counters = results.Descendants().Single(element => element.Name.LocalName == "Counters");
        foreach (var name in new[] { "total", "executed", "passed" }) counters.Attribute(name)!.Value.ShouldEqual("1");
        foreach (var name in new[] { "failed", "error", "timeout", "aborted", "notExecuted" }) counters.Attribute(name)!.Value.ShouldEqual("0");
        var result = results.Descendants().Single(element => element.Name.LocalName == "UnitTestResult");
        result.Attribute("testName")!.Value.ShouldEqual("SmokeApp.Specs.when_probing.should_allow_bdd_names");
        result.Attribute("outcome")!.Value.ShouldEqual("Passed");
    }
    [Fact] void should_reject_violations_of_the_application_editor_configuration() => fixture.OwnRules.ExitCode.ShouldNotEqual(0);
    [Fact] void should_report_the_application_naming_rule_violation() => fixture.OwnRules.Output.ShouldContain("error IDE1006");

    public class context : Specification
    {
        DirectoryInfo _root = null!;
        DirectoryInfo _application = null!;
        public ProcessResult Discovery { get; private set; } = null!;
        public ProcessResult Debug { get; private set; } = null!;
        public ProcessResult Release { get; private set; } = null!;
        public ProcessResult OwnRules { get; private set; } = null!;
        public string ResultsPath => Path.Combine(_application.FullName, "specifications.trx");
        public string PublishedPath => Path.Combine(_application.FullName, "published");

        void Establish()
        {
            _root = new(Path.Combine(Path.GetTempPath(), $"stage-scaffold-smoke-{Guid.NewGuid():N}"));
            _application = _root.CreateSubdirectory("application");
            File.WriteAllText(
                Path.Combine(_root.FullName, "Directory.Build.props"),
                """
                <Project>
                  <Target Name="RejectNestedBuild" BeforeTargets="PrepareForBuild">
                    <Error Text="The scaffold inherited conflicting parent build properties." />
                  </Target>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(_root.FullName, "Directory.Build.targets"),
                """
                <Project>
                  <Target Name="RejectNestedTargetBuild" BeforeTargets="PrepareForBuild">
                    <Error Text="The scaffold inherited conflicting parent build targets." />
                  </Target>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(_root.FullName, "Directory.Packages.props"),
                """
                <Project>
                  <PropertyGroup>
                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                </Project>
                """);

            File.WriteAllText(Path.Combine(_root.FullName, ".globalconfig"), """
                is_global = true
                dotnet_diagnostic.IDE1006.severity = warning
                dotnet_naming_rule.parent_types.symbols = types
                dotnet_naming_rule.parent_types.style = parent_prefix
                dotnet_naming_rule.parent_types.severity = warning
                dotnet_naming_symbols.types.applicable_kinds = class
                dotnet_naming_style.parent_prefix.required_prefix = Parent
                dotnet_naming_style.parent_prefix.capitalization = pascal_case
                """);
            File.WriteAllText(Path.Combine(_root.FullName, ".editorconfig"), """
                root = true
                [*.cs]
                csharp_style_namespace_declarations = block_scoped:warning
                dotnet_diagnostic.IDE0160.severity = warning
                """);

            foreach (var input in new CratisBackendApplicationScaffold().Create(CratisBackendApplicationScaffoldRequest.Create("SmokeApp")))
            {
                var path = Path.Combine(_application.FullName, input.Name["cratis-scaffold:text:".Length..]);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. input.Bytes]);
            }

            const string methodNaming = """
                [*.cs]
                dotnet_diagnostic.IDE1006.severity = warning
                dotnet_naming_rule.methods.symbols = methods
                dotnet_naming_rule.methods.style = pascal
                dotnet_naming_rule.methods.severity = warning
                dotnet_naming_symbols.methods.applicable_kinds = method
                dotnet_naming_style.pascal.capitalization = pascal_case
                """;
            var editorConfigPath = Path.Combine(_application.FullName, ".editorconfig");
            File.WriteAllText(editorConfigPath, File.ReadAllText(editorConfigPath).Replace("[*.cs]", methodNaming, StringComparison.Ordinal));
            File.WriteAllText(Path.Combine(_application.FullName, "AnalyzerProbe.cs"), "namespace SmokeApp; public class AnalyzerProbe { public void ValidName() { } }");
            Directory.CreateDirectory(Path.Combine(_application.FullName, "for_Probe"));
            File.WriteAllText(Path.Combine(_application.FullName, "for_Probe", "when_probing.cs"), """
                using Cratis.Specifications;
                using Xunit;

                namespace SmokeApp.Specs;

                public class when_probing : Specification
                {
                    [Fact] void should_allow_bdd_names() => true.ShouldBeTrue();
                }
                """);
        }

        async Task Because()
        {
            Discovery = await Run("run", "--project", "SmokeApp.csproj", "-c", "Debug", "--", "--specifications", "-list", "full");
            Discovery.ExitCode.ShouldEqual(0);
            Debug = await Run("run", "--project", "SmokeApp.csproj", "-c", "Debug", "--", "--specifications", "-filterVSTest", "FullyQualifiedName=SmokeApp.Specs.when_probing.should_allow_bdd_names", "-result-trx", ResultsPath);
            Debug.ExitCode.ShouldEqual(0);
            Release = await Run("publish", "SmokeApp.csproj", "-c", "Release", "--nologo", "-o", PublishedPath);
            Release.ExitCode.ShouldEqual(0);

            // Release has a different package graph: restore the Debug-only specification references before probing its rules.
            var debugRestore = await Run("restore", "SmokeApp.csproj", "-p:Configuration=Debug", "--nologo");
            debugRestore.ExitCode.ShouldEqual(0);
            await File.WriteAllTextAsync(Path.Combine(_application.FullName, "AnalyzerProbe.cs"), "namespace SmokeApp; public class AnalyzerProbe { public void invalidName() { } }");
            OwnRules = await Run("build", "SmokeApp.csproj", "-c", "Debug", "--no-restore", "--nologo");
        }

        void Destroy()
        {
            if (_root.Exists)
            {
                _root.Delete(recursive: true);
            }
        }


        async Task<ProcessResult> Run(params string[] arguments)
        {
            // Bounded so a local run does not start every generated application's build at once.
            using var slot = await GeneratedApplicationBuilds.Enter();
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = _application.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            // A generated application is a multi-project solution, so MSBuild starts worker nodes. Reused nodes outlive the build and keep the
            // redirected output pipes open, so reading the output would never finish. Build in fresh nodes that exit with the command.
            startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
            startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
            foreach (var argument in GeneratedApplicationBuilds.Capped("dotnet", arguments))
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            var output = $"{await standardOutput}{await standardError}";
            return new(process.ExitCode, output);
        }

        public sealed record ProcessResult(int ExitCode, string Output);
    }
}
