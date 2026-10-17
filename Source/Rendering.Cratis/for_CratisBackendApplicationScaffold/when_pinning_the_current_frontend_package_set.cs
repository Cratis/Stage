// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_pinning_the_current_frontend_package_set : a_current_scaffold
{
    CratisFrontendPackageSet _frontend = null!;

    void Because() => _frontend = _profile.FrontendPackageSet;

    [Fact] void should_keep_npm_arc_on_the_emitted_nuget_release() => _frontend.ArcPackageVersion.ShouldEqual(_profile.CratisPackageVersion);
    [Fact] void should_keep_npm_arc_react_on_the_emitted_nuget_release() => _frontend.ArcReactPackageVersion.ShouldEqual(_profile.CratisArcMongoDBPackageVersion);
    [Fact] void should_keep_npm_arc_vite_on_the_emitted_nuget_release() => _frontend.ArcVitePackageVersion.ShouldEqual(_profile.CratisArcChronicleTestingPackageVersion);
    [Fact] void should_carry_exactly_twenty_one_frontend_packages() => _frontend.Versions().Count().ShouldEqual(21);
    [Fact] void should_default_the_frontend_package_set_when_it_is_not_supplied() => FrontendValues(CratisBackendApplicationScaffoldProfile.Current.FrontendPackageSet).ShouldEqual(FrontendValues(_frontend));
    [Fact] void should_keep_every_frontend_package_out_of_the_backend_scaffold() => _first.Any(input => Text(input).Contains("@cratis/", StringComparison.Ordinal)).ShouldBeFalse();

    static string FrontendValues(CratisFrontendPackageSet frontend) => string.Join('|', frontend.Versions());
}
