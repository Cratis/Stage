// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// The result of a guarded container removal.
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Detail">Evidence text for a failure, otherwise empty.</param>
public sealed record ContainerRemovalResult(ContainerRemoval Outcome, string Detail)
{
    /// <summary>
    /// Gets a value indicating whether no container of this run remains.
    /// </summary>
    public bool Succeeded => Outcome is ContainerRemoval.NotCreated or ContainerRemoval.Removed or ContainerRemoval.AlreadyGone;
}
