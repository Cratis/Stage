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
        foreach (var specification in slice.Specifications.Where(CarriesCredentialsWithoutAuthentication))
        {
            diagnostics.Add(Error("STAGE-ESM-011", $"Specification '{specification.Name}' cannot render: An unauthenticated caller cannot carry roles or claims; Arc supplies an empty guest principal.", specification.Id));
        }
    }

    static bool CarriesCredentialsWithoutAuthentication(SemanticSpecification specification) =>
        specification.GivenCaller is { Authenticated: false } caller && (!caller.Roles.IsEmpty || !caller.Claims.IsEmpty);

    static bool ValidateCommandAuthorization(SemanticApplicationContext context, SemanticCommand command, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (command.Authorization is null)
        {
            return true;
        }

        if (SemanticAnonymousAccess.IsAnonymous(command.Authorization, context.Application.Policies))
        {
            diagnostics.Add(AnonymousDiagnostic("Command", command.Name, command.Id));
            return true;
        }

        if (SemanticAnonymousAccess.NestsAnonymous(command.Authorization, context.Application.Policies))
        {
            diagnostics.Add(NestedAnonymousDiagnostic(command.Name, command.Id));
            return false;
        }

        // The reference evaluator selects the first supplied identifier, not necessarily the first declared
        // identifier. A generated command record cannot distinguish omitted values from defaulted values.
        var identifiers = command.Properties.Where(property => property.IsIdentifier).ToArray();
        if (identifiers.Length != 1)
        {
            var declaration = identifiers.Length == 0 ? "no identifier" : $"{identifiers.Length} identifiers ({string.Join(", ", identifiers.Select(property => property.Name))})";
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorized command '{command.Name}' declares {declaration}; it needs exactly one identifier to preserve the reference subject.", command.Id));
            return false;
        }

        var subject = command.Properties.SingleOrDefault(property => property.IsIdentifier);
        var site = new AuthorizationSite(command.Id, command.Name, subject, command.Properties);
        return ValidateAuthorization(context, command.Authorization, site, diagnostics) &&
            ValidateClaimTargets(context, command.Authorization, site, diagnostics);
    }

    static bool ValidateQueryAuthorization(SemanticApplicationContext context, SemanticKeyedQuery query, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (SemanticAnonymousAccess.IsAnonymous(query.Authorization, context.Application.Policies))
        {
            diagnostics.Add(AnonymousDiagnostic("Query", query.Name, query.Id));
            return true;
        }

        var subject = query.Argument is null ? null : new SemanticProperty(query.Argument.Id, query.Argument.Name, query.Argument.Type, false);
        var site = new AuthorizationSite(query.Id, query.Name, subject, subject is null ? [] : [subject]);
        if (!ValidateAuthorization(context, query.Authorization, site, diagnostics))
        {
            return false;
        }

        if (subject is null && ReferencesSubject(context, query))
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{query.Name}' references an argument or subject, but the query has no argument.", query.Id));
            return false;
        }

        return ValidateClaimTargets(context, query.Authorization, site, diagnostics);
    }

    static bool ReferencesSubject(SemanticApplicationContext context, SemanticKeyedQuery query) =>
        query.Authorization is { } authorization && ReferencesSubject(context, authorization, query.Id);

    static bool ReferencesSubject(SemanticApplicationContext context, SemanticAuthorization authorization, SemanticId operation) =>
        Claims(authorization, context.Application.Policies).Any(claim => claim.TargetKind != SemanticClaimTargetKind.Literal) ||
        OpaquePolicies(authorization, context.Application.Policies).Any(policy =>
            context.PolicyContextReads[(((SemanticOpaquePolicyCondition)policy.Condition).RequirementId, operation)].Contains("Subject"));

    static bool ValidateAuthorization(SemanticApplicationContext context, SemanticAuthorization? authorization, AuthorizationSite site, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (authorization is null)
        {
            return true;
        }

        if (SemanticAnonymousAccess.NestsAnonymous(authorization, context.Application.Policies))
        {
            diagnostics.Add(NestedAnonymousDiagnostic(site.Name, site.Id));
            return false;
        }

        // An opaque predicate composes only as a whole named policy. Screenplay defines no three-valued result for
        // an unsupported operand, so one nested in a condition (including under `not`) is never given a truth value.
        var nested = OpaquePolicies(authorization, context.Application.Policies)
            .FirstOrDefault(policy => policy.Condition is not SemanticOpaquePolicyCondition);
        if (nested is not null)
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{site.Name}' references policy '{nested.Name}', which nests an opaque predicate inside a condition; only a whole csharp/file policy can compose with portable policies.", site.Id));
            return false;
        }

        if (!CanRender(authorization, context.Application.Policies) ||
            Claims(authorization, context.Application.Policies).Any(claim => IsRoleClaim(claim.Claim)))
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{site.Name}' contains a role claim URI, or has an unresolved or non-portable policy.", site.Id));
            return false;
        }

        var admitted = true;
        foreach (var policy in OpaquePolicies(authorization, context.Application.Policies).DistinctBy(policy => policy.Name))
        {
            admitted &= ValidateOpaquePolicy(context, policy, site, diagnostics);
        }

        return admitted;
    }

    static bool ValidateOpaquePolicy(SemanticApplicationContext context, SemanticPolicy policy, AuthorizationSite site, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var opaque = (SemanticOpaquePolicyCondition)policy.Condition;
        if (BindPolicy(context, policy, site, diagnostics) is not { } binding)
        {
            return false;
        }

        PureTransitionAdmission.Verdict verdict;
        try
        {
            verdict = PurePolicyAdmission.Analyze(binding.Body, context, binding.Descriptor, binding.Requirement);
        }
        catch (Exception exception)
        {
            diagnostics.Add(Error("STAGE-ESM-021", $"Policy '{policy.Name}' used by '{site.Name}' could not be analysed: {exception.Message}", site.Id));
            return false;
        }

        if (!verdict.Accepted)
        {
            diagnostics.Add(Error(verdict.Code!, $"Policy '{policy.Name}' used by '{site.Name}': {verdict.Reason}", site.Id));
            return false;
        }

        if (ReadsUnsupportedSubject(context, verdict, binding.Descriptor, site.Subject))
        {
            diagnostics.Add(Error("STAGE-ESM-015", $"Policy '{policy.Name}' used by '{site.Name}' reads context.Subject, but the subject is neither text, Uuid, Date nor DateTime.", site.Id));
            return false;
        }

        context.PolicyContextReads[(opaque.RequirementId, site.Id)] = verdict.ContextReads;
        return true;
    }

    static PolicyBinding? BindPolicy(SemanticApplicationContext context, SemanticPolicy policy, AuthorizationSite site, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var opaque = (SemanticOpaquePolicyCondition)policy.Condition;
        var verified = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, opaque.RequirementId, site.Id, verified, out var body))
        {
            diagnostics.AddRange(verified);
            return null;
        }

        var requirement = context.Request.ImplementationRequirements.FirstOrDefault(candidate => candidate?.RequirementId == opaque.RequirementId);
        if (requirement is null || !IsPurePolicyPredicate(requirement))
        {
            diagnostics.Add(Error("STAGE-ESM-019", $"Policy '{policy.Name}' used by '{site.Name}' has an unsupported implementation envelope or language.", site.Id));
            return null;
        }

        if (SemanticImplementationBodySelector.LanguageDiagnostic(requirement, site.Id, $"Policy predicate for '{policy.Name}'") is { } language)
        {
            diagnostics.Add(language);
            return null;
        }

        var descriptors = ContextDescriptors(context, opaque.RequirementId, site.Id);
        if (!IsWrapperReadyPolicyContext(descriptors, site))
        {
            diagnostics.Add(Error("STAGE-ESM-021", $"Policy '{policy.Name}' used by '{site.Name}' has no unique wrapper-ready PolicyContext (Artifact, Subject, Identity, Tenant, Occurred) for this use site.", site.Id));
            return null;
        }

        return new PolicyBinding(body!, requirement, descriptors[0]);
    }

    static bool IsWrapperReadyPolicyContext(SemanticTypedContextDescriptor[] descriptors, AuthorizationSite site) =>
        descriptors.Length == 1 && descriptors[0].IsWrapperReady && IsPolicyContext(descriptors[0], site.Id, site.Subject);

    static SemanticTypedContextDescriptor[] ContextDescriptors(SemanticApplicationContext context, string requirementId, SemanticId operation) =>
        context.Request.TypedContextDescriptors.IsDefault ? [] :
            [.. context.Request.TypedContextDescriptors.Where(descriptor => descriptor?.RequirementId == requirementId && descriptor.OperationId == operation)];

    static bool IsPurePolicyPredicate(SemanticImplementationRequirement requirement) =>
        requirement.Role == SemanticImplementationRole.PolicyPredicate &&
        requirement.ContextVersion == 1 && requirement.ResultVersion == 1 && requirement.RequiredCapability == "pure" &&
        SemanticImplementationBodySelector.Select(requirement).Outcome != SemanticImplementationBodySelector.SelectionOutcome.UndeterminedLanguage;

    // Screenplay's Subject is text; Stage supplies only the canonical text of a text, Uuid, Date or DateTime key.
    static bool ReadsUnsupportedSubject(SemanticApplicationContext context, PureTransitionAdmission.Verdict verdict, SemanticTypedContextDescriptor descriptor, SemanticProperty? subject) =>
        verdict.ContextReads.Contains("Subject") &&
        descriptor.Members.Single(member => member.Name == "Subject").Source.Kind != SemanticContextSourceKinds.Unavailable &&
        !IsTextualSubject(context, subject);

    static bool IsTextualSubject(SemanticApplicationContext context, SemanticProperty? subject) =>
        subject is { Type.IsCollection: false } &&
        SemanticClaimTargets.Primitive(context, subject) is
            SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime;

    static bool IsPolicyContext(SemanticTypedContextDescriptor descriptor, SemanticId operation, SemanticProperty? subject)
    {
        var members = descriptor.Members;
        return HasPolicyContextShape(members) &&
            IsArtifactMember(members[0], operation) &&
            IsRuntimeMember(members[1], "Subject", SemanticContextRuntimeTokens.Text) &&
            IsSubjectSource(members[1], subject) &&
            HasRuntimeContextMembers(members) &&
            members.Skip(2).All(member => member.Source.Kind == SemanticContextSourceKinds.ContextContract && member.Source.Path == member.Name);
    }

    static bool HasRuntimeContextMembers(ImmutableArray<SemanticTypedContextMember> members) =>
        IsRuntimeMember(members[2], "Identity", SemanticContextRuntimeTokens.Identity) &&
        IsRuntimeMember(members[3], "Tenant", SemanticContextRuntimeTokens.TenantId) &&
        IsRuntimeMember(members[4], "Occurred", SemanticContextRuntimeTokens.DateTime);

    static bool HasPolicyContextShape(ImmutableArray<SemanticTypedContextMember> members) =>
        !members.IsDefault && members.Length == 5 && !members.Any(member => member?.Source is null || member.Type is null || member.IsDerived);

    static bool IsArtifactMember(SemanticTypedContextMember member, SemanticId operation) =>
        member.Name == "Artifact" && member.Source.Kind == SemanticContextSourceKinds.AuthorizedOperation &&
        member.Source.SemanticId == operation && member.Type.Kind == SemanticContextTypeKinds.Shape && member.Type.Shape == operation;

    static bool IsRuntimeMember(SemanticTypedContextMember member, string memberName, string token) =>
        member.Name == memberName && member.Type.Kind == SemanticContextTypeKinds.Runtime && member.Type.RuntimeToken == token &&
        member.Type.ModelType is null && member.Type.Shape is null && !member.IsNullable;

    static bool IsSubjectSource(SemanticTypedContextMember member, SemanticProperty? subject) =>
        member.Source.Kind == SemanticContextSourceKinds.Unavailable ||
        ((member.Source.Kind == SemanticContextSourceKinds.CommandIdentifier || member.Source.Kind == SemanticContextSourceKinds.QueryKey) &&
            subject is not null && member.Source.SemanticId == subject.Id);

    static bool ValidateClaimTargets(SemanticApplicationContext context, SemanticAuthorization? authorization, AuthorizationSite site, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (authorization is null)
        {
            return true;
        }

        var paths = Claims(authorization, context.Application.Policies).Where(claim => claim.TargetKind != SemanticClaimTargetKind.Literal)
            .Select(claim => claim.TargetKind == SemanticClaimTargetKind.Subject ? site.Subject?.Name : claim.Value);
        if (paths.All(path => path is not null && IsSupportedClaimPath(context, site.Properties, path)))
        {
            return true;
        }

        diagnostics.Add(Error("STAGE-ESM-015", $"Authorization of '{site.Name}' compares a claim with an artifact value that is neither text, Uuid, Date, DateTime, nor a supported always-denied scalar.", site.Id));
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
        SemanticAuthenticatedCondition or SemanticOpaquePolicyCondition => true,
        SemanticRoleCondition { Role: not null } => true,
        SemanticClaimCondition claim => CanRender(claim),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And or SemanticLogicalOperator.Or } logical =>
            CanRender(logical.Left) && CanRender(logical.Right),
        SemanticNotPolicyCondition not => CanRender(not.Operand),
        _ => false
    };

    static bool CanRender(SemanticClaimCondition claim) => claim is
    { Claim: not null, TargetKind: SemanticClaimTargetKind.Subject, Value: null } or
    { Claim: not null, TargetKind: SemanticClaimTargetKind.Literal or SemanticClaimTargetKind.Artifact, Value: not null };

    static ArtifactRenderDiagnostic NestedAnonymousDiagnostic(string name, SemanticId artifact) =>
        Error("STAGE-ESM-015", $"Authorization of '{name}' combines or nests require anonymous; anonymous access must be the whole authorization.", artifact);

    static ArtifactRenderDiagnostic AnonymousDiagnostic(string kind, string name, SemanticId artifact) =>
        new("STAGE-ESM-060", ArtifactRenderDiagnosticSeverity.Information, $"{kind} '{name}' is explicitly anonymous (require anonymous): any caller may invoke it", artifact);

    sealed record AuthorizationSite(SemanticId Id, string Name, SemanticProperty? Subject, IReadOnlyList<SemanticProperty> Properties);

    sealed record PolicyBinding(string Body, SemanticImplementationRequirement Requirement, SemanticTypedContextDescriptor Descriptor);
}
