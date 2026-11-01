// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Realization;

/// <summary>
/// Reports what the authentication and onboarding of a model ask for that a rendered application does not realize.
/// </summary>
/// <remarks>
/// Codes are permanent and never reused. Retired: STAGE-ONBOARDING-007 (the 'signupDeclined' template rendered but not sent), retired
/// when Ante began publishing OrganizationRegistrationDeclined; STAGE-ONBOARDING-001 (the offering is not rendered), retired when
/// the offering began to be rendered against Box Office.
/// </remarks>
internal static class RealizationDiagnostics
{
    internal const string UnsupportedProvider = "STAGE-AUTH-001";
    internal const string PersonaNotMappable = "STAGE-AUTH-002";
    internal const string TenantInvitationUnattended = "STAGE-ONBOARDING-002";
    internal const string SignupWithoutPersona = "STAGE-ONBOARDING-003";
    internal const string LegalDocumentCount = "STAGE-ONBOARDING-004";
    internal const string EmailTemplateMissing = "STAGE-ONBOARDING-005";
    internal const string InvitationWithoutInviters = "STAGE-ONBOARDING-006";
    internal const string OfferingNeedsOperator = "STAGE-ONBOARDING-008";

    internal static IEnumerable<ArtifactRenderDiagnostic> For(ApplicationRealization realization, SemanticId application)
    {
        foreach (var provider in RuntimeConfigurationFiles.UnsupportedProviders(realization))
        {
            yield return Warning(UnsupportedProvider, $"Authentication provider '{provider.Identity}' (kind '{provider.Kind}') is not one AuthProxy can sign people in with, so it is not rendered. Supported kinds: EntraId, GitHub, Google, Apple, OpenId.", application);
        }

        foreach (var persona in realization.Personas.Where(persona => !persona.Mappable))
        {
            yield return Warning(PersonaNotMappable, $"Persona '{persona.Name}' holds a policy that is not a requirement on authentication, roles or claims, so no caller is matched to it: it can neither invite nor be granted.", application);
        }

        if (realization.Onboarding is not { } onboarding)
        {
            yield break;
        }

        foreach (var diagnostic in Onboarding(realization, onboarding, application))
        {
            yield return diagnostic;
        }
    }

    static IEnumerable<ArtifactRenderDiagnostic> Onboarding(ApplicationRealization realization, RealizedOnboarding onboarding, SemanticId application)
    {
        if (onboarding.Offering.Length > 0)
        {
            yield return new(
                OfferingNeedsOperator,
                ArtifactRenderDiagnosticSeverity.Information,
                $"Offering '{onboarding.Offering}' is started for every new tenant through Box Office. Its operator (event store 'Direct' unless the host configures another) must publish an offer named '{onboarding.Offering}' in its catalog; a tenant provisioned before it arrives is not started on it until its outcome is delivered again. The rendered application does not define the BoxOffice.ManageSeats and BoxOffice.ManageCompute authorization policies, so the seat and compute commands of Box Office are not available until it does.",
                application);
        }

        var tenant = onboarding.Invitations.FirstOrDefault(invitation => invitation.Kind == "tenant");
        if (tenant is not null)
        {
            yield return new(
                TenantInvitationUnattended,
                ArtifactRenderDiagnosticSeverity.Information,
                "An invitation to create a tenant has no persona allowed to send it in the model, so no command is rendered for it. The application's operator invites a tenant by appending UserInvitedToCreateTenant to the application's outbox.",
                application);
        }

        var member = onboarding.Invitations.FirstOrDefault(invitation => invitation.Kind == "member");
        if (member?.By.IsDefaultOrEmpty == true)
        {
            yield return Warning(InvitationWithoutInviters, "An invitation to join a tenant names no persona allowed to send it, so no command is rendered for it.", application);
        }

        if (onboarding.Signup == "CreatesTenant" && RealizationArtifacts.SignupPersona(onboarding).Length == 0)
        {
            yield return Warning(SignupWithoutPersona, "A sign-up creates a tenant, but no 'invite tenant as <Persona>' names the persona its first member gets, so the first member is recorded without a persona.", application);
        }

        if (!onboarding.LegalDocuments.IsDefaultOrEmpty && onboarding.LegalDocuments.Length != 2)
        {
            yield return Warning(LegalDocumentCount, $"Ante presents two legal documents (terms first, privacy second); the model lists {onboarding.LegalDocuments.Length}. The paths are written as a list and Ante has to be given them explicitly.", application);
        }

        foreach (var email in onboarding.Emails)
        {
            if (!realization.Texts.ContainsKey(email.Path))
            {
                yield return Warning(EmailTemplateMissing, $"The '{email.Kind}' email template '{email.Path}' could not be read from the model root, so that email is not sent.", application);
            }
        }
    }

    static ArtifactRenderDiagnostic Warning(string code, string message, SemanticId application) =>
        new(code, ArtifactRenderDiagnosticSeverity.Warning, message, application);
}
