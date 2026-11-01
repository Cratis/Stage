// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication;

public class when_planning_an_application_without_onboarding : given.a_model_with_authentication_and_onboarding
{
    protected override string Onboarding => string.Empty;

    [Fact] void should_not_render_onboarding() => Has("GeneratedOnboarding/Provisioning.cs").ShouldBeFalse();
    [Fact] void should_not_render_the_ante_settings() => Has(".runtime/ante.onboarding.json").ShouldBeFalse();
    [Fact] void should_still_render_the_authproxy_providers() => Has(".runtime/authproxy.application.json").ShouldBeTrue();
    [Fact] void should_not_register_onboarding() => Text("Program.cs").ShouldNotContain("GeneratedOnboarding");
    [Fact] void should_not_reference_herald() => Text($"{Application}.csproj").ShouldNotContain("Herald");
    [Fact] void should_keep_the_chronicle_version_of_every_other_render() => Text("Directory.Packages.props").ShouldNotContain("19.115.2");
}
