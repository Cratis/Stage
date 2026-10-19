// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// Building the generated React application for the blueprint-template vector (<c language="shell">Verification/blueprint-templates/model</c>): the default
/// blueprint's MasterDetail, Dashboard and ProfileSettings templates applied to screens, rendered the way their author would build them - the Debug backend, which
/// generates the Arc proxies the frontend imports, an npm install of the pinned packages and the frontend build (Cratis/Scene#95).
/// </summary>
/// <remarks>
/// With <c language="shell">STAGE_BLUEPRINT_TEMPLATES_KEEP</c> set, the application is built in that directory and left there, which is how the browser scenario in
/// <c language="shell">Verification/blueprint-templates</c> gets an application to serve.
/// </remarks>
/// <param name="built">The application, rendered and built once for every assertion.</param>
public class when_building_the_blueprint_templates_application(when_building_the_blueprint_templates_application.context built)
    : IClassFixture<when_building_the_blueprint_templates_application.context>
{
    public const string KeepVariable = "STAGE_BLUEPRINT_TEMPLATES_KEEP";

    [Fact] void should_build_the_debug_backend_and_its_proxies_without_warnings() => DotnetFailure(built.DebugBuild).ShouldEqual(string.Empty);
    [Fact] void should_install_the_pinned_frontend_packages() => Failure(built.Install).ShouldEqual(string.Empty);
    [Fact] void should_build_the_frontend() => Failure(built.FrontendBuild).ShouldEqual(string.Empty);
    [Fact] void should_type_check_the_whole_frontend_without_errors() => built.FrontendBuild.Output.Contains("error TS", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_emit_the_built_application_into_the_web_root() => built.BuiltIndex.ShouldBeTrue();

    static string Failure(ProcessResult result) => result.ExitCode == 0 ? string.Empty : $"exit {result.ExitCode}{Environment.NewLine}{result.Output}";

    static string DotnetFailure(ProcessResult result) =>
        result.ExitCode == 0 &&
        result.Output.Contains("0 Warning(s)", StringComparison.Ordinal) &&
        !result.Output.Contains(": warning ", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $"exit {result.ExitCode}{Environment.NewLine}{result.Output}";

    /// <summary>
    /// Renders and builds the application once for every assertion above.
    /// </summary>
    public class context : Specification
    {
        static readonly TimeSpan _stepTimeout = TimeSpan.FromMinutes(8);

        string _application = null!;
        bool _keep;

        public ProcessResult DebugBuild { get; private set; } = null!;

        public ProcessResult Install { get; private set; } = null!;

        public ProcessResult FrontendBuild { get; private set; } = null!;

        public bool BuiltIndex { get; private set; }

        static string Root([CallerFilePath] string path = "")
        {
            var directory = Directory.GetParent(path);
            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Verification")))
            {
                directory = directory.Parent;
            }

            return directory!.FullName;
        }

        async Task Establish()
        {
            var folder = Path.Combine(Root(), "Verification", "blueprint-templates", "model");
            var loaded = await SemanticModelLoader.LoadFromPathAsync(folder, null, "Workspaces");
            var compilation = new PlayFileCompiler().CompileFolder(folder).Result;
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            var scene = new ScreenplaySceneVisitor().Visit(compilation.Value!);

            var options = new CratisRenderingOptions("Workspaces", "Workspaces");
            var profile = CratisRendering.CreateProfile("Workspaces", options, scene, null, null, null);
            var plan = new CratisArtifactRenderPlanner().Plan(new ArtifactRenderRequest(loaded.Model, loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
            {
                ImplementationRequirements = loaded.ImplementationRequirements,
                ImplementationContents = loaded.ImplementationContents,
                AttachmentDiagnostics = loaded.AttachmentDiagnostics,
                TypedContextDescriptors = loaded.TypedContextDescriptors
            });
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics.Select(_ => $"{_.Code}: {_.Message}")));

            var kept = Environment.GetEnvironmentVariable(KeepVariable);
            _keep = !string.IsNullOrWhiteSpace(kept);
            _application = _keep ? Path.GetFullPath(kept!) : SpecTemporaryRoot.NewPath("stage-blueprint-templates");
            if (_keep && Directory.Exists(_application))
            {
                Directory.Delete(_application, recursive: true);
            }

            Directory.CreateDirectory(_application);
            foreach (var artifact in plan.Artifacts)
            {
                var path = Path.Combine(_application, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. artifact.Bytes]);
            }
        }

        async Task Because()
        {
            DebugBuild = await ApplicationProcess.Run(_application, _stepTimeout, "dotnet", "build", "Workspaces.slnx", "-c", "Debug", "--nologo");
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

        void Destroy()
        {
            if (!_keep)
            {
                SpecTemporaryRoot.Delete(_application);
            }
        }
    }
}
