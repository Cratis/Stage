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

    // The npm Arc packages move with the Stage frontend to the Arc 24 line, which the Showrunner notes 2.28 and Scene 6.2 peers accept.
    [Fact] void should_pin_npm_arc_to_the_arc_24_line() => _frontend.ArcPackageVersion.ShouldEqual("24.8.1");
    [Fact] void should_pin_npm_arc_react_to_the_arc_24_line() => _frontend.ArcReactPackageVersion.ShouldEqual("24.8.1");
    [Fact] void should_pin_npm_arc_vite_to_the_arc_24_line() => _frontend.ArcVitePackageVersion.ShouldEqual("24.8.1");
    [Fact] void should_carry_exactly_twenty_three_frontend_packages() => _frontend.Versions().Count().ShouldEqual(23);
    [Fact] void should_default_the_frontend_package_set_when_it_is_not_supplied() => FrontendValues(CratisBackendApplicationScaffoldProfile.Current.FrontendPackageSet).ShouldEqual(FrontendValues(_frontend));
    [Fact] void should_keep_every_frontend_package_out_of_the_backend_scaffold() => _first.Any(input => Text(input).Contains("@cratis/", StringComparison.Ordinal)).ShouldBeFalse();

    static string FrontendValues(CratisFrontendPackageSet frontend) => string.Join('|', frontend.Versions());
}
