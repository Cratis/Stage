// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits semantic type declarations.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static void ValidateTypes(SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var concept in context.Application.Concepts)
        {
            if (!GeneratedPascalCase.ConceptMembersAreUnique(concept.Name, concept.Values, context.IdentifierConcepts.Contains(concept.Id)))
            {
                diagnostics.Add(Error("STAGE-ESM-012", $"Concept '{concept.Name}' has members that collide in generated C#.", concept.Id));
            }

            ValidateGenerationBody(context, concept, diagnostics);

            if (concept.Validations.Any(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation))
            {
                diagnostics.Add(Error("STAGE-ESM-005", $"Concept '{concept.Name}' contains validation implementation bodies ({string.Join(", ", concept.Validations.Where(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation).Select(rule => rule.RequirementId))}): Arc does not supply RuleContext.Occurred (received-at) to a generated validator, and Stage does not yet enforce the pure capability.", concept.Id));
                continue;
            }

            if (concept.Primitive == SemanticPrimitiveType.Unknown ||
                (concept.Values.Length > 0 && (concept.Primitive != SemanticPrimitiveType.Text || !concept.Validations.IsEmpty)) ||
                !concept.Validations.All(rule => SemanticValidationRendering.CanRender(rule, context)))
            {
                diagnostics.Add(Error("STAGE-ESM-002", $"Concept '{concept.Name}' uses unsupported values or validation.", concept.Id));
            }
        }

        foreach (var type in context.Application.Types)
        {
            if (!GeneratedPascalCase.RecordMembersAreUnique(type.Name, type.Properties.Select(property => property.Name)))
            {
                diagnostics.Add(Error("STAGE-ESM-012", $"Type '{type.Name}' has property names that collide in generated C#.", type.Id));
            }

            foreach (var property in type.Properties.Where(property => !TypeExists(context, property.Type)))
            {
                diagnostics.Add(Error("STAGE-ESM-003", $"Property '{property.Name}' of '{type.Name}' has an unresolved type.", type.Id));
            }
        }
    }

    static void ValidateGenerationBody(SemanticApplicationContext context, SemanticConcept concept, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (!SemanticGenerationRendering.IsRule(concept.Generation)) return;
        var generation = concept.Generation!;
        var verified = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, generation.RequirementId!, concept.Id, verified, out var body))
        {
            diagnostics.AddRange(verified);
            return;
        }

        var requirement = context.Request.ImplementationRequirements.Single(candidate => candidate.RequirementId == generation.RequirementId);
        if (requirement.Role != SemanticImplementationRole.ValueGeneration || requirement.ContextVersion != 1 || requirement.ResultVersion != 1 ||
            requirement.RequiredCapability != "provider-defined" ||
            !((requirement.Language == "csharp" && requirement.File is null) ||
                (requirement.Language is null && requirement.File?.EndsWith(".cs", StringComparison.Ordinal) == true)))
        {
            diagnostics.Add(Error("STAGE-ESM-019", $"Concept '{concept.Name}' has an unsupported generation envelope or language.", concept.Id));
            return;
        }

        // SemanticTypedContextAdmission.Verify has already admitted every descriptor (role, version,
        // wrapper readiness, model revision, uniqueness). It tolerates an empty envelope for callers
        // without typed contexts, so the use site still has to prove it came from this compilation.
        if (!context.Request.TypedContextDescriptors.Any(descriptor => descriptor.RequirementId == generation.RequirementId && descriptor.OperationId == concept.Id))
        {
            diagnostics.Add(Error("STAGE-ESM-021", $"Concept '{concept.Name}' has no typed context for generation '{generation.RequirementId}' from this compilation.", concept.Id));
            return;
        }

        var verdict = SemanticGenerationBodyAdmission.Analyze(body!, concept, context, requirement);
        if (!verdict.Accepted) diagnostics.Add(Error(verdict.Code!, verdict.Reason!, concept.Id));
    }
}
