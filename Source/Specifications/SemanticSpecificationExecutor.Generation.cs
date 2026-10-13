// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications;

public sealed partial class SemanticSpecificationExecutor
{
    static string? StrategyFixtureFailure(SemanticRunContext context, ImmutableArray<SemanticPropertyValue> generated)
    {
        foreach (var fixture in generated)
        {
            var property = context.Command.Properties.SingleOrDefault(property => property.Id == fixture.TargetProperty && property.IsGenerated);
            if (property is null || generated.Count(value => value.TargetProperty == fixture.TargetProperty) != 1)
            {
                return $"Command '{context.Command.Name}' generated values do not match its generated property shape.";
            }
            var concept = context.Plan.Model.Application.Concepts.Single(concept => concept.Id == property.Type.Target);
            if (!SemanticGenerationShapes.Matches(concept, fixture.Value)) return SemanticGenerationShapes.FixtureMessage(property.Name);
        }

        return null;
    }

    static bool ReusesGeneratedSource(SemanticRunContext context, IEnumerable<SemanticValue> destinations)
    {
        if (!context.Plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15)) return false;
        foreach (var fixture in context.Specification.When!.GeneratedValues)
        {
            var property = context.Command.Properties.Single(property => property.Id == fixture.TargetProperty);
            var concept = context.Plan.Model.Application.Concepts.Single(concept => concept.Id == property.Type.Target);
            var uuid = concept.Primitive == SemanticPrimitiveType.Uuid;
            if (destinations.Any(destination => SameGeneratedSource(destination, fixture.Value, uuid)) &&
                context.Destinations.Any(prior => prior is not SemanticNullValue && SameGeneratedSource(prior, fixture.Value, uuid)))
            {
                return true;
            }
        }

        return false;
    }

    static bool SameGeneratedSource(SemanticValue left, SemanticValue right, bool uuid) =>
        uuid && left is SemanticTextValue first && right is SemanticTextValue second &&
        Guid.TryParse(first.Value, out var firstUuid) && Guid.TryParse(second.Value, out var secondUuid)
            ? firstUuid == secondUuid : Equals(left, right);
}
