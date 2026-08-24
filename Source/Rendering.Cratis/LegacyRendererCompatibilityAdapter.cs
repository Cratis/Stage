// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Keeps the published syntax-based <see cref="IRenderer"/> contract isolated from direct ESM artifact planning.
/// </summary>
/// <param name="render">The existing syntax renderer operation.</param>
internal sealed class LegacyRendererCompatibilityAdapter(
    Func<IReadOnlyList<ApplicationSyntax>, DirectoryInfo, TextWriter, TextWriter, Task> render) : IRenderer
{
    /// <inheritdoc/>
    public Task Render(
        IReadOnlyList<ApplicationSyntax> applications,
        DirectoryInfo targetDirectory,
        TextWriter output,
        TextWriter error) =>
        render(applications, targetDirectory, output, error);
}
