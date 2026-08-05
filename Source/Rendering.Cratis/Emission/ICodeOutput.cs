// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Emission;

/// <summary>
/// Writes a rendered file to its destination.
/// </summary>
public interface ICodeOutput
{
    /// <summary>
    /// Writes a rendered file.
    /// </summary>
    /// <param name="file">The file to write.</param>
    /// <param name="targetDirectory">The target application's root directory.</param>
    /// <param name="output">The <see cref="TextWriter"/> progress is reported to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Write(RenderedFile file, DirectoryInfo targetDirectory, TextWriter output);
}
