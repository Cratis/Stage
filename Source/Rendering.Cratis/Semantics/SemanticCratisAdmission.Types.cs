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
            if (!GeneratedPascalCase.ConceptMembersAreUnique(
                concept.Name,
                concept.Values,
                context.IdentifierConcepts.Contains(concept.Id) && !SemanticCommonArtifactRenderer.IsGeneratable(concept, context)))
            {
                diagnostics.Add(Error("STAGE-ESM-012", $"Concept '{concept.Name}' has members that collide in generated C#.", concept.Id));
            }

            ValidateGenerationStrategy(concept, diagnostics);
            ValidateGenerationBody(context, concept, diagnostics);
            RememberGenerationRefusals(context, concept, diagnostics);

            foreach (var rule in concept.Validations.Where(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation))
            {
                ValidateRuleBody(context, rule, concept.Id, null, diagnostics);
            }

            if (concept.Primitive == SemanticPrimitiveType.Unknown ||
                (concept.Values.Length > 0 && (concept.Primitive != SemanticPrimitiveType.Text || !concept.Validations.IsEmpty)) ||
                !concept.Validations.All(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation || SemanticValidationRendering.CanRender(rule, context)))
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

    static void RememberGenerationRefusals(SemanticApplicationContext context, SemanticConcept concept, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics.Where(diagnostic => diagnostic.Artifact == concept.Id && SemanticGenerationRendering.IsGenerationFailure(context, diagnostic)))
        {
            context.GenerationFailures[concept.Id] = diagnostic.Message;
        }
    }

    static void ValidateGenerationStrategy(SemanticConcept concept, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (SemanticGenerationRendering.Rejection(concept.Generation) is { } rejection)
        {
            diagnostics.Add(Error("STAGE-ESM-012", rejection, concept.Id));
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
