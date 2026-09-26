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

    [Fact] void should_pin_the_exact_frontend_package_set() => FrontendValues(_frontend).ShouldEqual("22.24.0|22.24.0|22.24.0|7.19.6|7.8.2|4.10.0|0.2.2|19.3.0|19.3.0|7.18.4|8.3.0|7.0.2|6.1.1|19.3.0|19.3.0|4.14.0|4.2.0|11.1.0|8.0.1|3.0.1|26.6.2");
    [Fact] void should_carry_exactly_twenty_one_frontend_packages() => _frontend.Versions().Count().ShouldEqual(21);
    [Fact] void should_default_the_frontend_package_set_when_it_is_not_supplied() => FrontendValues(CratisBackendApplicationScaffoldProfile.Current.FrontendPackageSet).ShouldEqual(FrontendValues(_frontend));
    [Fact] void should_keep_every_frontend_package_out_of_the_backend_scaffold() => _first.Any(input => Text(input).Contains("@cratis/", StringComparison.Ordinal)).ShouldBeFalse();

    static string FrontendValues(CratisFrontendPackageSet frontend) => string.Join('|', frontend.Versions());
}
