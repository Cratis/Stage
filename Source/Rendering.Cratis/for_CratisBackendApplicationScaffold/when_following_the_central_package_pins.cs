// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_following_the_central_package_pins : a_current_scaffold
{
    Dictionary<string, string> _central = null!;
    Dictionary<string, string> _emitted = null!;

    void Because()
    {
        _central = Versions(File.ReadAllText(CentralPackagesPath()), _profile.TargetFramework);
        _emitted = Versions(Content("Directory.Packages.props"), _profile.TargetFramework);
    }

    [Fact] void should_exercise_every_emitted_package() => _emitted.Count.ShouldEqual(19);
    [Fact] void should_emit_the_versions_stage_builds_with() => _emitted.Where(pin => pin.Value != _central[pin.Key]).Select(pin => $"{pin.Key}: emitted {pin.Value}, central {_central[pin.Key]}").ShouldBeEmpty();
    [Fact] void should_pin_the_telemetry_abstractions_required_by_chronicle_testing() => _emitted["Microsoft.Extensions.Telemetry.Abstractions"].ShouldEqual("11.0.1");
    [Fact] void should_use_the_same_chronicle_release_for_client_and_image() => _profile.ChronicleImageVersion.ShouldEqual(_central["Cratis.Chronicle"]);

    [Theory]
    [InlineData("3", "10.11.1")]
    [InlineData("4", "11.0.1")]
    void should_use_the_profile_framework_not_the_renderers_framework(string version, string expected)
    {
        var profile = CratisBackendApplicationScaffoldProfile.ForVersion(version)!;
        var pins = Versions(CratisBackendApplicationScaffold.DirectoryPackagesProps(profile), profile.TargetFramework);
        pins["Microsoft.Extensions.Telemetry.Abstractions"].ShouldEqual(expected);
        pins["Microsoft.Extensions.Telemetry.Abstractions"].ShouldEqual(Versions(File.ReadAllText(CentralPackagesPath()), profile.TargetFramework)["Microsoft.Extensions.Telemetry.Abstractions"]);
    }

    static Dictionary<string, string> Versions(string xml, string framework)
    {
        var document = XDocument.Parse(xml);
        var screenplayVersion = document.Descendants("CratisScreenplayVersion").SingleOrDefault()?.Value;
        var telemetryProperty = framework == "net11.0" ? "TelemetryAbstractionsVersionNet11" : "TelemetryAbstractionsVersionNet10";
        var telemetryVersion = document.Descendants(telemetryProperty).SingleOrDefault()?.Value;
        return document.Descendants("PackageVersion").ToDictionary(
            pin => pin.Attribute("Include").Value,
            pin => pin.Attribute("Version").Value.Replace("$(CratisScreenplayVersion)", screenplayVersion ?? string.Empty, StringComparison.Ordinal)
                .Replace("$(TelemetryAbstractionsVersion)", telemetryVersion ?? string.Empty, StringComparison.Ordinal),
            StringComparer.Ordinal);
    }

    static string CentralPackagesPath([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path), "../../..", "Directory.Packages.props"));
}
