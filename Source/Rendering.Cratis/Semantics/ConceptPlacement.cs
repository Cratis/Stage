// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Defines which project a rendered concept or composite type source belongs to.
/// </summary>
internal enum ConceptPlacement
{
    /// <summary>
    /// The whole declaration renders into the application.
    /// </summary>
    Application,

    /// <summary>
    /// The declaration an event contract needs renders into the contracts project, without its Arc validator.
    /// </summary>
    Contracts,

    /// <summary>
    /// Only the Arc validator renders, into the application, for a concept whose declaration is in the contracts project.
    /// </summary>
    Validator
}
