// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Represents a <c>reads</c> declaration on a command — the read model the command consults before it decides.
/// </summary>
/// <param name="ReadModel">The name of the read model the command reads.</param>
/// <param name="By">The command property the read model is looked up by, or <see langword="null"/> when the read
/// model is not keyed — a single view the whole application shares rather than one instance per identifier.</param>
public record ReadsDefinition(string ReadModel, string? By);
