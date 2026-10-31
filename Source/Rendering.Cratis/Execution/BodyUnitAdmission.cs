// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Reuses the renderer's integrity, descriptor and pure-body admission gates.</summary>
internal static partial class BodyUnitAdmission
{
    internal static bool Supported(SemanticImplementationRole role) => role is SemanticImplementationRole.RulePredicate or SemanticImplementationRole.CommandValidation or SemanticImplementationRole.ConceptValidation or SemanticImplementationRole.ReducerTransition;

    internal static string NotExecuted(SemanticImplementationRole role) => BodyDiagnostics.NotExecuted(role);

    internal static (SemanticTypedContextDescriptor? Descriptor, string? Body, BodyUnitEntry? Refusal) Admit(SemanticApplicationContext context, SemanticImplementationRequirement requirement)
    {
        var id = requirement.RequirementId;
        if (!Supported(requirement.Role)) return (null, null, Refuse(requirement, "STAGE-IMPL-003", NotExecuted(requirement.Role)));
        var owner = requirement.Source?.SemanticId ?? context.Application.Id;
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, id, owner, diagnostics, out var body))
        {
            var diagnostic = diagnostics.First();
            return (null, null, Refuse(requirement, diagnostic.Code, diagnostic.Message));
        }
        if (!ValidBinding(context, owner, requirement))
        {
            return (null, null, Refuse(requirement, "STAGE-ESM-019", "The validation requirement has an unsupported binding, envelope or language."));
        }
        var descriptors = context.Request.TypedContextDescriptors.Where(descriptor => descriptor.RequirementId == id && descriptor.OperationId == owner).ToArray();
        if (!ValidDescriptor(descriptors, requirement))
        {
            return (null, null, Refuse(requirement, "STAGE-ESM-021", "No unique wrapper-ready typed context is available."));
        }
        var descriptor = descriptors[0];
        var error = RendererError(context, owner);
        if (error is not null) return (null, null, Refuse(requirement, error.Code, error.Message));
        return Analyze(context, requirement, descriptor, body!);
    }

    internal static BodyUnitEntry Refuse(SemanticImplementationRequirement requirement, string code, string message) => new(requirement.RequirementId, requirement.Role.ToString(), false, code, BodyDiagnostics.Format(code, message));

    static (SemanticTypedContextDescriptor? Descriptor, string? Body, BodyUnitEntry? Refusal) Analyze(
        SemanticApplicationContext context, SemanticImplementationRequirement requirement, SemanticTypedContextDescriptor descriptor, string body)
    {
        var verdict = PureValidationAdmission.Analyze(body, context, descriptor, requirement);
        return verdict.Accepted ? (descriptor, body, null) : (null, null, Refuse(requirement, verdict.Code!, verdict.Reason!));
    }

    static bool ValidBinding(SemanticApplicationContext context, SemanticId owner, SemanticImplementationRequirement requirement) =>
        Referenced(context, owner, requirement.RequirementId) && ValidEnvelope(requirement);

    static bool Referenced(SemanticApplicationContext context, SemanticId owner, string id) =>
        context.Application.Concepts.SelectMany(concept => concept.Validations.Select(rule => (Owner: concept.Id, rule.RequirementId)))
            .Concat(context.Commands.Values.SelectMany(command => command.Validations.Select(rule => (Owner: command.Id, rule.RequirementId))
                .Concat(command.CodeValidations.Select(validation => (Owner: command.Id, (string?)validation.RequirementId)))))
            .Contains((owner, id));

    static bool ValidEnvelope(SemanticImplementationRequirement requirement) =>
        requirement.ContextVersion == 1 && requirement.ResultVersion == 1 && requirement.RequiredCapability == "pure" && ValidLanguage(requirement);

    static bool ValidLanguage(SemanticImplementationRequirement requirement) =>
        (requirement.Language == "csharp" && requirement.File is null) ||
        (requirement.Language is null && requirement.File?.EndsWith(".cs", StringComparison.Ordinal) == true);

    static bool ValidDescriptor(SemanticTypedContextDescriptor[] descriptors, SemanticImplementationRequirement requirement) =>
        descriptors.Length == 1 && descriptors[0].IsWrapperReady && descriptors[0].Role == requirement.Role;

    static ArtifactRenderDiagnostic? RendererError(SemanticApplicationContext context, SemanticId owner) =>
        SemanticCratisAdmission.Evaluate(context, context.AllSlices).FirstOrDefault(diagnostic => diagnostic.Artifact == owner && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error &&
            new[] { "STAGE-ESM-019", "STAGE-ESM-021", "STAGE-ESM-015", "STAGE-ESM-056" }.Contains(diagnostic.Code, StringComparer.Ordinal));
}
