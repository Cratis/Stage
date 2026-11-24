// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;

// Freeze structure independently of package releases. Only exact package/image tokens are normalized;
// a missing package, a wrong pin or a changed application source still changes the digest.
internal static class FrozenScaffoldPins
{
    internal static string TargetVersion => "22.109.0";

    internal static string Normalize(string path, string text, bool previous = false)
    {
        var profile = CratisBackendApplicationScaffoldProfile.Current;
        if (path.EndsWith("Directory.Packages.props", StringComparison.Ordinal)) text = NormalizeNugetPins(text, profile, previous);
        if (path.EndsWith("package.json", StringComparison.Ordinal)) text = NormalizeFrontendPins(text, profile.FrontendPackageSet, previous);
        text = WithoutYarnChanges(text, previous);
        return text.Replace($"cratis/chronicle:{profile.ChronicleImageVersion}-development", $"cratis/chronicle:{(previous ? "19.55.2" : "19.71.0")}-development", StringComparison.Ordinal);
    }

    static string NormalizeNugetPins(string text, CratisBackendApplicationScaffoldProfile profile, bool previous)
    {
        var cratis = previous ? "22.87.0" : TargetVersion;
        var chronicle = previous ? "19.55.2" : "19.71.0";
        (string Id, string Current, string Baseline)[] pins =
        [
            ("Cratis", profile.CratisPackageVersion, cratis),
            ("Cratis.Arc.MongoDB", profile.CratisArcMongoDBPackageVersion, cratis),
            ("Cratis.Arc.Chronicle.Testing", profile.CratisArcChronicleTestingPackageVersion, cratis),
            ("Cratis.Chronicle", profile.CratisChroniclePackageVersion, chronicle),
            ("Cratis.Chronicle.AspNetCore", profile.CratisChroniclePackageVersion, chronicle),
            ("Cratis.Chronicle.Testing", profile.CratisChroniclePackageVersion, chronicle),
            ("Cratis.Choreography", ScaffoldPackageVersions.Choreography, "2.2.0"),
            ("Cratis.Choreography.Chronicle", ScaffoldPackageVersions.ChoreographyChronicle, "2.2.0"),
            ("Cratis.Screenplay", ScaffoldPackageVersions.Screenplay, "4.241.0"),
            ("Cratis.Screenplay.Matching", ScaffoldPackageVersions.ScreenplayMatching, "4.241.0"),
            ("Cratis.Specifications", profile.CratisSpecificationsPackageVersion, "4.1.1"),
            ("Cratis.Specifications.XUnit", profile.CratisSpecificationsXUnitPackageVersion, "4.1.1"),
            ("Microsoft.Identity.Web", ScaffoldPackageVersions.IdentityWeb, "4.16.1"),
            ("Microsoft.Extensions.Telemetry.Abstractions", ScaffoldPackageVersions.TelemetryAbstractionsNet10, "10.11.0"),
            ("Microsoft.Extensions.Telemetry.Abstractions", ScaffoldPackageVersions.TelemetryAbstractionsNet11, "10.11.0"),
            ("Microsoft.NET.Test.Sdk", profile.MicrosoftNetTestSdkPackageVersion, "18.10.1"),
            ("NSubstitute", profile.NSubstitutePackageVersion, "6.2.0"),
            ("System.Security.Cryptography.Pkcs", ScaffoldPackageVersions.CryptographyPkcs, "10.0.13"),
            ("xunit", profile.XunitPackageVersion, "2.9.3"),
            ("xunit.runner.visualstudio", profile.XunitRunnerVisualStudioPackageVersion, "4.0.0")
        ];
        foreach (var pin in pins)
        {
            text = text.Replace($"Include=\"{pin.Id}\" Version=\"{pin.Current}\"", $"Include=\"{pin.Id}\" Version=\"{pin.Baseline}\"", StringComparison.Ordinal);
        }

        return text;
    }

    static string NormalizeFrontendPins(string text, CratisFrontendPackageSet frontend, bool previous)
    {
        var scene = previous ? "4.22.1" : "4.67.0";
        (string Id, string Current, string Baseline)[] pins =
        [
            ("arc", frontend.ArcPackageVersion, "22.58.2"),
            ("arc.react", frontend.ArcReactPackageVersion, "22.58.2"),
            ("arc.vite", frontend.ArcVitePackageVersion, "22.58.2"),
            ("fundamentals", frontend.FundamentalsPackageVersion, "7.22.8"),
            ("components", frontend.ComponentsPackageVersion, "4.26.2"),
            ("scene.blueprint.default", frontend.ScenePackageVersion, scene),
            ("scene.components", frontend.ScenePackageVersion, scene),
            ("scene.engine", frontend.ScenePackageVersion, scene),
            ("scene.model", frontend.ScenePackageVersion, scene),
            ("scene.primereact", frontend.ScenePackageVersion, scene),
            ("scene.react", frontend.ScenePackageVersion, scene),
            ("stage.runtime", ScaffoldPackageVersions.StageRuntime, "1.0.0")
        ];
        foreach (var pin in pins)
        {
            text = text.Replace($"\"@cratis/{pin.Id}\": \"{pin.Current}\"", $"\"@cratis/{pin.Id}\": \"{pin.Baseline}\"", StringComparison.Ordinal);
        }

        // The frozen shell predates the extension contract, so its dependency is not part of the freeze.
        text = string.Join('\n', text.Split('\n').Where(line => !line.Contains("\"@cratis/scene.extensions\"", StringComparison.Ordinal)));
        (string Id, string Current, string Baseline)[] externalPins =
        [
            ("@primereact/core", frontend.PrimeReactPackageVersion, "11.2.0"),
            ("@primereact/headless", frontend.PrimeReactPackageVersion, "11.2.0"),
            ("@primereact/hooks", frontend.PrimeReactPackageVersion, "11.2.0"),
            ("@primereact/styles", frontend.PrimeReactPackageVersion, "11.2.0"),
            ("@primereact/types", frontend.PrimeReactPackageVersion, "11.2.0"),
            ("@primereact/ui", frontend.PrimeReactPackageVersion, "11.2.0"),
            ("primereact", frontend.PrimeReactPackageVersion, "11.2.0"),
            ("react", frontend.ReactPackageVersion, "19.3.0"),
            ("react-dom", frontend.ReactDomPackageVersion, "19.3.0"),
            ("@types/node", frontend.TypesNodePackageVersion, "26.6.4"),
            ("@vitejs/plugin-react", frontend.VitePluginReactPackageVersion, "6.1.1")
        ];
        foreach (var pin in externalPins)
        {
            text = text.Replace($"\"{pin.Id}\": \"{pin.Current}\"", $"\"{pin.Id}\": \"{pin.Baseline}\"", StringComparison.Ordinal);
        }

        return text;
    }

    // Cratis/Stage#734 and #741 intentionally changed the scaffolded Yarn files; the previous frozen plan predates both.
    static string WithoutYarnChanges(string text, bool previous) => !previous ? text :
        text.Replace("nodeLinker: node-modules\nnpmPreapprovedPackages:\n  - \"@cratis/*\"", "nodeLinker: node-modules", StringComparison.Ordinal)
            .Replace("node_modules/\n.yarn/\n", "node_modules/\n", StringComparison.Ordinal);

    internal static string Digest(ArtifactRenderPlan plan, IEnumerable<PlannedArtifact>? artifacts = null)
    {
        plan.TargetVersion.ShouldEqual(CratisBackendApplicationScaffoldProfile.Current.CratisPackageVersion);
        return ArtifactRenderPlan.ComputeDigest(
            plan.Target,
            TargetVersion,
            plan.Renderer,
            plan.RendererVersion,
            plan.ApplicationName,
            [.. (artifacts ?? plan.Artifacts).Select(Normalize)],
            plan.Diagnostics);
    }

    internal static PlannedArtifact Normalize(PlannedArtifact artifact) => artifact.Kind == PlannedArtifactKind.Text
        ? PlannedArtifact.CreateText(artifact.RelativePath, Normalize(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())), artifact.Sources)
        : artifact;
}
#endif
