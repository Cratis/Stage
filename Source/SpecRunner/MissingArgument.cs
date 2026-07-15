// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.SpecRunner;

/// <summary>
/// The exception that is thrown when a required command-line argument is missing.
/// </summary>
/// <param name="name">The name of the missing argument.</param>
public class MissingArgument(string name)
    : Exception($"Required argument '{name}' was not provided.");
