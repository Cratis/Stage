// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static void ValidateMatchPatterns(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var owners = context.Application.Concepts.Select(concept => (concept.Id, concept.Name, concept.Validations))
            .Concat(slices.SelectMany(slice => slice.Slice.Commands.Select(command => (command.Id, command.Name, command.Validations))));
        foreach (var (id, name, rules) in owners)
        {
            foreach (var rule in rules.Where(rule => rule.Kind == SemanticValidationRuleKind.Matches))
            {
                if (rule.Operand is SemanticTextValue pattern && PortableMatchPattern.Check(pattern.Value) is { Status: not PortableMatchStatus.Admitted } check)
                {
                    diagnostics.Add(Error("STAGE-ESM-005", $"'{name}' validation '{rule.Name ?? rule.Kind.ToString()}' cannot preserve match construct '{check.Construct}' in generated code. {check.Code}: {check.Description}", id));
                }
            }
        }
    }
}
