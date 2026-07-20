// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts a Screenplay <see cref="ScreenplaySyntax.SliceSyntax"/> into a Stage <see cref="Slice"/>, delegating the
/// command, events, read model and specifications to their focused converters.
/// </summary>
public static class SliceConverter
{
    /// <summary>
    /// Converts a slice declaration into its Stage record.
    /// </summary>
    /// <param name="slice">The slice to convert.</param>
    /// <param name="schema">The schema synthesizer.</param>
    /// <param name="eventPropertyTypes">The global map of event property name to Screenplay type name, used to infer read-model property types.</param>
    /// <param name="featurePath">The fully-qualified feature path the slice belongs to.</param>
    /// <returns>The Stage slice.</returns>
    public static Slice Convert(
        ScreenplaySyntax.SliceSyntax slice,
        SchemaSynthesizer schema,
        IReadOnlyDictionary<string, string> eventPropertyTypes,
        string featurePath)
    {
        var slicePath = $"{featurePath}.{slice.Name}";
        var command = slice.Commands.FirstOrDefault();

        return new Slice(
            DeterministicId.From(slicePath),
            slice.Name,
            MapType(slice.Type),
            EventConverter.Convert(slice.Events, slice.Constraints, schema, slicePath),
            command is not null ? CommandConverter.Convert(command, schema, slicePath) : null,
            ReadModelConverter.Convert(slice, schema, eventPropertyTypes, slicePath),
            [.. slice.Specifications.Select(specification => SpecificationConverter.Convert(specification, slicePath))]);
    }

    static SliceType MapType(ScreenplaySyntax.SliceType type) =>
        type switch
        {
            ScreenplaySyntax.SliceType.StateChange => SliceType.StateChange,
            ScreenplaySyntax.SliceType.StateView => SliceType.StateView,
            ScreenplaySyntax.SliceType.Automation => SliceType.Automation,
            ScreenplaySyntax.SliceType.Translate => SliceType.Translator,
            _ => SliceType.StateChange
        };
}
