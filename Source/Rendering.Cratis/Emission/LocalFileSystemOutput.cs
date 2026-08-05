// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Emission;

/// <summary>
/// Writes rendered files to the local file system.
/// </summary>
public class LocalFileSystemOutput : ICodeOutput
{
    /// <inheritdoc/>
    public async Task Write(RenderedFile file, DirectoryInfo targetDirectory, TextWriter output)
    {
        var fullPath = Path.Combine(targetDirectory.FullName, file.RelativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, file.Content);
        await output.WriteLineAsync($"Writing {file.RelativePath}");
    }
}
