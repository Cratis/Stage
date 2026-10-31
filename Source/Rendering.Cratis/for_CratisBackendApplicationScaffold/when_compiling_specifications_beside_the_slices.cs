// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_compiling_specifications_beside_the_slices : a_current_scaffold
{
    XDocument _web = null!;

    void Because() => _web = XDocument.Parse(Content("MyApp.csproj"));

    [Fact] void should_emit_no_specifications_project() => _first.Select(PathOf).Any(path => path.Contains(".Specs", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_expose_internals_to_another_assembly() => _web.Descendants("InternalsVisibleTo").ShouldBeEmpty();
    [Fact] void should_compile_specifications_in_debug() => _web.Descendants("Compile").Single().Parent!.Attribute("Condition")!.Value.ShouldEqual("'$(Configuration)' != 'Debug'");
    [Fact] void should_remove_every_specification_shape_from_release() => _web.Descendants("Compile").Single().Attribute("Remove")!.Value.ShouldEqual("**/when_*.cs;**/when_*/**;**/for_*/**;**/given/**");
    [Fact] void should_have_only_production_dependencies_outside_debug() => _web.Descendants("ItemGroup").Where(group => group.Attribute("Condition") is null).SelectMany(group => group.Elements("PackageReference")).Select(reference => reference.Attribute("Include")!.Value).ShouldContainOnly("Cratis", "Cratis.Arc.MongoDB", "Cratis.Chronicle", "Cratis.Chronicle.AspNetCore", "Cratis.Screenplay.Matching");
    [Fact] void should_reference_the_telemetry_abstractions_required_by_chronicle_testing_only_in_debug() => _web.Descendants("PackageReference").Single(reference => reference.Attribute("Include")!.Value == "Microsoft.Extensions.Telemetry.Abstractions").Parent!.Attribute("Condition")!.Value.ShouldEqual("'$(Configuration)' == 'Debug'");
    [Fact] void should_preserve_proxy_generation() => _web.Descendants("CratisProxiesUseSourceFileAsOutputFile").Single().Value.ShouldEqual("true");
    [Fact] void should_keep_package_json_out_of_the_web_content() => _web.Descendants("Content").Single().Attribute("Remove")!.Value.ShouldEqual("package.json");
    [Fact] void should_list_only_the_application_in_the_solution() => XDocument.Parse(Content("MyApp.slnx")).Root!.Elements("Project").Select(project => project.Attribute("Path")!.Value).ShouldContainOnly("MyApp.csproj");
}
