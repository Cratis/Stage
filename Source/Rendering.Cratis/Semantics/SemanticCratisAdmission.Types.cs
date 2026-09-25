// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

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
            if (concept.Validations.Any(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation))
            {
                diagnostics.Add(Error("STAGE-ESM-005", $"Concept '{concept.Name}' contains validation implementation bodies that Stage cannot execute or render.", concept.Id));
                continue;
            }

            if (concept.Primitive == SemanticPrimitiveType.Unknown ||
                (concept.Values.Length > 0 && (concept.Primitive != SemanticPrimitiveType.Text || !concept.Validations.IsEmpty)) ||
                !concept.Validations.All(SemanticValidationRendering.CanRender))
            {
                diagnostics.Add(Error("STAGE-ESM-002", $"Concept '{concept.Name}' uses unsupported values or validation.", concept.Id));
            }
        }

        foreach (var type in context.Application.Types)
        {
            foreach (var property in type.Properties.Where(property => !TypeExists(context, property.Type)))
            {
                diagnostics.Add(Error("STAGE-ESM-003", $"Property '{property.Name}' of '{type.Name}' has an unresolved type.", type.Id));
            }
        }
    }
}
