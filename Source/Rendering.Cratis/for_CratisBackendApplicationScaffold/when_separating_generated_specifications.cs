// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_separating_generated_specifications : a_current_scaffold
{
    XDocument _specs = null!;
    XDocument _web = null!;

    void Because()
    {
        _specs = XDocument.Parse(Content("MyApp.Specs/MyApp.Specs.csproj"));
        _web = XDocument.Parse(Content("MyApp.csproj"));
    }

    [Fact] void should_use_the_plain_sdk_for_specifications() => _specs.Root!.Attribute("Sdk")!.Value.ShouldEqual("Microsoft.NET.Sdk");
    [Fact] void should_reference_the_host() => _specs.Descendants("ProjectReference").Single().Attribute("Include")!.Value.ShouldEqual("../MyApp.csproj");
    [Fact] void should_let_the_specifications_assembly_reach_internal_host_types() => _web.Descendants("InternalsVisibleTo").Single().Attribute("Include")!.Value.ShouldEqual("MyApp.Specs");
    [Fact] void should_link_all_emitted_specification_file_shapes_without_moving_them() => _specs.Descendants("Compile").Single().Attribute("Include")!.Value.ShouldEqual("../**/when_*.cs");
    [Fact] void should_keep_linked_paths_readable() => _specs.Descendants("Compile").Single().Attribute("Link")!.Value.ShouldEqual("%(RecursiveDir)%(Filename)%(Extension)");
    [Fact] void should_not_link_nested_project_or_build_outputs_twice() => _specs.Descendants("Compile").Single().Attribute("Exclude")!.Value.ShouldEqual("../MyApp.Specs/**;../**/bin/**;../**/obj/**;../node_modules/**");
    [Fact] void should_exclude_the_same_specification_glob_from_the_host() => _web.Descendants("Compile").Single().Attribute("Remove")!.Value.ShouldEqual("**/when_*.cs;MyApp.Specs/**");
    [Fact] void should_keep_nested_specs_content_out_of_the_web_host() => _web.Descendants("Content").Single().Attribute("Remove")!.Value.ShouldContain("MyApp.Specs/**");
    [Fact] void should_have_no_testing_dependencies_in_the_host() => _web.Descendants("PackageReference").Select(reference => reference.Attribute("Include")!.Value).ShouldContainOnly("Cratis", "Cratis.Arc.MongoDB", "Cratis.Chronicle", "Cratis.Chronicle.AspNetCore", "Cratis.Screenplay.Matching");
    [Fact] void should_preserve_proxy_generation_in_the_host() => _web.Descendants("CratisProxiesUseSourceFileAsOutputFile").Single().Value.ShouldEqual("true");
    [Fact] void should_not_add_specification_visibility_or_proxy_overrides() => _specs.Descendants("AssemblyAttribute").ShouldBeEmpty();
}
