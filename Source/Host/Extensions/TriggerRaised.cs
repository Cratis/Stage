// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// The result of raising a trigger.
/// </summary>
/// <param name="Status">The <see cref="TriggerRaiseStatus"/>.</param>
/// <param name="Detail">More about it, which says why for anything but raised.</param>
public sealed record TriggerRaised(TriggerRaiseStatus Status, string Detail = "");
