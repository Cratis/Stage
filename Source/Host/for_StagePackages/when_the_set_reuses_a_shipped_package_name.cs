// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StagePackages;

public class when_the_set_reuses_a_shipped_package_name : given.a_packages_folder
{
    StagePackages? _packages;

    void Establish() => File.WriteAllText(
        Path.Combine(Root, StagePackages.SetFileName),
        Document.Replace("Acme.Charts", "Cratis.Components", StringComparison.Ordinal));

    void Because() => _packages = StagePackages.Read(Root);

    [Fact] void should_refuse_the_set() => _packages!.Diagnostics.ShouldNotBeEmpty();
    [Fact] void should_approve_no_package() => _packages!.Set.IsEmpty.ShouldBeTrue();
    [Fact] void should_serve_no_file() => _packages!.CanServe("@acme/scene.charts/dist/index.js").ShouldBeFalse();
}
