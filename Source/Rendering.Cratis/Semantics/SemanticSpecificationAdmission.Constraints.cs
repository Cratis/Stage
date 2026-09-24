// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks whether a coded specification error is a reproducible constraint rejection.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    internal static string? ConstraintName(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.ThenErrors.IsEmpty || specification.GivenEvents.IsEmpty || specification.When is null)
        {
            return null;
        }

        var command = context.Commands[specification.When.Command];
        var error = specification.ThenErrors[0];
        var matches = context.Constraints.Select(_ => _.Constraint)
            .Where(constraint => command.Produces.Any(produced => constraint.Targets.Any(target => target.EventContract == produced.EventContract)) &&
                (error.Code == constraint.Name ||
                (error.Code is null && error.Message == MessageFor(constraint) &&
                    specification.GivenEvents.Any(given => constraint.Targets.Any(target => target.EventContract == given.EventContract)))))
            .Select(_ => _.Name).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    static bool HasRenderableErrors(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand? command) =>
        specification.ThenErrors.All(error => SemanticValidationRendering.SafeMessage(error.Message) &&
            (error.Code is null || IsConstraintViolation(context, specification, command, error)));

    static string MessageFor(SemanticConstraint constraint) => constraint.Message ?? (constraint.Kind == SemanticConstraintKind.UniquePropertyValue
        ? $"Constraint '{constraint.Name}' is violated: another event source already holds the constrained value."
        : $"Constraint '{constraint.Name}' is violated: the event source already has the constrained event.");

    static bool IsConstraintViolation(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        SemanticCommand? command,
        SemanticSpecificationError error)
    {
        if (command is null || specification.GivenEvents.IsEmpty)
        {
            return false;
        }

        var constraint = context.Constraints.Select(_ => _.Constraint).SingleOrDefault(_ => _.Name == error.Code);
        if (constraint is null || !command.Produces.Any(produced => constraint.Targets.Any(target => target.EventContract == produced.EventContract)))
        {
            return false;
        }

        return error.Message is null || string.Equals(error.Message, MessageFor(constraint), StringComparison.Ordinal);
    }
}
