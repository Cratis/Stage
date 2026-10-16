// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents a type declared by the generated project's own source, as opposed to a referenced framework type.
/// </summary>
/// <param name="Namespace">The full namespace, empty for the global namespace.</param>
/// <param name="Name">The type name.</param>
/// <param name="Arity">The generic arity.</param>
internal sealed record GeneratedTypeName(string Namespace, string Name, int Arity = 0);
