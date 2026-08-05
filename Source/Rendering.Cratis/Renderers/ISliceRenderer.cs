// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders one slice type into its generated file.
/// </summary>
public interface ISliceRenderer
{
    /// <summary>
    /// Renders a slice.
    /// </summary>
    /// <param name="slice">The located slice to render.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> the slice was declared in.</param>
    /// <param name="rootNamespace">The root namespace of the target application.</param>
    /// <returns>The <see cref="RenderedFile"/>.</returns>
    RenderedFile Render(LocatedSlice slice, ApplicationSet applicationSet, string rootNamespace);
}
