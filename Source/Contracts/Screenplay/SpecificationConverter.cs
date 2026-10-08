// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts the Screenplay <see cref="SpecificationSyntax">specifications</see> of a slice into Stage
/// <see cref="Specification"/> records — translating <c language="csharp">given</c>/<c language="csharp">when</c>/<c language="csharp">then</c> steps and rendering each
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
                DeterministicId.From($"{specificationPath}.given.{index}.{Name(@event.EventType, slicePath)}"),
                Name(@event.EventType, slicePath),
                DeclarationId(@event.EventType, "event", slicePath),
                Values(@event.Values)))
            .ToArray();

        var when = specification.When is { } command
            ? new SpecificationCommand(
                DeterministicId.From($"{specificationPath}.when.{Name(command.CommandType, slicePath)}"),
                DeclarationId(command.CommandType, "command", slicePath),
                Name(command.CommandType, slicePath),
                Values(command.Values))
            : null;

        var thenEvents = specification.ThenEvents
            .Select((@event, index) => new SpecificationThenEvent(
                DeterministicId.From($"{specificationPath}.then.{index}.{Name(@event.EventType, slicePath)}"),
                Name(@event.EventType, slicePath),
                DeclarationId(@event.EventType, "event", slicePath),
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
            thenErrors)
        {
            GivenReadModels = ReadModels(specification.GivenReadModels, $"{specificationPath}.given", slicePath),
            ThenReadModels = ReadModels(specification.ThenReadModels, $"{specificationPath}.then", slicePath)
        };
    }

    // The read model is referred to by name, resolved to the same identifier ReadModelConverter derives for the
    // slice's read model — the way the event and command steps already resolve what they refer to.
    static IReadOnlyList<SpecificationReadModel> ReadModels(
        IEnumerable<SpecificationReadModelSyntax>? readModels,
        string stepPath,
        string slicePath) =>
    [
        .. (readModels ?? []).Select((readModel, index) => new SpecificationReadModel(
            DeterministicId.From($"{stepPath}.readmodel.{index}.{Name(readModel.Name, slicePath)}"),
            Name(readModel.Name, slicePath),
            DeclarationId(readModel.Name, "readmodel", slicePath),
            Values(readModel.Properties)))
    ];

    static string Name(string reference, string slicePath) => reference.StartsWith($"{slicePath}.", StringComparison.Ordinal)
        ? reference[(slicePath.Length + 1)..]
        : reference;

    static Guid DeclarationId(string reference, string kind, string slicePath)
    {
        var separator = reference.LastIndexOf('.');
        var owner = separator < 0 ? slicePath : reference[..separator];
        return DeterministicId.From($"{owner}.{kind}.{reference[(separator + 1)..]}");
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
