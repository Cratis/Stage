// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Rejects authorization that cannot be rendered safely, and admits opaque policy bodies through the pure gate.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    internal static bool IsRoleClaim(string type) => string.Equals(type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase);

    internal static IEnumerable<SemanticPolicy> OpaquePolicies(SemanticAuthorization authorization, IEnumerable<SemanticPolicy> policies) => authorization switch
    {
        SemanticPolicyReference reference => policies.SingleOrDefault(policy => policy.Name == reference.Name) is { } policy && HasOpaque(policy.Condition) ? [policy] : [],
        SemanticLogicalAuthorization logical => OpaquePolicies(logical.Left, policies).Concat(OpaquePolicies(logical.Right, policies)),
        _ => []
    };

    static bool HasOpaque(SemanticPolicyCondition condition) => condition switch
    {
        SemanticOpaquePolicyCondition => true,
        SemanticNotPolicyCondition not => HasOpaque(not.Operand),
        SemanticLogicalPolicyCondition logical => HasOpaque(logical.Left) || HasOpaque(logical.Right),
        _ => false
    };

    static void ValidateCallerFixtures(SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in slice.Specifications.Where(specification =>
            specification.GivenCaller is { Authenticated: false } caller && (!caller.Roles.IsEmpty || !caller.Claims.IsEmpty)))
        {
            diagnostics.Add(Error("STAGE-ESM-011", $"Specification '{specification.Name}' cannot render: An unauthenticated caller cannot carry roles or claims; Arc supplies an empty guest principal.", specification.Id));
        }
    }

    static bool ValidateCommandAuthorization(SemanticApplicationContext context, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        // The reference evaluator selects the first supplied identifier, not necessarily the first declared
        // identifier. A generated command record cannot distinguish omitted values from defaulted values.
        if (command.Authorization is not null && command.Properties.Count(property => property.IsIdentifier) != 1)
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorized command '{command.Name}' needs exactly one identifier to preserve the reference subject.", command.Id));
            return false;
        }

        var subject = command.Properties.SingleOrDefault(property => property.IsIdentifier);
        return ValidateAuthorization(context, command.Authorization, command.Id, command.Name, subject, diagnostics) &&
            ValidateClaimTargets(context, command.Authorization, command.Properties, subject, command.Id, command.Name, diagnostics);
    }

    static bool ValidateQueryAuthorization(SemanticApplicationContext context, SemanticKeyedQuery query, List<ArtifactRenderDiagnostic> diagnostics) =>
        ValidateAuthorization(context, query.Authorization, query.Id, query.Name, new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false), diagnostics) &&
        ValidateClaimTargets(
            context,
            query.Authorization,
            [new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false)],
            new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false),
            query.Id,
            query.Name,
            diagnostics);

    static bool ValidateAuthorization(SemanticApplicationContext context, SemanticAuthorization? authorization, SemanticId id, string name, SemanticProperty? subject, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (authorization is null)
        {
            return true;
        }

        // An opaque predicate composes only as a whole named policy. Screenplay defines no three-valued result for
        // an unsupported operand, so one nested in a condition (including under `not`) is never given a truth value.
        var nested = OpaquePolicies(authorization, context.Application.Policies)
            .FirstOrDefault(policy => policy.Condition is not SemanticOpaquePolicyCondition);
        if (nested is not null)
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{name}' references policy '{nested.Name}', which nests an opaque predicate inside a condition; only a whole csharp/file policy can compose with portable policies.", id));
            return false;
        }

        if (!CanRender(authorization, context.Application.Policies) ||
            Claims(authorization, context.Application.Policies).Any(claim => IsRoleClaim(claim.Claim)))
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{name}' contains a role claim URI, or has an unresolved or non-portable policy.", id));
            return false;
        }

        var admitted = true;
        foreach (var policy in OpaquePolicies(authorization, context.Application.Policies).DistinctBy(policy => policy.Name))
        {
            admitted &= ValidateOpaquePolicy(context, (SemanticOpaquePolicyCondition)policy.Condition, policy.Name, id, name, subject, diagnostics);
        }

        return admitted;
    }

    static bool ValidateOpaquePolicy(
        SemanticApplicationContext context,
        SemanticOpaquePolicyCondition opaque,
        string policy,
        SemanticId operation,
        string name,
        SemanticProperty? subject,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        var verified = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, opaque.RequirementId, operation, verified, out var body))
        {
            diagnostics.AddRange(verified);
            return false;
        }

        var requirement = context.Request.ImplementationRequirements.FirstOrDefault(candidate => candidate?.RequirementId == opaque.RequirementId);
        if (requirement is null || requirement.Role != SemanticImplementationRole.PolicyPredicate ||
            requirement.ContextVersion != 1 || requirement.ResultVersion != 1 || requirement.RequiredCapability != "pure" ||
            !((requirement.Language == "csharp" && requirement.File is null) ||
                (requirement.Language is null && requirement.File?.EndsWith(".cs", StringComparison.Ordinal) == true)))
        {
            diagnostics.Add(Error("STAGE-ESM-019", $"Policy '{policy}' used by '{name}' has an unsupported implementation envelope or language.", operation));
            return false;
        }

        var descriptors = context.Request.TypedContextDescriptors.IsDefault ? [] :
            context.Request.TypedContextDescriptors.Where(descriptor => descriptor?.RequirementId == opaque.RequirementId && descriptor.OperationId == operation).ToArray();
        if (descriptors.Length != 1 || !descriptors[0].IsWrapperReady || !IsPolicyContext(descriptors[0], operation, subject))
        {
            diagnostics.Add(Error("STAGE-ESM-021", $"Policy '{policy}' used by '{name}' has no unique wrapper-ready PolicyContext (Artifact, Subject, Identity, Tenant, Occurred) for this use site.", operation));
            return false;
        }

        PureTransitionAdmission.Verdict verdict;
        try
        {
            verdict = PurePolicyAdmission.Analyze(body!, context, descriptors[0], requirement);
        }
        catch (Exception exception)
        {
            diagnostics.Add(Error("STAGE-ESM-021", $"Policy '{policy}' used by '{name}' could not be analysed: {exception.Message}", operation));
            return false;
        }

        if (!verdict.Accepted)
        {
            diagnostics.Add(Error(verdict.Code!, $"Policy '{policy}' used by '{name}': {verdict.Reason}", operation));
            return false;
        }

        // Screenplay's Subject is text; Stage supplies only the canonical text of a text, Uuid, Date or DateTime key.
        if (verdict.ContextReads.Contains("Subject") &&
            descriptors[0].Members.Single(member => member.Name == "Subject").Source.Kind != SemanticContextSourceKinds.Unavailable &&
            !(subject is { Type.IsCollection: false } && SemanticClaimTargets.Primitive(context, subject) is
                SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime))
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Policy '{policy}' used by '{name}' reads context.Subject, but the subject is neither text, Uuid, Date nor DateTime.", operation));
            return false;
        }

        context.PolicyContextReads[(opaque.RequirementId, operation)] = verdict.ContextReads;
        return true;
    }

    static bool IsPolicyContext(SemanticTypedContextDescriptor descriptor, SemanticId operation, SemanticProperty? subject)
    {
        var members = descriptor.Members;
        if (members.IsDefault || members.Length != 5 || members.Any(member => member?.Source is null || member.Type is null || member.IsDerived))
        {
            return false;
        }

        static bool Runtime(SemanticTypedContextMember member, string memberName, string token) =>
            member.Name == memberName && member.Type.Kind == SemanticContextTypeKinds.Runtime && member.Type.RuntimeToken == token &&
            member.Type.ModelType is null && member.Type.Shape is null && !member.IsNullable;

        var subjectSource = members[1].Source;
        return members[0].Name == "Artifact" && members[0].Source.Kind == SemanticContextSourceKinds.AuthorizedOperation &&
            members[0].Source.SemanticId == operation && members[0].Type.Kind == SemanticContextTypeKinds.Shape && members[0].Type.Shape == operation &&
            Runtime(members[1], "Subject", SemanticContextRuntimeTokens.Text) &&
            (subjectSource.Kind == SemanticContextSourceKinds.Unavailable ||
                ((subjectSource.Kind == SemanticContextSourceKinds.CommandIdentifier || subjectSource.Kind == SemanticContextSourceKinds.QueryKey) && subject is not null && subjectSource.SemanticId == subject.Id)) &&
            Runtime(members[2], "Identity", SemanticContextRuntimeTokens.Identity) &&
            Runtime(members[3], "Tenant", SemanticContextRuntimeTokens.TenantId) &&
            Runtime(members[4], "Occurred", SemanticContextRuntimeTokens.DateTime) &&
            members.Skip(2).All(member => member.Source.Kind == SemanticContextSourceKinds.ContextContract && member.Source.Path == member.Name);
    }

    static bool ValidateClaimTargets(
        SemanticApplicationContext context,
        SemanticAuthorization? authorization,
        IReadOnlyList<SemanticProperty> properties,
        SemanticProperty? subject,
        SemanticId id,
        string name,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (authorization is null)
        {
            return true;
        }

        var paths = Claims(authorization, context.Application.Policies).Where(claim => claim.TargetKind != SemanticClaimTargetKind.Literal)
            .Select(claim => claim.TargetKind == SemanticClaimTargetKind.Subject ? subject?.Name : claim.Value);
        if (paths.All(path => path is not null && IsSupportedClaimPath(context, properties, path)))
        {
            return true;
        }

        diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{name}' compares a claim with an artifact value that is neither text, Uuid, Date, DateTime, nor a supported always-denied scalar.", id));
        return false;
    }

    static IEnumerable<SemanticClaimCondition> Claims(SemanticAuthorization authorization, IEnumerable<SemanticPolicy> policies) => authorization switch
    {
        SemanticPolicyReference reference => Conditions(policies.Single(policy => policy.Name == reference.Name).Condition),
        SemanticLogicalAuthorization logical => Claims(logical.Left, policies).Concat(Claims(logical.Right, policies)),
        _ => []
    };

    static IEnumerable<SemanticClaimCondition> Conditions(SemanticPolicyCondition condition) => condition switch
    {
        SemanticClaimCondition claim => [claim],
        SemanticNotPolicyCondition not => Conditions(not.Operand),
        SemanticLogicalPolicyCondition logical => Conditions(logical.Left).Concat(Conditions(logical.Right)),
        _ => []
    };

    static bool IsSupportedClaimPath(SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, string path)
    {
        var property = SemanticClaimTargets.Property(context, properties, path);

        // Screenplay's SemanticValueValidator admits numbers and booleans only as non-text values.
        // MatchClaim therefore always denies these targets; generated claim terms are the literal false.
        // Date and DateTime, like Uuid, are canonical SemanticTextValue targets.
        var primitive = SemanticClaimTargets.Primitive(context, property);
        return property is { Type.IsCollection: false } && primitive is
            SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime or
            SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean;
    }

    static bool CanRender(SemanticAuthorization authorization, IEnumerable<SemanticPolicy> policies) => authorization switch
    {
        SemanticPolicyReference reference => policies.SingleOrDefault(policy => policy.Name == reference.Name) is { } policy && CanRender(policy.Condition),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And or SemanticLogicalOperator.Or } logical =>
            CanRender(logical.Left, policies) && CanRender(logical.Right, policies),
        _ => false
    };

    static bool CanRender(SemanticPolicyCondition condition) => condition switch
    {
        SemanticAuthenticatedCondition => true,
        SemanticOpaquePolicyCondition => true,
        SemanticRoleCondition { Role: not null } => true,
        SemanticClaimCondition { Claim: not null, TargetKind: SemanticClaimTargetKind.Subject, Value: null } => true,
        SemanticClaimCondition { Claim: not null, TargetKind: SemanticClaimTargetKind.Literal or SemanticClaimTargetKind.Artifact, Value: not null } => true,
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And or SemanticLogicalOperator.Or } logical =>
            CanRender(logical.Left) && CanRender(logical.Right),
        SemanticNotPolicyCondition not => CanRender(not.Operand),
        _ => false
    };
}
