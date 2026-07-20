// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay <see cref="SpecificationSyntax">specifications</see> of a slice into Stage
/// <see cref="Specification"/> records — translating <c>given</c>/<c>when</c>/<c>then</c> steps and rendering each
/// step's property values as a JSON object string.
/// </summary>
public static class SpecificationConverter
{
    /// <summary>
    /// Converts a specification declaration into its Stage record.
    /// </summary>
    /// <param name="specification">The specification to convert.</param>
    /// <param name="slicePath">The fully-qualified slice path, used to derive stable identifiers and resolve referenced events and commands.</param>
    /// <returns>The Stage specification.</returns>
    public static Specification Convert(SpecificationSyntax specification, string slicePath)
    {
        var specificationPath = $"{slicePath}.spec.{specification.Name}";

        var given = specification.Given
            .Select((@event, index) => new SpecificationGivenEvent(
                DeterministicId.From($"{specificationPath}.given.{index}.{@event.EventType}"),
                @event.EventType,
                DeterministicId.From($"{slicePath}.event.{@event.EventType}"),
                Values(@event.Values)))
            .ToArray();

        var when = specification.When is { } command
            ? new SpecificationCommand(
                DeterministicId.From($"{specificationPath}.when.{command.CommandType}"),
                DeterministicId.From($"{slicePath}.command.{command.CommandType}"),
                command.CommandType,
                Values(command.Values))
            : null;

        var thenEvents = specification.ThenEvents
            .Select((@event, index) => new SpecificationThenEvent(
                DeterministicId.From($"{specificationPath}.then.{index}.{@event.EventType}"),
                @event.EventType,
                DeterministicId.From($"{slicePath}.event.{@event.EventType}"),
                Values(@event.Values)))
            .ToArray();

        var thenErrors = specification.ThenErrors
            .Select((error, index) => new SpecificationError(
                DeterministicId.From($"{specificationPath}.error.{index}.{error.Name}"),
                error.Name))
            .ToArray();

        return new Specification(
            DeterministicId.From(specificationPath),
            specification.Name,
            given,
            when,
            thenEvents,
            thenErrors);
    }

    static string Values(IEnumerable<PropertyMappingSyntax> mappings)
    {
        var values = new JsonObject();
        foreach (var mapping in mappings)
        {
            values[mapping.Property] = ScreenplayExpression.ToJsonValue(mapping.Source);
        }

        return values.ToJsonString();
    }
}
