// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Xml.Linq;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding.for_CratisBackendApplicationScaffoldProfile;

public class when_checking_the_dependency_floors : Specification
{
    XDocument _project = null!;
    void Because()
    {
        var scaffold = CratisRendering.PlanScaffold(new("Shop", "Shop", "Shop"));
        _project = XDocument.Parse(Encoding.UTF8.GetString(scaffold.Artifacts.Single(artifact => artifact.RelativePath == "Shop.csproj").Bytes.AsSpan()));
    }
    [Fact] void should_pin_the_chronicle_client_above_the_required_floor() => (Version.Parse(PackageVersion("Cratis.Chronicle")) >= new Version(19, 30, 0)).ShouldBeTrue();
    [Fact] void should_pin_the_cratis_integration_above_the_arc_chronicle_floor() => (Version.Parse(PackageVersion("Cratis")) >= new Version(22, 49, 1)).ShouldBeTrue();
    [Fact] void should_keep_the_pinned_client_and_kernel_together() => PackageVersion("Cratis.Chronicle").ShouldEqual(CratisBackendApplicationScaffoldProfile.Current.ChronicleImageVersion);
    string PackageVersion(string name) => _project.Descendants("PackageReference").Single(element => (string?)element.Attribute("Include") == name).Attribute("Version")!.Value;
}
