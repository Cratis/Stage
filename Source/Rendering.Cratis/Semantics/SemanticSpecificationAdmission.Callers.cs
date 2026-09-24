// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks caller fixtures and supported specification actions.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    // A caller fixture and a denial assertion only mean something against rendered authorization, which
    // admission rejects. An appended-event action has no command, so it has no When either.
    static bool HasRenderableCallerAndCommand(
        SemanticApplicationContext context,
        SemanticSpecification specification,
        out SemanticCommand? command)
    {
        command = null;
        return specification.When is not null && specification.GivenCaller is null && !specification.ThenDenied &&
            context.Commands.TryGetValue(specification.When.Command, out command);
    }
}
