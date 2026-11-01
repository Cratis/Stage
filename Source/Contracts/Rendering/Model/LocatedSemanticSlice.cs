// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

/// <summary>
/// Represents one semantic slice together with the module and features that contain it.
/// </summary>
/// <param name="Module">The containing module.</param>
/// <param name="Features">The containing features, outermost first.</param>
/// <param name="Slice">The slice.</param>
/// <param name="Path">The placement path: module name, feature names, then slice name.</param>
internal sealed record LocatedSemanticSlice(
    SemanticModule Module,
    IReadOnlyList<SemanticFeature> Features,
    SemanticSlice Slice,
    IReadOnlyList<string> Path);
