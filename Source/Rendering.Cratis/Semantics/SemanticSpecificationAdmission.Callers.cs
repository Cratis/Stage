// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks caller fixtures and supported specification actions.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    // The command scenario evaluates the generated Arc policy with the fixture's principal.
    // A lowered reaction invocation may deliberately have no caller, even for a denial.
    internal static bool HasRenderableCallerAndCommand(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        out SemanticCommand? command,
        bool reactionInvocation = false)
    {
        command = null;
        return specification.When is not null &&
            context.Commands.TryGetValue(specification.When.Command, out command) &&
            specification.GivenCaller?.Claims.Any(claim => SemanticCratisAdmission.IsRoleClaim(claim.Type)) != true &&
            HasRenderableCommandDenial(context, specification, command, reactionInvocation) &&
            specification.ThenQueries.All(_ => context.Queries.TryGetValue(_.Query, out var query) &&
                (!IsProtected(context, query) || specification.GivenCaller is not null));
    }

    internal static bool CanDenyQueryOnly(SemanticSpecification specification, SemanticApplicationContext context) =>
        specification.When is null && specification.WhenAppended is null && specification.ThenDenied &&
        specification.GivenCaller is { } caller &&
        !caller.Claims.Any(claim => SemanticCratisAdmission.IsRoleClaim(claim.Type)) &&
        specification.GivenEvents.IsEmpty && specification.GivenReadModels.IsEmpty &&
        specification.ThenEvents.IsEmpty && specification.ThenReadModels.IsEmpty && specification.ThenErrors.IsEmpty &&
        specification.ThenQueries.Length == 1 && specification.ThenQueries[0].Results.IsEmpty &&
        HasProtectedQueryWithRenderableKey(specification.ThenQueries[0], context);

    internal static bool CanSeedQueryOnly(SemanticSpecification specification, SemanticApplicationContext context)
    {
        if (specification.When is not null || specification.WhenAppended is not null ||
            !specification.GivenEvents.IsEmpty ||
            specification.GivenReadModels.Length != 1 || specification.ThenQueries.Length != 1 || !specification.ThenAbsentReadModels.IsEmpty ||
            !specification.ThenEvents.IsEmpty || !specification.ThenReadModels.IsEmpty ||
            !specification.ThenErrors.IsEmpty || specification.ThenDenied)
        {
            return false;
        }

        var given = specification.GivenReadModels[0];
        var expected = specification.ThenQueries[0];
        return TryGetSeedableQuery(expected, context, out var query) &&
            (specification.GivenCaller is null || SemanticAnonymousAccess.IsAnonymous(query.Authorization, context.Application.Policies)) &&
            context.ReadModels.TryGetValue(given.ReadModel, out var readModel) && query.ReadModel == given.ReadModel &&
            Equals(given.Key, expected.Key) && expected.Results.Length == 1 &&
            ValuesMatch(given.Values, readModel.Properties) && IsScalar(given.Key) &&
            readModel.Properties.SingleOrDefault(_ => _.IsIdentifier) is { } identifier &&
            given.Values.Any(_ => _.TargetProperty == identifier.Id && Equals(_.Value, given.Key));
    }

    static bool HasRenderableCommandDenial(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command, bool reactionInvocation) =>
        !specification.ThenDenied || (specification.ThenQueries.IsEmpty && SemanticAnonymousAccess.RequiresCaller(command.Authorization, context.Application.Policies) &&
            (specification.GivenCaller is not null || reactionInvocation));

    static bool IsProtected(SemanticApplicationContext context, SemanticKeyedQuery query) =>
        SemanticAnonymousAccess.RequiresCaller(query.Authorization, context.Application.Policies);
}
