// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRepositoryScaffold;

public class when_following_the_central_package_pins : Specification
{
    Dictionary<string, string> _central = null!;
    Dictionary<string, string> _emitted = null!;
    string _project = null!;

    void Because()
    {
        var result = CratisRendering.PlanRepository(new("Acme", ["Orders"]));
        _central = Versions(File.ReadAllText(CentralPackagesPath()));
        _emitted = Versions(Text(result, "Directory.Packages.props"));
        _project = Text(result, "Source/Composition/Composition.csproj");
    }

    [Fact] void should_emit_the_aspire_version_stage_pins() => _emitted["Aspire.Hosting.AppHost"].ShouldEqual(_central["Aspire.Hosting.AppHost"]);
    [Fact] void should_emit_the_application_host_sdk_at_the_same_version() => _project.Contains($"<Sdk Name=\"Aspire.AppHost.Sdk\" Version=\"{_central["Aspire.Hosting.AppHost"]}\" />", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_chronicle_release_for_the_aspire_integration() => _emitted["Cratis.Chronicle.Aspire"].ShouldEqual(_central["Cratis.Chronicle"]);

    static string Text(CratisPlanResult result, string path) => Encoding.UTF8.GetString(result.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    static Dictionary<string, string> Versions(string xml) =>
        XDocument.Parse(xml).Descendants("PackageVersion").ToDictionary(pin => pin.Attribute("Include")!.Value, pin => pin.Attribute("Version")!.Value, StringComparer.Ordinal);

    static string CentralPackagesPath([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "../../..", "Directory.Packages.props"));
}
