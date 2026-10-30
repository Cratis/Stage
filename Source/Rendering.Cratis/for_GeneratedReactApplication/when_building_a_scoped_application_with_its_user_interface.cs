// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// Building an application generated the way a host generates it slice by slice: the scaffold, then one scoped plan
/// that carries the shared user interface for the feature generated so far. The model's other feature is not
/// generated, so its proxies never exist; the frontend only builds if the bindings import exactly the proxies that do.
/// </summary>
/// <param name="built">The application, rendered and built once for every assertion.</param>
public class when_building_a_scoped_application_with_its_user_interface(when_building_a_scoped_application_with_its_user_interface.context built)
    : IClassFixture<when_building_a_scoped_application_with_its_user_interface.context>
{
    [Fact] void should_plan_the_slice_with_its_user_interface() => built.Planned.ShouldEqual(string.Empty);
    [Fact] void should_build_the_debug_backend_and_its_proxies() => Failure(built.DebugBuild).ShouldEqual(string.Empty);
    [Fact] void should_bind_the_command_of_the_generated_feature() => built.Bindings.ShouldContain("RegisterProject");
    [Fact] void should_bind_the_query_of_the_generated_feature() => built.Bindings.ShouldContain("ProjectById");
    [Fact] void should_not_bind_the_command_of_the_feature_that_is_not_generated() => built.Bindings.ShouldNotContain("ArchiveTask");
    [Fact] void should_generate_the_proxies_of_the_generated_feature() => built.GeneratedProxy.ShouldBeTrue();
    [Fact] void should_not_generate_the_proxy_of_the_feature_that_is_not_generated() => built.MissingProxy.ShouldBeFalse();
    [Fact] void should_install_the_pinned_frontend_packages() => Failure(built.Install).ShouldEqual(string.Empty);
    [Fact] void should_build_the_frontend() => Failure(built.FrontendBuild).ShouldEqual(string.Empty);
    [Fact] void should_type_check_the_bindings_against_the_generated_proxies() => built.FrontendBuild.Output.Contains("error TS", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_emit_the_built_application_into_the_web_root() => built.BuiltIndex.ShouldBeTrue();

    static string Failure(ProcessResult result) => result.ExitCode == 0 ? string.Empty : $"exit {result.ExitCode}{Environment.NewLine}{result.Output}";

    /// <summary>
    /// Renders and builds the application once for every assertion above.
    /// </summary>
    public class context : Specification
    {
        const string ArchivingSource = """
            concept TaskId : Uuid
            module Tasks
              feature Archiving
                slice StateChange ArchiveTask
                  command ArchiveTask
                    taskId TaskId identifier
                    produces TaskArchived
                      for taskId
                      taskId = taskId
                  event TaskArchived
                    taskId TaskId
            """;

        static readonly TimeSpan _stepTimeout = TimeSpan.FromMinutes(8);

        string _application = null!;

        public string Planned { get; private set; } = string.Empty;

        public string Bindings { get; private set; } = string.Empty;

        public ProcessResult DebugBuild { get; private set; } = null!;

        public ProcessResult Install { get; private set; } = null!;

        public ProcessResult FrontendBuild { get; private set; } = null!;

        public bool GeneratedProxy { get; private set; }

        public bool MissingProxy { get; private set; }

        public bool BuiltIndex { get; private set; }

        void Establish()
        {
            var compilation = when_rendering_authoring_documentation.Compile(when_rendering_authoring_documentation.Source + "\n" + ArchivingSource);
            var loaded = new LoadedSemanticModel(compilation.Model, SemanticExecutionPlan.Compile(compilation.Model).Plan!);
            var options = new CratisPlanOptions("Projects", "Projects", "Projects");
            var scaffold = CratisRendering.PlanScaffold(options);
            var feature = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Feature("Projects", "Registration")]), options with { UserInterface = new([]) });
            Planned = string.Join(Environment.NewLine, scaffold.Diagnostics.Concat(feature.Diagnostics)
                .Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error)
                .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
            Bindings = feature.Artifacts.SingleOrDefault(artifact => artifact.RelativePath == "src/bindings.ts") is { } bindings
                ? System.Text.Encoding.UTF8.GetString(bindings.Bytes.AsSpan())
                : string.Empty;

            _application = SpecTemporaryRoot.NewPath("stage-scoped-react");
            foreach (var artifact in scaffold.Artifacts.Concat(feature.Artifacts))
            {
                var path = Path.Combine(_application, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. artifact.Bytes]);
            }
        }

        async Task Because()
        {
            if (Planned.Length > 0)
            {
                DebugBuild = Install = FrontendBuild = new(-1, "The application was not built because it could not be planned.");
                return;
            }

            DebugBuild = await ApplicationProcess.Run(_application, _stepTimeout, "dotnet", "build", "Projects.csproj", "-c", "Debug", "--nologo");
            GeneratedProxy = HasProxies("RegisterProject") && HasProxies("ProjectLookup");
            MissingProxy = HasProxies("ArchiveTask");
            if (DebugBuild.ExitCode == 0)
            {
                await StageRuntimePackage.Install(_application);
            }

            Install = DebugBuild.ExitCode == 0
                ? await ApplicationProcess.Run(_application, _stepTimeout, "npm", "install", "--no-audit", "--no-fund")
                : new(-1, "The frontend was not installed because the Debug build, which generates the proxies it imports, failed.");
            FrontendBuild = Install.ExitCode == 0
                ? await ApplicationProcess.Run(_application, _stepTimeout, "npm", "run", "build")
                : new(-1, "The frontend was not built because its install failed.");
            BuiltIndex = File.Exists(Path.Combine(_application, "wwwroot", "index.html"));
        }

        bool HasProxies(string slice) => Directory.EnumerateFiles(_application, "*.ts", SearchOption.AllDirectories)
            .Any(path => path.Contains($"{Path.DirectorySeparatorChar}{slice}{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        void Destroy() => SpecTemporaryRoot.Delete(_application);
    }
}
