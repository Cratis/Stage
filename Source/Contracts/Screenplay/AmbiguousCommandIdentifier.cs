// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// The exception that is thrown when a command declares more than one property as its identifier, leaving the engine
/// with no way to choose which one identifies the event source.
/// </summary>
/// <param name="command">The name of the command declaring the identifiers.</param>
/// <param name="properties">The names of the properties marked as the identifier.</param>
public class AmbiguousCommandIdentifier(string command, IEnumerable<string> properties)
    : Exception($"Command '{command}' marks more than one property as identifier ({string.Join(", ", properties)}) - only one property can be the identifier");
