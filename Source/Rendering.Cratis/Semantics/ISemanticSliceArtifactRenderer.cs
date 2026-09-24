// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders a supported slice and its specifications in model order.
/// </summary>
internal interface ISemanticSliceArtifactRenderer
{
    /// <summary>
    /// Determines whether this renderer handles the slice.
    /// </summary>
    /// <param name="located">The selected slice.</param>
    /// <returns>Whether this renderer handles the slice.</returns>
    bool Handles(LocatedSemanticSlice located);

    /// <summary>
    /// Renders the slice and its specifications.
    /// </summary>
    /// <param name="located">The selected slice.</param>
    /// <param name="context">The indexed application.</param>
    /// <returns>The generated files in model order.</returns>
    IEnumerable<RenderedFile> Render(LocatedSemanticSlice located, SemanticApplicationContext context);
}
