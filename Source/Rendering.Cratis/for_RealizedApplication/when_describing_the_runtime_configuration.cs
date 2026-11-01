// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Realization;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication;

public class when_describing_the_runtime_configuration : Specification
{
    RealizedOnboarding _onboarding = null!;
    JsonNode _ante = null!;

    void Because()
    {
        _onboarding = new(
            "JoinsTenant",
            [new("member", ["Owner"], ["Member"])],
            ["Legal/a.md", "Legal/b.md", "Legal/c.md"],
            string.Empty,
            [],
            string.Empty);
        _ante = JsonNode.Parse(RuntimeConfigurationFiles.Ante(_onboarding, "Shop"))!["Ante"]!["Onboarding"]!;
    }

    [Fact] void should_map_a_sign_up_that_joins_a_tenant_to_join_tenant() => _ante["SignUp"]!.GetValue<string>().ShouldEqual("JoinTenant");
    [Fact] void should_list_documents_when_the_model_does_not_name_terms_and_privacy() => _ante["Legal"]!["Documents"]!.ToJsonString().ShouldEqual("[\"Legal/a.md\",\"Legal/b.md\",\"Legal/c.md\"]");
    [Fact] void should_not_invent_terms_and_privacy_paths() => (_ante["Legal"] as JsonObject)!.ContainsKey("TermsAndConditions").ShouldBeFalse();
    [Fact] void should_name_the_host_store_after_the_application() => _ante["HostStore"]!.GetValue<string>().ShouldEqual("Shop");
    [Fact] void should_report_a_number_of_legal_documents_ante_does_not_present() =>
        RealizationDiagnostics.For(new(ImmutableArray<RealizedProvider>.Empty, [], _onboarding, []), default).Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ONBOARDING-004");
    [Fact] void should_report_an_invitation_nobody_may_send() =>
        RealizationDiagnostics.For(new(ImmutableArray<RealizedProvider>.Empty, [], _onboarding with { Invitations = [new("member", [], ["Member"])] }, []), default).Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ONBOARDING-006");
    [Fact] void should_report_a_provider_authproxy_has_no_kind_for() =>
        RealizationDiagnostics.For(new([new("Okta", "Okta")], [], null, []), default).Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-AUTH-001");
    [Fact] void should_report_a_persona_it_cannot_reduce_to_roles() =>
        RealizationDiagnostics.For(new([new("GitHub", "GitHub")], [new("Ghost", ["IsGhost"], [], [], false)], null, []), default).Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-AUTH-002");
    [Fact] void should_report_a_missing_email_template() =>
        RealizationDiagnostics.For(new([new("GitHub", "GitHub")], [], _onboarding with { Emails = [new("welcome", "Emails/welcome.html")] }, []), default).Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ONBOARDING-005");
}
