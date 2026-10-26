// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Verifies the resolved implementation envelope without interpreting implementation code.</summary>
internal static class SemanticImplementationAdmission
{
    internal const string PerformerGapCode = "STAGE-IMPL-006";
    internal const string BodyIntegrityCode = "STAGE-ESM-056";

    internal static ImmutableArray<ArtifactRenderDiagnostic> Verify(ArtifactRenderRequest request)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        var requirements = request.ImplementationRequirements;
        var contents = request.ImplementationContents;
        if (requirements.IsDefault || contents is null)
        {
            diagnostics.Add(Error("The implementation attachment envelope is missing.", request.Model.Application.Id));
            return diagnostics.ToImmutable();
        }

        var byId = IndexRequirements(request, diagnostics);

        // Check the whole supplied envelope, even for a narrow render scope or an unused role.
        foreach (var (id, requirement) in byId.Where(pair => pair.Value.Role != SemanticImplementationRole.QueryPerformer))
        {
            TryGetVerifiedBody(request, byId, id, requirement.Source?.SemanticId ?? request.Model.Application.Id, diagnostics, out _);
        }

        foreach (var id in contents.Keys.Where(id => !byId.ContainsKey(id)).Order(StringComparer.Ordinal))
        {
            diagnostics.Add(Error($"Implementation body '{id}' has no compiler requirement.", request.Model.Application.Id));
        }

        // Model references must not depend on which subset of requirements a caller chose to pass.
        foreach (var (id, owner) in ReferencedBodies(request.Model.Application).Where(reference => !byId.ContainsKey(reference.Id))
            .DistinctBy(reference => reference.Id, StringComparer.Ordinal))
        {
            TryGetVerifiedBody(request, byId, id, owner, diagnostics, out _);
        }

        if (diagnostics.Count == 0 && CratisArtifactRenderProfileAdmission.Matches(request, out var options, out _))
        {
            var context = new SemanticApplicationContext(request, options);
            diagnostics.AddRange(PerformerDiagnostics(context));
        }

        return diagnostics.ToImmutable();
    }

    internal static PerformerAdmission AdmitPerformer(SemanticApplicationContext context, SemanticKeyedQuery query)
    {
        var requirements = context.Request.ImplementationRequirements;
        var requirement = requirements.IsDefault ? null : requirements.FirstOrDefault(candidate =>
            candidate is { Role: SemanticImplementationRole.QueryPerformer } && candidate.Source?.SemanticId == query.Id);
        if (requirement is null) return new(null, null, "The performer requirement is missing.");
        if (query.Delivery == SemanticQueryDelivery.Live)
        {
            return new(requirement, null, "An observable performer has no body-owned stream; live performer bodies are not supported.");
        }
        if (!SemanticCratisAdmission.IsCSharpImplementation(requirement))
        {
            return new(requirement, null, ImplementationLanguage.Reason(requirement), true);
        }

        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (!TryGetVerifiedBody(context.Request, requirement.RequirementId, query.Id, diagnostics, out var body) || body is null)
        {
            return new(requirement, null, string.Join(' ', diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        var analysis = SemanticQueryContextRuntime.Analyze(context, query, requirement, body);
        return new(requirement, body, analysis?.Message);
    }

    // This is the only path by which an implementation body can be obtained. Any future admitted
    // implementation role must use it rather than reading ImplementationContents directly.
    internal static bool TryGetVerifiedInline(
        ArtifactRenderRequest request,
        SemanticInlineCodeSite site,
        ImmutableArray<ArtifactRenderDiagnostic>.Builder diagnostics,
        out InlineCode? code)
    {
        var (requirementId, owner, placement) = site;
        code = null;
        if (!TryGetVerifiedBody(request, requirementId, owner, diagnostics, out var body)) return false;
        var requirement = request.ImplementationRequirements.Single(item => item.RequirementId == requirementId);
        if (!string.Equals(placement.Owner, owner.ToString(), StringComparison.Ordinal))
        {
            diagnostics.Add(Error("The inline placement owner differs from the semantic insertion site.", owner));
            return false;
        }

        var language = requirement.Language ?? (requirement.File?.EndsWith(".cs", StringComparison.Ordinal) == true ? "csharp" : string.Empty);

        // This request contract supplies strings only: never claim these are the original disk bytes.
        var verified = InlineCode.VerifyUtf8Text(body!, new(requirement.ContentHash!, language, placement, requirementId));
        code = verified.Value;
        foreach (var diagnostic in verified.Diagnostics) diagnostics.Add(Error(diagnostic.Message, owner));
        return code is not null;
    }

    internal static bool TryGetVerifiedBody(
        ArtifactRenderRequest request,
        string requirementId,
        SemanticId owner,
        ImmutableArray<ArtifactRenderDiagnostic>.Builder diagnostics,
        out string? body)
    {
        var requirements = request.ImplementationRequirements;
        var byId = requirements.IsDefault ? new Dictionary<string, SemanticImplementationRequirement>(StringComparer.Ordinal) :
            requirements.Where(requirement => requirement is not null && !string.IsNullOrEmpty(requirement.RequirementId))
                .GroupBy(requirement => requirement.RequirementId, StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        return TryGetVerifiedBody(request, byId, requirementId, owner, diagnostics, out body);
    }

    static Dictionary<string, SemanticImplementationRequirement> IndexRequirements(ArtifactRenderRequest request, ImmutableArray<ArtifactRenderDiagnostic>.Builder diagnostics)
    {
        var byId = new Dictionary<string, SemanticImplementationRequirement>(StringComparer.Ordinal);
        foreach (var requirement in request.ImplementationRequirements)
        {
            if (requirement is null || string.IsNullOrEmpty(requirement.RequirementId) ||
                !byId.TryAdd(requirement.RequirementId, requirement))
            {
                diagnostics.Add(Error("The implementation attachment envelope contains a missing or duplicate requirement identity.", request.Model.Application.Id));
            }
        }
        return byId;
    }

    static IEnumerable<ArtifactRenderDiagnostic> PerformerDiagnostics(SemanticApplicationContext context)
    {
        foreach (var query in context.Queries.Values.Where(query => context.PerformedQueries.Contains(query.Id)))
        {
            var admission = AdmitPerformer(context, query);
            if (admission.Reason is not { } reason) continue;
            if (admission is { LanguageMismatch: true, Requirement: { } mismatched })
            {
                yield return ImplementationLanguage.Warning(mismatched, query.Id, $"Query '{query.Name}'");
                continue;
            }

            yield return new(
                PerformerGapCode,
                ArtifactRenderDiagnosticSeverity.Warning,
                $"Query '{query.Name}' (requirement {admission.Requirement?.RequirementId}) performer rendered as a gap: {reason}",
                query.Id);
        }
    }

    static bool TryGetVerifiedBody(
        ArtifactRenderRequest request,
        Dictionary<string, SemanticImplementationRequirement> byId,
        string requirementId,
        SemanticId owner,
        ImmutableArray<ArtifactRenderDiagnostic>.Builder diagnostics,
        out string? body)
    {
        body = null;
        if (string.IsNullOrEmpty(requirementId) || !byId.TryGetValue(requirementId, out var requirement) || requirement.Source is null)
        {
            diagnostics.Add(Error($"Model implementation requirement '{requirementId}' has no verified requirement/body.", owner));
            return false;
        }

        if (requirement is { Role: SemanticImplementationRole.CommandHandler, AttachmentResolution: SemanticAttachmentResolution.Pending })
        {
            diagnostics.Add(new(
                SemanticVersionFeatures.PendingCommandHandler,
                ArtifactRenderDiagnosticSeverity.Error,
                $"Pending command handler requirement '{requirementId}' is not implemented by Stage yet; no body can be verified.",
                owner));
            return false;
        }

        if (requirement.AttachmentResolution != SemanticAttachmentResolution.Resolved ||
            request.ImplementationContents is null || !request.ImplementationContents.TryGetValue(requirementId, out var candidate) || candidate is null)
        {
            var reason = request.AttachmentDiagnostics.IsDefault ? null : request.AttachmentDiagnostics.FirstOrDefault(diagnostic =>
                requirement.File is not null && diagnostic.Code.StartsWith("PLAY043", StringComparison.Ordinal) &&
                diagnostic.Message.Contains($"'{requirement.File}'", StringComparison.Ordinal) &&
                diagnostic.Location.Line == requirement.Source.Span.StartLine && diagnostic.Location.Column == requirement.Source.Span.StartColumn);
            diagnostics.Add(Error(
                $"Implementation requirement '{requirementId}' has no resolved body." +
                (reason is null ? string.Empty : $" {reason.Code}: {reason.Message}"),
                owner));
            return false;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(candidate))).ToLowerInvariant();
        if (!string.Equals(hash, requirement.ContentHash, StringComparison.Ordinal))
        {
            diagnostics.Add(new(BodyIntegrityCode, ArtifactRenderDiagnosticSeverity.Error, $"Implementation requirement '{requirementId}' has a body whose SHA-256 differs from ContentHash.", owner));
            return false;
        }

        body = candidate;
        return true;
    }

    static IEnumerable<(string Id, SemanticId Owner)> ReferencedBodies(SemanticApplication application)
    {
        foreach (var concept in application.Concepts)
        {
            if (string.Equals(concept.Generation?.Kind, "implementation", StringComparison.Ordinal))
            {
                yield return (concept.Generation!.RequirementId!, concept.Id);
            }

            foreach (var rule in concept.Validations.Where(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation))
            {
                yield return (rule.RequirementId!, concept.Id);
            }
        }

        // A declared but unreferenced policy cannot affect a rendered operation. Keep the
        // four-argument v3 render path compatible for that case.
        var policies = application.Policies.ToDictionary(policy => policy.Name, StringComparer.Ordinal);
        foreach (var slice in application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices))
        {
            foreach (var authorization in slice.Commands.Select(command => command.Authorization)
                .Concat(slice.Queries.Select(query => query.Authorization)).OfType<SemanticAuthorization>())
            {
                foreach (var name in PolicyReferences(authorization))
                {
                    if (policies.TryGetValue(name, out var policy))
                    {
                        foreach (var id in PolicyBodies(policy.Condition))
                        {
                            yield return (id, application.Id);
                        }
                    }
                }
            }

            foreach (var command in slice.Commands)
            {
                foreach (var code in command.CodeValidations)
                {
                    yield return (code.RequirementId, command.Id);
                }

                foreach (var rule in command.Validations.Where(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation))
                {
                    yield return (rule.RequirementId!, command.Id);
                }
            }

            foreach (var id in slice.Reducers.SelectMany(reducer => reducer.Transitions).Select(transition => transition.RequirementId)
                .Concat(SemanticReactionBodyRendering.References(slice)))
            {
                yield return (id, slice.Id);
            }
        }
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    static IEnumerable<string> PolicyReferences(SemanticAuthorization authorization) => authorization switch
    {
        SemanticPolicyReference reference => [reference.Name],
        SemanticLogicalAuthorization logical => PolicyReferences(logical.Left).Concat(PolicyReferences(logical.Right)),
        _ => []
    };

    static IEnumerable<string> PolicyBodies(SemanticPolicyCondition condition) => condition switch
    {
        SemanticOpaquePolicyCondition opaque => [opaque.RequirementId],
        SemanticLogicalPolicyCondition logical => PolicyBodies(logical.Left).Concat(PolicyBodies(logical.Right)),
        SemanticNotPolicyCondition not => PolicyBodies(not.Operand),
        _ => []
    };

    static ArtifactRenderDiagnostic Error(string message, SemanticId artifact) =>
        new("STAGE-ESM-020", ArtifactRenderDiagnosticSeverity.Error, message, artifact);

    internal sealed record PerformerAdmission(SemanticImplementationRequirement? Requirement, string? Body, string? Reason, bool LanguageMismatch = false);
}
