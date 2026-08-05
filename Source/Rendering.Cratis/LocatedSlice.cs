// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Represents a slice located within an application, together with the module/feature path leading to it.
/// </summary>
/// <param name="Slice">The located <see cref="SliceSyntax"/>.</param>
/// <param name="Path">The module name followed by the (sub-)feature names leading to the slice.</param>
public sealed record LocatedSlice(SliceSyntax Slice, IReadOnlyList<string> Path)
{
    /// <summary>
    /// Gets the full path to the slice, including the slice's own name — the namespace/folder convention is
    /// <c>&lt;Module&gt;.&lt;Feature&gt;.&lt;Slice&gt;</c>, not just <c>&lt;Module&gt;.&lt;Feature&gt;</c>.
    /// </summary>
    public IReadOnlyList<string> FullPath => [.. Path, Slice.Name];
}
