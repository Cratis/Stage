// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception thrown when a command property has a type no command form schema can describe.
/// </summary>
/// <param name="type">The type that cannot be described.</param>
public sealed class UnsupportedCommandFormSchema(string type) : Exception($"A command form schema cannot describe a property of type '{type}'.");
