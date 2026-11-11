// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Extensions;

/// <summary>
/// What came of raising a trigger.
/// </summary>
/// <param name="Status">The status: <c lang="text">Raised</c>, <c lang="text">UnknownTrigger</c>, <c lang="text">NotRouted</c> (nothing in the model reacts to it) or <c lang="text">Refused</c>.</param>
/// <param name="Detail">More about it.</param>
public sealed record TriggerResult(string Status, string Detail)
{
    /// <summary>
    /// Gets a value indicating whether the trigger was raised.
    /// </summary>
    public bool Raised => Status == "Raised";
}
