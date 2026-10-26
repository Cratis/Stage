// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// The exception that is thrown when an authored name cannot become a valid generated identifier.
/// </summary>
/// <param name="name">The authored name that was refused.</param>
public sealed class InvalidGeneratedIdentifier(string name) : Exception($"The authored name '{name}' starts with a combining mark after generated identifier normalization.")
{
    internal string Code => "STAGE-ESM-019";
}
