// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static string? MissingConstrainedGeneratedFixture(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is not { } action || !context.Commands.TryGetValue(action.Command, out var command)) return null;

        foreach (var constraint in context.Constraints.Select(selected => selected.Constraint).Where(constraint => constraint.Kind == SemanticConstraintKind.UniquePropertyValue))
        {
            var assertedCollision = specification.ThenErrors.Any(error => error.Code == constraint.Name || error.Message == MessageFor(constraint));
            var claims = ActiveClaimSources(constraint, specification);
            foreach (var produced in command.Produces)
            {
                var target = constraint.Targets.SingleOrDefault(target => target.EventContract == produced.EventContract);
                if (target is null) continue;
                var destination = SemanticDestinations.Of(command, produced) is SemanticResolvedExpression source
                    ? action.Values.Concat(action.GeneratedValues).SingleOrDefault(value => value.TargetProperty == source.Target)?.Value
                    : null;
                if (!assertedCollision && !claims.Any(claim => claim is null || destination is null || claim.Value != destination)) continue;

                foreach (var mapping in produced.Mappings.Where(mapping => target.Properties.Contains(mapping.TargetProperty)))
                {
                    if (mapping.Source is not SemanticResolvedExpression mapped) continue;
                    var property = command.Properties.SingleOrDefault(property => property.Id == mapped.Target && property.IsGenerated);
                    if (property is not null && !action.GeneratedValues.Any(value => value.TargetProperty == property.Id))
                    {
                        return $"Specification '{specification.Name}' needs a deterministic generated fixture for '{property.Name}' under constraint '{constraint.Name}'; an unpinned draw could change the constraint outcome.";
                    }
                }
            }
        }

        return null;
    }

    static HashSet<SemanticEventSourceIdentity?> ActiveClaimSources(SemanticConstraint constraint, SemanticSpecification specification)
    {
        var claims = new HashSet<SemanticEventSourceIdentity?>();
        foreach (var given in specification.GivenEvents)
        {
            if (constraint.Targets.Any(target => target.EventContract == given.EventContract))
            {
                claims.Add(given.EventSource);
            }
            else if (constraint.ReleasedBy.Contains(given.EventContract))
            {
                claims.Remove(given.EventSource);
            }
        }

        return claims;
    }
}
