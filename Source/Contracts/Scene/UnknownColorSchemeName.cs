// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Thrown when a ui profile's appearance names a colour scheme Scene does not know.
/// </summary>
/// <param name="name">The name as the model wrote it.</param>
public class UnknownColorSchemeName(string name) : Exception($"'{name}' is not a known color scheme name - expected 'light', 'dark' or 'system'");
