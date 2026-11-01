// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Realization;

/// <summary>
/// Plans the files an application rendered with authentication or onboarding gets besides the ones its slices produce.
/// </summary>
internal static class RealizationArtifacts
{
    internal const string AuthenticationFolder = "GeneratedAuthentication";
    internal const string OnboardingFolder = "GeneratedOnboarding";
    internal const string EmailsFolder = "Emails";
    internal const string DefaultLanguage = "en";

    static readonly Dictionary<string, (string Herald, string Subject, string[] Values, string[] Required)> _emails = new(StringComparer.Ordinal)
    {
        ["invitation"] = (
            "invitation",
            "You are invited to {{application.name}}",
            ["invitation.link", "invitation.expiresAt", "invitation.tenant", "application.name"],
            ["invitation.link"]),
        ["welcome"] = (
            "welcome",
            "Welcome to {{application.name}}",
            ["member.name", "tenant.name", "application.name", "application.link"],
            []),
        ["signupDeclined"] = (
            "signup-declined",
            "Your request to join {{application.name}}",
            ["application.name", "reason"],
            [])
    };

    internal static IReadOnlyList<string> EmailKinds => [.. _emails.Keys];

    internal static IEnumerable<PlannedArtifact> Render(
        ApplicationRealization realization,
        string applicationName,
        string rootNamespace,
        string language,
        ImmutableArray<SemanticId> sources)
    {
        foreach (var (path, content) in RuntimeConfigurationFiles.Render(realization, applicationName))
        {
            yield return PlannedArtifact.CreateText(path, content, sources);
        }

        yield return Source($"{AuthenticationFolder}/Personas.cs", RealizationTemplates.Read("Personas", rootNamespace, ("__Personas__", PersonaEntries(realization))), sources);
        yield return Source($"{AuthenticationFolder}/MemberIdentity.cs", RealizationTemplates.Read("MemberIdentity", rootNamespace), sources);
        yield return Source($"{AuthenticationFolder}/RuntimeConventions.cs", RealizationTemplates.Read("RuntimeConventions", rootNamespace), sources);
        if (realization.Onboarding is not { } onboarding)
        {
            yield break;
        }

        foreach (var artifact in Onboarding(realization, onboarding, applicationName, rootNamespace, language, sources))
        {
            yield return artifact;
        }
    }

    internal static string SignupPersona(RealizedOnboarding onboarding) =>
        onboarding.Invitations.FirstOrDefault(invitation => invitation.Kind == "tenant")?.Grant.FirstOrDefault() ?? string.Empty;

    internal static string WithHeader(string template, string subject)
    {
        var text = template.TrimStart('﻿').Replace("\r\n", "\n", StringComparison.Ordinal);

        return text.StartsWith("---\n", StringComparison.Ordinal) ? text : $"---\nsubject: {subject}\n---\n{text}";
    }

    internal static string Manifest(string[] values, string[] required)
    {
        var builder = new StringBuilder("{\n  \"values\": [\n");
        builder.AppendJoin(",\n", values.Select(value => required.Contains(value, StringComparer.Ordinal)
            ? $"    {{ \"name\": \"{value}\", \"required\": true }}"
            : $"    \"{value}\""));

        return builder.Append("\n  ]\n}\n").ToString();
    }

    static IEnumerable<PlannedArtifact> Onboarding(
        ApplicationRealization realization,
        RealizedOnboarding onboarding,
        string applicationName,
        string rootNamespace,
        string language,
        ImmutableArray<SemanticId> sources)
    {
        var member = onboarding.Invitations.FirstOrDefault(invitation => invitation.Kind == "member");
        var tenant = onboarding.Invitations.FirstOrDefault(invitation => invitation.Kind == "tenant");
        var inviters = member is null ? [] : member.By;
        var emails = onboarding.Emails.Where(email => _emails.ContainsKey(email.Kind) && realization.Texts.ContainsKey(email.Path)).ToArray();
        var invitations = inviters.Length > 0;

        yield return Source(
            $"{OnboardingFolder}/OnboardingRules.cs",
            RealizationTemplates.Read(
                "OnboardingRules",
                rootNamespace,
                ("__Signup__", RealizationTemplates.Literal(onboarding.Signup)),
                ("__SignupPersona__", RealizationTemplates.Literal(SignupPersona(onboarding))),
                ("__TenantPersonas__", RealizationTemplates.List(tenant?.Grant ?? [])),
                ("__MemberGrantable__", RealizationTemplates.List(member?.Grant ?? [])),
                ("__MemberInviters__", RealizationTemplates.List(inviters)),
                ("__Emails__", RealizationTemplates.List(emails.Select(email => email.Kind)))),
            sources);
        yield return Source(
            $"{OnboardingFolder}/OnboardingRegistration.cs",
            RealizationTemplates.Read(
                "OnboardingRegistration",
                rootNamespace,
                ("__Application__", RealizationTemplates.Literal(applicationName)),
                ("__DefaultLanguage__", RealizationTemplates.Literal(language)),
                ("__AuthorizationUsing__", invitations ? "using Cratis.Arc.Authorization;\n" : string.Empty),
                ("__InvitationPolicies__", invitations ? "        builder.Services.AddArcAuthorizationPolicy<InviteMemberPolicy>(OnboardingPolicies.InviteMember);\n" : string.Empty)),
            sources);
        foreach (var template in new[] { "OnboardingEnvironment", "OnboardingEvents", "MemberPersonaClaims", "Provisioning", "OnboardingEmails", "AnteOutcomes" })
        {
            yield return Source($"{OnboardingFolder}/{template}.cs", RealizationTemplates.Read(template, rootNamespace), sources);
        }

        if (invitations)
        {
            yield return Source($"{OnboardingFolder}/OnboardingInvitations.cs", RealizationTemplates.Read("OnboardingInvitations", rootNamespace), sources);
            yield return Source($"{OnboardingFolder}/InvitationPublisher.cs", RealizationTemplates.Read("InvitationPublisher", rootNamespace), sources);
        }

        foreach (var artifact in EmailArtifacts(realization, emails, language, sources))
        {
            yield return artifact;
        }
    }

    // The persona a sign-up gives its first member is the one the model names for an invitation to create a tenant.
    static IEnumerable<PlannedArtifact> EmailArtifacts(ApplicationRealization realization, RealizedEmail[] emails, string language, ImmutableArray<SemanticId> sources)
    {
        foreach (var email in emails)
        {
            var (herald, subject, values, required) = _emails[email.Kind];
            yield return PlannedArtifact.CreateText($"{EmailsFolder}/{herald}.json", Manifest(values, required), sources);
            yield return PlannedArtifact.CreateText($"{EmailsFolder}/{herald}.{language}.html", WithHeader(realization.Texts[email.Path], subject), sources);
        }

        if (emails.Length == 0)
        {
            yield break;
        }

        foreach (var (name, bytes) in realization.Images)
        {
            yield return PlannedArtifact.CreateBinary($"{EmailsFolder}/images/{name}", bytes, sources);
        }
    }

    static string PersonaEntries(ApplicationRealization realization) => string.Join(
        ",\n",
        realization.Personas.Select(persona =>
            $"        new({RealizationTemplates.Literal(persona.Name)}, {RealizationTemplates.List(persona.Roles)}, " +
            $"[{string.Join(", ", persona.Claims.Select(claim => $"({RealizationTemplates.Literal(claim.Type)}, {RealizationTemplates.Literal(claim.Value)})"))}], " +
            $"{(persona.Mappable ? "true" : "false")})"));

    static PlannedArtifact Source(string path, string content, ImmutableArray<SemanticId> sources) => PlannedArtifact.CreateText(path, content, sources);
}
