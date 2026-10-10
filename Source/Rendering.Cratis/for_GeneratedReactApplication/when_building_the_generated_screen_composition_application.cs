// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// Building the generated React application for the canonical screen-composition corpus the way its author would:
/// the Debug backend build, which generates the Arc proxies the frontend imports, its generated specifications, an
/// npm install of the pinned packages, and the frontend build.
/// </summary>
/// <remarks>
/// <para>
/// Any failure in any step fails the specification with that step's output. Each step runs only when the step it
/// depends on succeeded, and says so when it did not.
/// </para>
/// <para>
/// With <c language="shell">STAGE_GENERATED_REACT_KEEP</c> set, the application is built in that directory and left there,
/// which is how the browser scenarios in <c language="shell">Verification/generated-react-browser</c> get an application
/// to serve.
/// </para>
/// </remarks>
/// <param name="built">The application, rendered and built once for every assertion.</param>
public class when_building_the_generated_screen_composition_application(when_building_the_generated_screen_composition_application.context built)
    : IClassFixture<when_building_the_generated_screen_composition_application.context>
{
    public const string KeepVariable = "STAGE_GENERATED_REACT_KEEP";

    [Fact] void should_build_the_debug_backend_and_its_proxies_without_warnings() => DotnetFailure(built.DebugBuild).ShouldEqual(string.Empty);
    [Fact] void should_pass_the_generated_specifications() => Failure(built.DebugTest).ShouldEqual(string.Empty);
    [Fact] void should_install_the_pinned_frontend_packages() => Failure(built.Install).ShouldEqual(string.Empty);
    [Fact] void should_build_the_frontend() => Failure(built.FrontendBuild).ShouldEqual(string.Empty);
    [Fact] void should_type_check_the_whole_frontend_without_errors() => built.FrontendBuild.Output.Contains("error TS", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_emit_the_built_application_into_the_web_root() => built.BuiltIndex.ShouldBeTrue();
    [Fact] void should_generate_the_proxy_every_route_is_read_from() => built.GeneratedProxy.ShouldBeTrue();

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
    public class context : a_screen_composition_render
    {
        static readonly TimeSpan _stepTimeout = TimeSpan.FromMinutes(8);

        string _application = null!;
        bool _keep;

        public ProcessResult DebugBuild { get; private set; } = null!;

        public ProcessResult DebugTest { get; private set; } = null!;

        public ProcessResult Install { get; private set; } = null!;

        public ProcessResult FrontendBuild { get; private set; } = null!;

        public bool BuiltIndex { get; private set; }

        public bool GeneratedProxy { get; private set; }

        void Establish()
        {
            var plan = Plan();
            if (!plan.Success)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, plan.Diagnostics.Select(_ => $"{_.Code}: {_.Message}")));
            }

            var kept = Environment.GetEnvironmentVariable(KeepVariable);
            _keep = !string.IsNullOrWhiteSpace(kept);
            _application = _keep ? Path.GetFullPath(kept!) : SpecTemporaryRoot.NewPath("stage-generated-react");
            if (_keep && Directory.Exists(_application))
            {
                Directory.Delete(_application, recursive: true);
            }

            Directory.CreateDirectory(_application);
            Write(plan, _application);
        }

        async Task Because()
        {
            DebugBuild = await ApplicationProcess.Run(_application, _stepTimeout, "dotnet", "build", "Workspaces.csproj", "-c", "Debug", "--nologo");
            DebugTest = DebugBuild.ExitCode == 0
                ? await ApplicationProcess.Run(_application, _stepTimeout, "dotnet", "test", "Workspaces.csproj", "-c", "Debug", "--no-build", "--nologo")
                : new(-1, "The generated specifications were not run because the Debug build failed.");
            Install = DebugBuild.ExitCode == 0
                ? await ApplicationProcess.Run(_application, _stepTimeout, "npm", "install", "--no-audit", "--no-fund")
                : new(-1, "The frontend was not installed because the Debug build, which generates the proxies it imports, failed.");
            FrontendBuild = Install.ExitCode == 0
                ? await ApplicationProcess.Run(_application, _stepTimeout, "npm", "run", "build")
                : new(-1, "The frontend was not built because its install failed.");
            BuiltIndex = File.Exists(Path.Combine(_application, "wwwroot", "index.html"));
            GeneratedProxy = File.Exists(Path.Combine(_application, "Workspaces", "Tracking", "AddComment", "AddComment.ts"));
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
