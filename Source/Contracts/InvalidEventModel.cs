// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts;

/// <summary>
/// The exception that is thrown when an event model file cannot be read or deserialized into a model.
/// </summary>
/// <param name="path">The path to the event model file that could not be loaded.</param>
public class InvalidEventModel(string path)
    : Exception($"The event model file at '{path}' could not be read as a valid event model.");
