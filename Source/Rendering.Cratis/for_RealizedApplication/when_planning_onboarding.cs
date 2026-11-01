// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

#pragma warning disable CA1507 // The strings are the keys of the configuration files, which happen to be the names of types.
namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication;

public class when_planning_onboarding : given.a_model_with_authentication_and_onboarding
{
    JsonNode Ante => JsonNode.Parse(Text(".runtime/ante.onboarding.json"))!["Ante"]!["Onboarding"]!;

    string[] Codes => [.. _plan.Diagnostics.Select(diagnostic => diagnostic.Code)];

    [Fact] void should_plan_without_errors() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_configure_ante_for_the_application_store() => Ante["HostStore"]!.GetValue<string>().ShouldEqual(Application);
    [Fact] void should_have_ante_use_the_store_the_application_observes() => Ante["EventStore"]!.GetValue<string>().ShouldEqual("Ante");
    [Fact] void should_enable_the_onboarding_contract() => Ante["Enabled"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_open_registration_because_the_model_opens_sign_up() => Ante["RegistrationEnabled"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_have_sign_up_create_a_tenant() => Ante["SignUp"]!.GetValue<string>().ShouldEqual("CreateTenant");
    [Fact] void should_allow_both_kinds_of_invitation() => Ante["InvitationTypes"]!.ToJsonString().ShouldEqual("[\"CreateTenant\",\"JoinTenant\"]");
    [Fact] void should_let_ante_grant_only_the_personas_the_model_grants() => Ante["Personas"]!.ToJsonString().ShouldEqual("[\"Librarian\",\"Member\"]");
    [Fact] void should_name_the_terms_and_the_privacy_policy() => Ante["Legal"]!.ToJsonString().ShouldEqual("{\"TermsAndConditions\":\"Legal/terms-of-service.md\",\"PrivacyPolicy\":\"Legal/privacy-policy.md\"}");
    [Fact] void should_give_the_first_member_of_a_signed_up_tenant_the_persona_of_a_tenant_invitation() => Text("GeneratedOnboarding/OnboardingRules.cs").ShouldContain("SignupPersona = \"Librarian\"");
    [Fact] void should_grant_an_invitation_only_what_the_model_grants() => Text("GeneratedOnboarding/OnboardingRules.cs").ShouldContain("MemberGrantable { get; } = [\"Member\"]");
    [Fact] void should_let_only_the_inviting_personas_invite() => Text("GeneratedOnboarding/OnboardingRules.cs").ShouldContain("MemberInviters { get; } = [\"Librarian\"]");
    [Fact] void should_render_the_command_that_invites_a_member() => Text("GeneratedOnboarding/OnboardingInvitations.cs").ShouldContain("public record InviteMemberToTenant(string Email, IReadOnlyList<string> Personas)");
    [Fact] void should_reject_a_persona_outside_the_grant() => Text("GeneratedOnboarding/OnboardingInvitations.cs").ShouldContain("OnboardingRules.MemberGrantable.Contains(persona");
    [Fact] void should_record_an_invitation_in_the_tenant_of_the_caller_and_publish_it_to_the_outbox_ante_observes() =>
        Text("GeneratedOnboarding/InvitationPublisher.cs").ShouldContain("EventStoreNamespaceName.Default");
    [Fact] void should_publish_an_invitation_once() => Text("GeneratedOnboarding/InvitationPublisher.cs").ShouldContain("new IdempotencyKey($\"invitation:{context.EventSourceId}\")");
    [Fact] void should_protect_the_address_of_the_invitee_in_the_event_log() => Text("GeneratedOnboarding/OnboardingEvents.cs").ShouldContain("[property: PII] string Email");
    [Fact] void should_observe_the_seven_outcomes_of_ante() => new[] { "InvitationTokenIssued", "InvitationRejected", "InvitationToJoinTenantAccepted", "InvitationToCreateTenantAccepted", "OrganizationRegistrationCompleted", "OrganizationRegistrationDeclined", "LegalTermsAccepted" }
        .Where(outcome => !Text("GeneratedOnboarding/AnteOutcomes.cs").Contains($"({outcome} @event", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_provision_each_outcome_once() => Text("GeneratedOnboarding/Provisioning.cs").ShouldContain("AppendOnce(eventSourceId, @event, new IdempotencyKey(key))");
    [Fact] void should_send_the_invitation_through_herald_with_the_link_on_the_public_origin() => Text("GeneratedOnboarding/OnboardingEmails.cs").ShouldContain("/invite/{issued.Token}");
    [Fact] void should_declare_the_values_of_the_invitation_email() => Text("Emails/invitation.json").ShouldContain("\"name\": \"invitation.link\", \"required\": true");
    [Fact] void should_keep_the_subject_the_model_gives_the_invitation() => Text("Emails/invitation.en.html").StartsWith("---\nsubject: Join {{application.name}}\n---\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_give_a_template_without_a_header_a_subject() => Text("Emails/welcome.en.html").StartsWith("---\nsubject: Welcome to {{application.name}}\n---\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_declined_email_the_way_herald_does() => Has("Emails/signup-declined.json").ShouldBeTrue();
    [Fact] void should_render_the_images_of_the_templates() => _plan.Artifacts.Single(artifact => artifact.RelativePath == "Emails/images/logo.png").Bytes.ToArray().ShouldEqual(Png);
    [Fact] void should_reference_herald_and_the_ante_contracts() => Text($"{Application}.csproj").ShouldContain("Cratis.Showrunner.Herald");
    [Fact] void should_register_onboarding_with_the_application() => Text("Program.cs").ShouldContain($"{Application}.GeneratedOnboarding.Registration.Register(builder);");
    [Fact] void should_say_the_offering_is_not_rendered() => Codes.ShouldContain("STAGE-ONBOARDING-001");
    [Fact] void should_not_reference_box_office() => _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) || artifact.RelativePath.EndsWith(".csproj", StringComparison.Ordinal))
        .Where(artifact => Text(artifact.RelativePath).Contains("BoxOffice", StringComparison.OrdinalIgnoreCase)).ShouldBeEmpty();
    [Fact] void should_say_a_tenant_invitation_has_no_command() => Codes.ShouldContain("STAGE-ONBOARDING-002");
    [Fact] void should_record_a_rejected_invitation_for_an_administrator() => Text("GeneratedOnboarding/AnteOutcomes.cs").ShouldContain("provisioning.InvitationRejected(context.EventSourceId.Value, @event)");
    [Fact] void should_record_a_rejected_invitation_once() => Text("GeneratedOnboarding/Provisioning.cs").ShouldContain("new OnboardingInvitationRejected(rejected.Reason.ToString()), $\"{nameof(InvitationRejected)}:{invitation}\"");
    [Fact] void should_not_email_the_invitee_of_a_rejected_invitation() => Text("GeneratedOnboarding/OnboardingEmails.cs").Contains("InvitationRejected", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_send_the_declined_email_when_ante_publishes_a_declined_sign_up() => Text("GeneratedOnboarding/AnteOutcomes.cs").ShouldContain("emails.SignupDeclined(context.EventSourceId.Value, @event)");
    [Fact] void should_send_the_declined_email_with_the_message_of_the_reviewer() => Text("GeneratedOnboarding/OnboardingEmails.cs").ShouldContain("[\"decline.message\"] = declined.Message.Value");
    [Fact] void should_send_the_declined_email_to_the_address_of_the_sign_up_when_there_is_one() => Text("GeneratedOnboarding/OnboardingEmails.cs").ShouldContain("declined.Email.Value.Length == 0");
    [Fact] void should_send_the_declined_email_through_the_herald_kind_once_per_registration() => Text("GeneratedOnboarding/OnboardingEmails.cs").ShouldContain("Send(\"signup-declined\", registration, declined.Email.Value");
    [Fact] void should_declare_the_values_of_the_declined_email() => Text("Emails/signup-declined.json").ShouldContain("decline.message");
    [Fact] void should_not_emit_the_retired_declined_not_sent_diagnostic() => Codes.ShouldNotContain("STAGE-ONBOARDING-007");
    [Fact] void should_not_turn_on_sign_up_review() => Text(".runtime/ante.onboarding.json").ShouldNotContain("Review");
}
