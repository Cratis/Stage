// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks caller fixtures and supported specification actions.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    // The command scenario evaluates the generated Arc policy with the fixture's principal.
    // An appended-event or query-only action has no command scenario to execute.
    internal static bool HasRenderableCallerAndCommand(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        out SemanticCommand? command)
    {
        command = null;
        return specification.When is not null &&
            context.Commands.TryGetValue(specification.When.Command, out command) &&
            specification.GivenCaller?.Claims.Any(claim => SemanticCratisAdmission.IsRoleClaim(claim.Type)) != true &&
            (!specification.ThenDenied || (specification.ThenQueries.IsEmpty && command.Authorization is not null && specification.GivenCaller is not null)) &&
            specification.ThenQueries.All(_ => context.Queries.TryGetValue(_.Query, out var query) && query.Authorization is null);
    }

    internal static bool CanSeedQueryOnly(SemanticSpecification specification, SemanticApplicationContext context)
    {
        if (specification.When is not null || specification.WhenAppended is not null ||
            specification.GivenCaller is not null || !specification.GivenEvents.IsEmpty ||
            specification.GivenReadModels.Length != 1 || specification.ThenQueries.Length != 1 ||
            !specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty ||
            !specification.ThenErrors.IsEmpty || specification.ThenDenied)
        {
            return false;
        }

        var given = specification.GivenReadModels[0];
        var expected = specification.ThenQueries[0];
        return context.Queries.TryGetValue(expected.Query, out var query) && query.Authorization is null &&
            context.ReadModels.TryGetValue(given.ReadModel, out var readModel) && query.ReadModel == given.ReadModel &&
            Equals(given.Key, expected.Key) && expected.Results.Length == 1 &&
            ValuesMatch(given.Values, readModel.Properties) && IsScalar(given.Key) &&
            readModel.Properties.SingleOrDefault(_ => _.IsIdentifier) is { } identifier &&
            given.Values.Any(_ => _.TargetProperty == identifier.Id && Equals(_.Value, given.Key));
    }
}
