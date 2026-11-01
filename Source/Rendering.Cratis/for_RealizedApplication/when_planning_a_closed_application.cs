// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication;

public class when_planning_a_closed_application : given.a_model_with_authentication_and_onboarding
{
    protected override string Onboarding => """
        onboarding
          invite member
            by Librarian
            grant Member
        """;

    JsonNode Ante => JsonNode.Parse(Text(".runtime/ante.onboarding.json"))!["Ante"]!["Onboarding"]!;

    string[] Codes => [.. _plan.Diagnostics.Select(diagnostic => diagnostic.Code)];

    [Fact] void should_close_registration() => Ante["RegistrationEnabled"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_leave_out_the_sign_up_outcome() => (Ante as JsonObject)!.ContainsKey("SignUp").ShouldBeFalse();
    [Fact] void should_allow_only_the_invitation_the_model_declares() => Ante["InvitationTypes"]!.ToJsonString().ShouldEqual("[\"JoinTenant\"]");
    [Fact] void should_leave_out_legal_terms() => (Ante as JsonObject)!.ContainsKey("Legal").ShouldBeFalse();
    [Fact] void should_not_warn_about_an_offering_the_model_does_not_name() => Codes.ShouldNotContain("STAGE-ONBOARDING-001");
    [Fact] void should_send_no_email_the_model_has_no_template_for() => Text("GeneratedOnboarding/OnboardingRules.cs").ShouldContain("Emails { get; } = []");
    [Fact] void should_render_no_email_templates() => Has("Emails/invitation.json").ShouldBeFalse();
}

public class when_planning_an_application_without_onboarding : given.a_model_with_authentication_and_onboarding
{
    protected override string Onboarding => string.Empty;

    [Fact] void should_not_render_onboarding() => Has("GeneratedOnboarding/Provisioning.cs").ShouldBeFalse();
    [Fact] void should_not_render_the_ante_settings() => Has(".runtime/ante.onboarding.json").ShouldBeFalse();
    [Fact] void should_still_render_the_authproxy_providers() => Has(".runtime/authproxy.application.json").ShouldBeTrue();
    [Fact] void should_not_register_onboarding() => Text("Program.cs").ShouldNotContain("GeneratedOnboarding");
    [Fact] void should_not_reference_herald() => Text($"{Application}.csproj").ShouldNotContain("Herald");
    [Fact] void should_keep_the_chronicle_version_of_every_other_render() => Text("Directory.Packages.props").ShouldNotContain("19.114.0");
}
