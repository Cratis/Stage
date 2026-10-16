// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents the names a derived type inherits from its base classes.
/// </summary>
/// <param name="Members">The inherited member names.</param>
/// <param name="NestedTypes">The inherited nested type names.</param>
internal sealed record InheritedNames(ImmutableHashSet<string> Members, ImmutableHashSet<string> NestedTypes);
