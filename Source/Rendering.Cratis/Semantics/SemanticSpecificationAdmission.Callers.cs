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
    static bool HasRenderableCallerAndCommand(
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
}
