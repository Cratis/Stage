// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static readonly string[] _ruleMemberNames = ["Artifact", "Value", "Property", "Tenant", "CausedBy", "Occurred", "IsWholeArtifact"];

    static void ValidateRuleBody(SemanticApplicationContext context, SemanticValidationRule rule, SemanticId owner, SemanticProperty? property, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var role = rule.Kind switch
        {
            SemanticValidationRuleKind.RulePredicate => SemanticImplementationRole.RulePredicate,
            _ when context.Concepts.ContainsKey(owner) => SemanticImplementationRole.ConceptValidation,
            _ => SemanticImplementationRole.CommandValidation
        };
        ValidateValidationBody(context, rule.RequirementId!, role, owner, property, diagnostics, rule.Name);
        if (!SemanticValidationRendering.CanResolve(rule.Message, context) || !Enum.IsDefined(rule.Severity))
        {
            var reason = context.Strings is null && rule.Message?.StartsWith("$strings.", StringComparison.Ordinal) == true
                ? $"Validation '{rule.Name}' {MessageRenderingReason(rule.Message)}"
                : $"Validation '{rule.Name}' has an unsupported message or severity.";
            diagnostics.Add(Error("STAGE-ESM-019", reason, owner));
        }
    }

    static void ValidateValidationBody(SemanticApplicationContext context, string requirementId, SemanticImplementationRole role, SemanticId owner, SemanticProperty? property, List<ArtifactRenderDiagnostic> diagnostics, string? name = null)
    {
        var verified = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, requirementId, owner, verified, out var body))
        {
            diagnostics.AddRange(verified);
            return;
        }
        var requirement = context.Request.ImplementationRequirements.SingleOrDefault(requirement => requirement.RequirementId == requirementId);
        if (requirement is null || requirement.Role != role || requirement.ContextVersion != 1 || requirement.ResultVersion != 1 ||
            requirement.RequiredCapability != "pure" || !((requirement.Language == "csharp" && requirement.File is null) ||
                (requirement.Language is null && requirement.File?.EndsWith(".cs", StringComparison.Ordinal) == true)))
        {
            diagnostics.Add(Error("STAGE-ESM-019", $"Validation '{name ?? requirementId}' has an unsupported envelope or language.", owner));
            return;
        }
        var descriptors = context.Request.TypedContextDescriptors.Where(descriptor => descriptor.RequirementId == requirementId && descriptor.OperationId == owner).ToArray();
        if (descriptors.Length != 1 || !descriptors[0].IsWrapperReady || !MatchesRuleMembers(context, descriptors[0], owner, property))
        {
            diagnostics.Add(Error("STAGE-ESM-021", $"Validation '{name ?? requirementId}' has no unique wrapper-ready typed context with matching members and sources.", owner));
            return;
        }
        var verdict = PureValidationAdmission.Analyze(body!, context, descriptors[0], requirement);
        if (!verdict.Accepted) diagnostics.Add(Error(verdict.Code!, verdict.Reason!, owner));
    }

    static bool MatchesRuleMembers(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, SemanticId owner, SemanticProperty? property)
    {
        var members = descriptor.Members;
        if (members.Length != 7 || !members.Select(member => member.Name).SequenceEqual(_ruleMemberNames)) return false;
        var concept = context.Concepts.ContainsKey(owner);
        for (var index = 0; index < 2; index++)
        {
            var member = members[index];
            if (member.IsDerived || member.IsNullable != (index == 1 && property?.Type.IsOptional == true) || member.Source?.EventRevision is not null || member.Source?.ConstantValue is not null) return false;
            if (concept)
            {
                if (member.Source?.Kind != SemanticContextSourceKinds.ConceptValue || member.Source.SemanticId != owner || member.Source.Path != "value" || !Model(member.Type, SemanticTypeReference.ForConcept(owner))) return false;
            }
            else if (index == 0 || property is null)
            {
                var properties = context.Commands[owner].Properties.Where(property => !property.IsGenerated).ToArray();
                if (member.Source?.Kind != SemanticContextSourceKinds.Command || member.Source.SemanticId != owner || member.Source.Path != (index == 0 ? "Artifact" : "Value") ||
                    member.Type.Kind != SemanticContextTypeKinds.Shape || member.Type.Shape != owner || member.Type.ModelType is not null || member.Type.RuntimeToken is not null ||
                    member.Type.Properties.Length != properties.Length || !member.Type.Properties.Zip(properties).All(pair => pair.First.Id == pair.Second.Id && pair.First.Name == pair.Second.Name && pair.First.Type == pair.Second.Type))
                {
                    return false;
                }
            }
            else if (member.Source?.Kind != SemanticContextSourceKinds.ModelProperty || member.Source.SemanticId != property.Id || member.Source.Path != property.Name || !Model(member.Type, property.Type))
            {
                return false;
            }
        }
        var path = property?.Name ?? string.Empty;
        if (!Runtime(members[2], SemanticContextRuntimeTokens.Text, false, property is null ? SemanticContextSourceKinds.ValidatedArtifact : SemanticContextSourceKinds.ValidatedProperty, property?.Id ?? owner, string.Empty, path)) return false;
        if (!Runtime(members[3], SemanticContextRuntimeTokens.TenantId, false, SemanticContextSourceKinds.ContextContract, null, "Tenant", null) ||
            !Runtime(members[4], SemanticContextRuntimeTokens.CausedBy, false, SemanticContextSourceKinds.ContextContract, null, "CausedBy", null) ||
            !Runtime(members[5], SemanticContextRuntimeTokens.DateTime, false, SemanticContextSourceKinds.ContextContract, null, "Occurred", null))
        {
            return false;
        }

        return Runtime(members[6], SemanticContextRuntimeTokens.Boolean, true, SemanticContextSourceKinds.Derived, null, "Property", null);
    }

    static bool Model(SemanticContextType type, SemanticTypeReference expected) =>
        type.Kind == SemanticContextTypeKinds.Model && type.ModelType == expected && type.Shape is null && type.RuntimeToken is null && type.Properties.IsEmpty;

    static bool Runtime(SemanticTypedContextMember member, string token, bool derived, string kind, SemanticId? id, string path, string? constant) =>
        !member.IsNullable && member.IsDerived == derived && member.Type.Kind == SemanticContextTypeKinds.Runtime && member.Type.RuntimeToken == token &&
        member.Type.ModelType is null && member.Type.Shape is null && member.Type.Properties.IsEmpty && member.Source?.Kind == kind && member.Source.SemanticId == id &&
        member.Source.Path == path && member.Source.EventRevision is null && member.Source.ConstantValue == constant;
}
