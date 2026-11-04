// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// What an inspect of one container established.
/// </summary>
public enum GeneratedApplicationContainerInspectionKind
{
    /// <summary>The container exists and its identifier and labels were read.</summary>
    Found,

    /// <summary>Docker confirmed the container does not exist.</summary>
    Missing,

    /// <summary>The result is not trustworthy: a daemon, permission, timeout or parse failure, or a different identifier.</summary>
    Unknown
}
