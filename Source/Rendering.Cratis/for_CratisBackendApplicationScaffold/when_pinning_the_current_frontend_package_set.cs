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

    // The availability packages exist from Arc 23.3.0 on, so the NuGet release moves to 23.5.0 while the npm Arc packages stay on the
    // 22.184.0 line: every @cratis/showrunner.notes release peers @cratis/arc ^22 and the generated frontends install it. The two
    // move together again when the Showrunner notes packages accept Arc 23.
    [Fact] void should_keep_npm_arc_on_the_line_the_showrunner_notes_peer_accepts() => _frontend.ArcPackageVersion.ShouldEqual("22.184.0");
    [Fact] void should_keep_npm_arc_react_on_the_line_the_showrunner_notes_peer_accepts() => _frontend.ArcReactPackageVersion.ShouldEqual("22.184.0");
    [Fact] void should_keep_npm_arc_vite_on_the_line_the_showrunner_notes_peer_accepts() => _frontend.ArcVitePackageVersion.ShouldEqual("22.184.0");
    [Fact] void should_carry_exactly_twenty_three_frontend_packages() => _frontend.Versions().Count().ShouldEqual(23);
    [Fact] void should_default_the_frontend_package_set_when_it_is_not_supplied() => FrontendValues(CratisBackendApplicationScaffoldProfile.Current.FrontendPackageSet).ShouldEqual(FrontendValues(_frontend));
    [Fact] void should_keep_every_frontend_package_out_of_the_backend_scaffold() => _first.Any(input => Text(input).Contains("@cratis/", StringComparison.Ordinal)).ShouldBeFalse();

    static string FrontendValues(CratisFrontendPackageSet frontend) => string.Join('|', frontend.Versions());
}
