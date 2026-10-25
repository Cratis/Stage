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
        var frontend = profile.FrontendPackageSet;
        if (path.EndsWith("Directory.Packages.props", StringComparison.Ordinal))
        {
            (string Id, string Current, string Baseline)[] pins =
            [
                ("Cratis", profile.CratisPackageVersion, previous ? "22.87.0" : TargetVersion),
                ("Cratis.Fundamentals", profile.CratisFundamentalsPackageVersion, "7.25.1"),
                ("Cratis.Arc.MongoDB", profile.CratisArcMongoDBPackageVersion, previous ? "22.87.0" : TargetVersion),
                ("Cratis.Arc.Chronicle.Testing", profile.CratisArcChronicleTestingPackageVersion, previous ? "22.87.0" : TargetVersion),
                ("Cratis.Chronicle", profile.CratisChroniclePackageVersion, previous ? "19.55.2" : "19.71.0"),
                ("Cratis.Chronicle.AspNetCore", profile.CratisChroniclePackageVersion, previous ? "19.55.2" : "19.71.0"),
                ("Cratis.Chronicle.Testing", profile.CratisChroniclePackageVersion, previous ? "19.55.2" : "19.71.0"),
                ("Cratis.Specifications", profile.CratisSpecificationsPackageVersion, "5.0.0"),
                ("Cratis.Specifications.XUnit.v3", profile.CratisSpecificationsXUnitV3PackageVersion, "5.0.0"),
                ("NSubstitute", profile.NSubstitutePackageVersion, "6.2.0"),
                ("xunit.v3", profile.XunitV3PackageVersion, "3.2.2")
            ];
            foreach (var pin in pins)
            {
                text = text.Replace($"Include=\"{pin.Id}\" Version=\"{pin.Current}\"", $"Include=\"{pin.Id}\" Version=\"{pin.Baseline}\"", StringComparison.Ordinal);
            }
        }

        if (path.EndsWith("package.json", StringComparison.Ordinal))
        {
            (string Id, string Current, string Baseline)[] pins =
            [
                ("arc", frontend.ArcPackageVersion, "22.58.2"),
                ("arc.react", frontend.ArcReactPackageVersion, "22.58.2"),
                ("arc.vite", frontend.ArcVitePackageVersion, "22.58.2"),
                ("fundamentals", frontend.FundamentalsPackageVersion, "7.22.8"),
                ("components", frontend.ComponentsPackageVersion, "4.26.2"),
                ("scene.blueprint.default", frontend.ScenePackageVersion, previous ? "4.22.1" : "4.67.0"),
                ("scene.components", frontend.ScenePackageVersion, previous ? "4.22.1" : "4.67.0"),
                ("scene.engine", frontend.ScenePackageVersion, previous ? "4.22.1" : "4.67.0"),
                ("scene.model", frontend.ScenePackageVersion, previous ? "4.22.1" : "4.67.0"),
                ("scene.primereact", frontend.ScenePackageVersion, previous ? "4.22.1" : "4.67.0"),
                ("scene.react", frontend.ScenePackageVersion, previous ? "4.22.1" : "4.67.0"),
                ("stage.runtime", ScaffoldPackageVersions.StageRuntime, "1.0.0")
            ];
            foreach (var pin in pins)
            {
                text = text.Replace($"\"@cratis/{pin.Id}\": \"{pin.Current}\"", $"\"@cratis/{pin.Id}\": \"{pin.Baseline}\"", StringComparison.Ordinal);
            }
        }

        return text.Replace($"cratis/chronicle:{profile.ChronicleImageVersion}-development", $"cratis/chronicle:{(previous ? "19.55.2" : "19.71.0")}-development", StringComparison.Ordinal);
    }

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
