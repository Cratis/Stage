// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents how one authored-body request resolved: a body to substitute, a degraded placeholder, or a refusal.
/// </summary>
/// <param name="Segment">The segment to hand to the language; <see langword="null"/> when refused.</param>
/// <param name="Gap">The gap that explains a placeholder, or <see langword="null"/> when the body is substituted.</param>
/// <param name="Refusal">The safety refusal, or <see langword="null"/> when resolved.</param>
sealed record DeclarativeBodyResolution(DeclarativeSegment? Segment, DeclarativeOwnedGap? Gap, ArtifactRenderDiagnostic? Refusal);

/// <summary>
/// Resolves authored implementation bodies against the request, verifying each one before it can be substituted.
/// </summary>
/// <param name="request">The render request that supplies requirements and bodies.</param>
/// <param name="language">The bound language that admits bodies and spells placeholders.</param>
/// <param name="session">The plan session whose output budget every authored substitution is charged against.</param>
sealed class DeclarativeBodies(ArtifactRenderRequest request, IDeclarativeLanguage language, TemplatePlanSession session)
{
    internal DeclarativeBodyResolution Resolve(string requirementId, SemanticId? item, string template)
    {
        var requirements = request.ImplementationRequirements.IsDefault ? [] : request.ImplementationRequirements.Where(_ => _ is not null && _.RequirementId == requirementId).ToArray();
        if (requirements.Length != 1)
        {
            return Degraded(requirementId, item, template, requirements.Length == 0 ? "the requirement is unknown" : "the requirement is ambiguous");
        }

        var requirement = requirements[0];
        if (!string.Equals(requirement.Language, language.Pack.Id, StringComparison.OrdinalIgnoreCase))
        {
            return Degraded(requirementId, item, template, $"the requirement language '{requirement.Language}' is not the bound language '{language.Pack.Id}'", requirement);
        }

        if (string.IsNullOrEmpty(requirement.ContentHash) || request.ImplementationContents is null || !request.ImplementationContents.TryGetValue(requirementId, out var body) || body is null)
        {
            return Degraded(requirementId, item, template, "the body is not resolved", requirement);
        }

        var overBudget = session.Charge(Encoding.UTF8.GetByteCount(body));
        if (overBudget is not null)
        {
            return new(null, null, DeclarativeOutputExpansion.Refuse(overBudget.Code, overBudget.Message));
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        if (!string.Equals(hash, requirement.ContentHash, StringComparison.Ordinal))
        {
            return new(null, null, DeclarativeOutputExpansion.Refuse(DeclarativeCodes.BodyIntegrity, $"The body of implementation requirement '{requirementId}' has a SHA-256 that differs from its ContentHash."));
        }

        var admission = language.Admit(requirementId, body);
        return admission.Outcome switch
        {
            AuthoredAdmissionOutcome.Admitted => new(new DeclarativeAuthoredSegment(requirementId, body), null, null),
            AuthoredAdmissionOutcome.Unsupported => Degraded(requirementId, item, template, $"{admission.Code}: {admission.Message}", requirement),
            _ => new(null, null, DeclarativeOutputExpansion.Refuse(admission.Code, admission.Message))
        };
    }

    DeclarativeBodyResolution Degraded(string requirementId, SemanticId? item, string template, string reason, SemanticImplementationRequirement? requirement = null)
    {
        var intent = $"Implementation requirement '{requirementId}' cannot be substituted: {reason}.";
        var gap = new TemplateGap(ArtifactRenderDegradationCodes.InlineMismatch, GapRole.ThrowingOperationBody, intent, item?.ToString() ?? requirement?.Source?.SemanticId.ToString() ?? string.Empty, template, 0);
        var owner = item ?? requirement?.Source?.SemanticId;
        var placeholder = language.Placeholder(gap.Role, gap.Code, intent);
        return new(new DeclarativeTextSegment(placeholder), new(owner, gap, DeclarativeRendering.Placeholder(gap.Role)), null);
    }
}
