// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StagePackages;

public class when_reading_a_folder : given.a_packages_folder
{
    StagePackages? _packages;

    void Because() => _packages = StagePackages.Read(Root);

    [Fact] void should_read_the_approved_set() => _packages!.Set.Packages.Count.ShouldEqual(1);
    [Fact] void should_report_no_problem() => _packages!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_resolve_the_entry_from_the_package_exports() => _packages!.Modules["@acme/scene.charts"].ShouldEqual("stage/packages/files/@acme/scene.charts/dist/index.js");
    [Fact] void should_serve_the_package_style_sheet() => _packages!.Styles.ShouldContainOnly("stage/packages/files/@acme/scene.charts/styles.css");
    [Fact] void should_serve_the_entry_module() => _packages!.CanServe("@acme/scene.charts/dist/index.js").ShouldBeTrue();
    [Fact] void should_not_serve_a_file_type_a_package_does_not_ship() => _packages!.CanServe("@acme/scene.charts/secret.txt").ShouldBeFalse();
    [Fact] void should_not_serve_a_package_the_set_does_not_approve() => _packages!.CanServe("@other/package/index.js").ShouldBeFalse();
    [Fact] void should_not_serve_the_set_document_itself() => _packages!.CanServe(StagePackages.SetFileName).ShouldBeFalse();
    [Fact] void should_not_serve_a_path_that_leaves_the_package() => _packages!.CanServe("@acme/scene.charts/../../@other/package/index.js").ShouldBeFalse();
}
