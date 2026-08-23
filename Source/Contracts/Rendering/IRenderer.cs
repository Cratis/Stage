// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Defines a system that renders a compiled Screenplay application into a target, such as generated source
/// files for a specific platform.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Renders the given applications into the target directory.
    /// </summary>
    /// <param name="applications">The compiled <see cref="ApplicationSyntax">applications</see> to render, merged into one logical model.</param>
    /// <param name="targetDirectory">The directory to render into.</param>
    /// <param name="output">The <see cref="TextWriter"/> progress is reported to.</param>
    /// <param name="error">The <see cref="TextWriter"/> rendering problems are reported to.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation. Implementations fault the task when a
    /// blocking render failure occurs; diagnostics written to <paramref name="error"/> are not a success result.
    /// </returns>
    Task Render(IReadOnlyList<ApplicationSyntax> applications, DirectoryInfo targetDirectory, TextWriter output, TextWriter error);
}
