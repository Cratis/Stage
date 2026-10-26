// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>A semantic command's structural path, owner and allocated runtime key.</summary>
/// <param name="Command">The semantic command.</param>
/// <param name="Slice">The owning slice.</param>
/// <param name="Path">The complete structural path.</param>
/// <param name="Key">The allocated runtime key.</param>
internal sealed record SceneCommandEntry(SemanticCommand Command, LocatedSemanticSlice Slice, ImmutableArray<string> Path, string Key);
