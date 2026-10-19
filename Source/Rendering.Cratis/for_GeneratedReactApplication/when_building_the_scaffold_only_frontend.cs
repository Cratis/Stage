// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// Builds only the first-run scaffold, before a model render or backend proxy generation has supplied any Scene files.
/// </summary>
/// <param name="built">The isolated scaffold frontend, installed and built once for all assertions.</param>
public class when_building_the_scaffold_only_frontend(when_building_the_scaffold_only_frontend.context built)
    : IClassFixture<when_building_the_scaffold_only_frontend.context>
{
    [Fact] void should_install_the_pinned_packages() => Failure(built.Install).ShouldEqual(string.Empty);
    [Fact] void should_type_check_without_a_scene_module() => Failure(built.TypeCheck).ShouldEqual(string.Empty);
    [Fact] void should_build_the_production_frontend_without_a_scene_module() => Failure(built.Build).ShouldEqual(string.Empty);
    [Fact] void should_emit_the_application_into_the_web_root() => built.BuiltIndex.ShouldBeTrue();
    [Fact] void should_not_require_rendered_scene_artifacts() => built.HasSceneArtifacts.ShouldBeFalse();

    static string Failure(ProcessResult result) => result.ExitCode == 0 ? string.Empty : $"exit {result.ExitCode}{Environment.NewLine}{result.Output}";

    /// <summary>
    /// Installs and builds the scaffold in a fresh directory using only its planned artifacts.
    /// </summary>
    public class context : Specification
    {
        static readonly TimeSpan _stepTimeout = TimeSpan.FromMinutes(3);
        string _application = null!;

        public ProcessResult Install { get; private set; } = null!;
        public ProcessResult TypeCheck { get; private set; } = null!;
        public ProcessResult Build { get; private set; } = null!;
        public bool BuiltIndex { get; private set; }
        public bool HasSceneArtifacts { get; private set; }

        void Establish()
        {
            var plan = CratisRendering.PlanScaffold(new("FirstRun", "FirstRun", "FirstRun"));
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            _application = SpecTemporaryRoot.NewPath("stage-scaffold-frontend");
            foreach (var artifact in plan.Artifacts)
            {
                var path = Path.Combine(_application, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. artifact.Bytes]);
            }
        }

        async Task Because()
        {
            await StageRuntimePackage.Install(_application);
            Install = await ApplicationProcess.Run(_application, _stepTimeout, "npm", "install", "--no-audit", "--no-fund");
            TypeCheck = Install.ExitCode == 0
                ? await ApplicationProcess.Run(_application, _stepTimeout, "npm", "exec", "--no", "--", "tsc", "--noEmit", "--project", ".frontend/tsconfig.json")
                : new(-1, "The type-check was not run because package installation failed.");
            Build = TypeCheck.ExitCode == 0
                ? await ApplicationProcess.Run(_application, _stepTimeout, "npm", "exec", "--no", "--", "vite", "build", "--config", ".frontend/vite.config.ts")
                : new(-1, "The production build was not run because the type-check failed.");
            BuiltIndex = File.Exists(Path.Combine(_application, "wwwroot", "index.html"));
            HasSceneArtifacts = File.Exists(Path.Combine(_application, "src", "stage.ts")) || File.Exists(Path.Combine(_application, "src", "stage-scene.json"));
        }

        void Destroy() => SpecTemporaryRoot.Delete(_application);
    }
}
