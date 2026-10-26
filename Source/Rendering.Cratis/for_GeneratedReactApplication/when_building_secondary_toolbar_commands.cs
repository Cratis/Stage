// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// Builds the secondary-command browser fixture with the packed candidate runtime, not a workspace alias.
/// </summary>
/// <param name="built">The generated application built once for all assertions.</param>
public class when_building_secondary_toolbar_commands(when_building_secondary_toolbar_commands.context built)
    : IClassFixture<when_building_secondary_toolbar_commands.context>
{
    [Fact] void should_build_the_backend_and_generate_proxies() => Failure(built.Backend).ShouldEqual(string.Empty);
    [Fact] void should_pass_generated_specifications() => Failure(built.Specifications).ShouldEqual(string.Empty);
    [Fact] void should_install_the_candidate_packages() => Failure(built.Install).ShouldEqual(string.Empty);
    [Fact] void should_build_the_frontend() => Failure(built.Frontend).ShouldEqual(string.Empty);
    [Fact] void should_share_registry_and_context_packages() => built.SingleInstanceFailure.ShouldEqual(string.Empty);
    [Fact] void should_record_the_installed_candidate_and_bundle_identity() => built.CandidateVerified.ShouldBeTrue();
    [Fact] void should_need_no_unrelated_slice_button() => built.NamesSliceButtons.ShouldBeFalse();

    static string Failure(ProcessResult result) => result.ExitCode == 0 ? string.Empty : $"exit {result.ExitCode}{Environment.NewLine}{result.Output}";

    /// <summary>
    /// Renders and builds the application through the existing packed-runtime harness.
    /// </summary>
    public class context : a_secondary_toolbar_render
    {
        static readonly TimeSpan _stepTimeout = TimeSpan.FromMinutes(8);
        static readonly System.Text.Json.JsonSerializerOptions _proofOptions = new() { WriteIndented = true };
        string _application = null!;
        bool _keep;

        public ProcessResult Backend { get; private set; } = new(-1, "Not run.");

        public ProcessResult Specifications { get; private set; } = new(-1, "The backend did not build.");

        public ProcessResult Install { get; private set; } = new(-1, "The generated specifications did not pass.");

        public ProcessResult Frontend { get; private set; } = new(-1, "The packages were not installed.");

        public string SingleInstanceFailure { get; private set; } = "The packages were not installed.";

        public bool CandidateVerified { get; private set; }

        public bool NamesSliceButtons { get; private set; }

        void Establish()
        {
            var plan = PlanApplication();
            NamesSliceButtons = CanonicalSceneJson.NamesSlices(JsonNode.Parse(System.Text.Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == StageSceneDocument.RelativePath).Bytes.AsSpan()))!);
            var kept = Environment.GetEnvironmentVariable(when_building_the_generated_screen_composition_application.KeepVariable);
            _keep = !string.IsNullOrWhiteSpace(kept);
            _application = _keep ? Path.GetFullPath(kept!) : SpecTemporaryRoot.NewPath("stage-secondary-commands");

            // Never replace a retained app from a different invocation or lane.
            Assert.False(Directory.Exists(_application), $"The candidate destination already exists: {_application}");
            foreach (var artifact in plan.Artifacts)
            {
                var path = Path.Combine(_application, artifact.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. artifact.Bytes]);
            }
        }

        async Task Because()
        {
            Backend = await ApplicationProcess.Run(_application, _stepTimeout, "dotnet", "build", "Workspaces.slnx", "-c", "Debug", "--nologo");
            if (Backend.ExitCode != 0) return;
            Specifications = await ApplicationProcess.Run(_application, _stepTimeout, "dotnet", "test", "Workspaces.csproj", "-c", "Debug", "--no-build", "--nologo");
            if (Specifications.ExitCode != 0) return;
            await StageRuntimePackage.Install(_application);
            Install = await ApplicationProcess.Run(_application, _stepTimeout, "npm", "install", "--no-audit", "--no-fund");
            if (Install.ExitCode != 0) return;
            SingleInstanceFailure = await StageRuntimePackage.SingleInstanceFailure(_application);
            Frontend = await ApplicationProcess.Run(_application, _stepTimeout, "npm", "run", "build");
            if (Frontend.ExitCode != 0) return;
            RecordCandidate();
        }

        void RecordCandidate()
        {
            var package = JsonNode.Parse(File.ReadAllText(Path.Combine(_application, "package.json")))!;
            var dependency = package["dependencies"]!["@cratis/stage.runtime"]!.GetValue<string>();
            Assert.StartsWith("file:", dependency);
            var tarball = dependency["file:".Length..];
            var lockfile = JsonNode.Parse(File.ReadAllText(Path.Combine(_application, "package-lock.json")))!;
            var installed = lockfile["packages"]!["node_modules/@cratis/stage.runtime"]!;
            var integrity = $"sha512-{Convert.ToBase64String(SHA512.HashData(File.ReadAllBytes(tarball)))}";
            Assert.Equal(integrity, installed["integrity"]!.GetValue<string>());
            var entry = Path.Combine(_application, "node_modules", "@cratis", "stage.runtime", "dist", "src", "index.js");
            Assert.True(File.Exists(entry));
            var bundles = Directory.GetFiles(Path.Combine(_application, "wwwroot", "assets"), "*.js");
            Assert.NotEmpty(bundles);
            var proof = new
            {
                tarball,
                sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(tarball))),
                integrity,
                resolved = installed["resolved"]!.GetValue<string>(),
                entry,
                bundles = bundles.Select(path => new { path, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) }).ToArray(),
                publishedPinEquivalence = "unknown"
            };
            File.WriteAllText(Path.Combine(_application, "runtime-proof.json"), System.Text.Json.JsonSerializer.Serialize(proof, _proofOptions));
            CandidateVerified = true;
        }

        void Destroy()
        {
            if (!_keep) SpecTemporaryRoot.Delete(_application);
        }
    }
}
