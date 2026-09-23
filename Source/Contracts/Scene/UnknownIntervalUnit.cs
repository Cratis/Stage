// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when an interval trigger carries a unit that <see cref="BehaviorConverter"/>
/// does not know how to express in seconds.
/// </summary>
/// <param name="unit">The unrecognized unit.</param>
public class UnknownIntervalUnit(string unit) : Exception($"'{unit}' is not a known interval unit");
