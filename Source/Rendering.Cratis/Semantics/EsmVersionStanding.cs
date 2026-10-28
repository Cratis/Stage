// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Defines the standing of an exact language and semantic version pair for one target.
/// </summary>
internal enum EsmVersionStanding
{
    Audited,
    NewerWithinRange,
    FutureMajor,
    Unadmitted,
    Malformed
}
