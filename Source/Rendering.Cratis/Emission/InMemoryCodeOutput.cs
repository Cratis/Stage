// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Emission;

/// <summary>
/// Collects rendered files in memory instead of writing them to disk — used by specs.
/// </summary>
public class InMemoryCodeOutput : ICodeOutput
{
    readonly List<RenderedFile> _files = [];

    /// <summary>
    /// Gets every file written so far.
    /// </summary>
    public IReadOnlyList<RenderedFile> Files => _files;

    /// <inheritdoc/>
    public Task Write(RenderedFile file, DirectoryInfo targetDirectory, TextWriter output)
    {
        _files.Add(file);
        return Task.CompletedTask;
    }
}
