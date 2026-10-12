// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StagePackages;

public class when_the_set_names_a_package_that_is_missing : given.a_packages_folder
{
    StagePackages? _packages;

    void Establish() => Directory.Delete(Path.Combine(Root, "@acme"), recursive: true);
    void Because() => _packages = StagePackages.Read(Root);

    [Fact] void should_report_the_missing_package() => _packages!.Diagnostics.ShouldContainOnly("Package 'Acme.Charts' is imported from '@acme/scene.charts', but the packages folder has no '@acme/scene.charts' folder.");
    [Fact] void should_load_no_module() => _packages!.Modules.ShouldBeEmpty();
}
