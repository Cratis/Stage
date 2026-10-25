// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_following_the_central_package_pins : a_current_scaffold
{
    Dictionary<string, string> _central = null!;
    Dictionary<string, string> _emitted = null!;

    void Because()
    {
        _central = Versions(File.ReadAllText(CentralPackagesPath()));
        _emitted = Versions(Content("Directory.Packages.props"));
    }

    [Fact] void should_exercise_every_emitted_package() => _emitted.Count.ShouldEqual(13);
    [Fact] void should_emit_the_centrally_pinned_scaffold_versions() => _emitted.Where(pin => pin.Key != "Cratis.Specifications" && pin.Value != _central[pin.Key]).Select(pin => $"{pin.Key}: emitted {pin.Value}, central {_central[pin.Key]}").ShouldBeEmpty();
    [Fact] void should_keep_generated_specifications_on_the_v2_scaffold_release() => _emitted["Cratis.Specifications"].ShouldEqual(_central["Cratis.Specifications.XUnit"]);
    [Fact] void should_use_the_same_chronicle_release_for_client_and_image() => _profile.ChronicleImageVersion.ShouldEqual(_central["Cratis.Chronicle"]);

    static Dictionary<string, string> Versions(string xml)
    {
        var document = XDocument.Parse(xml);
        var screenplayVersion = document.Descendants("CratisScreenplayVersion").SingleOrDefault()?.Value;
        return document.Descendants("PackageVersion").ToDictionary(
            pin => pin.Attribute("Include")!.Value,
            pin => pin.Attribute("Version")!.Value.Replace("$(CratisScreenplayVersion)", screenplayVersion ?? string.Empty, StringComparison.Ordinal),
            StringComparer.Ordinal);
    }

    static string CentralPackagesPath([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "../../..", "Directory.Packages.props"));
}
