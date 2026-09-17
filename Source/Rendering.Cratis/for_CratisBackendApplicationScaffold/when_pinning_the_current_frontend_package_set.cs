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

    [Fact] void should_pin_the_exact_frontend_package_set() => FrontendValues(_frontend).ShouldEqual("22.3.0|22.3.0|22.3.0|7.18.1|7.8.2|4.10.0|0.2.2");
    [Fact] void should_carry_exactly_seven_frontend_packages() => _frontend.Versions().Count().ShouldEqual(7);
    [Fact] void should_default_the_frontend_package_set_when_it_is_not_supplied() => FrontendValues(CratisBackendApplicationScaffoldProfile.Current.FrontendPackageSet).ShouldEqual(FrontendValues(_frontend));
    [Fact] void should_not_emit_any_frontend_package_yet() => _first.Any(input => Text(input).Contains("@cratis/", StringComparison.Ordinal)).ShouldBeFalse();

    static string FrontendValues(CratisFrontendPackageSet frontend) => string.Join('|', frontend.Versions());
}
