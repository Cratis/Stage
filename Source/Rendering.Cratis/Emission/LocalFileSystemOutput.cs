// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
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

    /// <inheritdoc/>
    public async Task<bool> TryWriteFailureMarker(DirectoryInfo targetDirectory, TextWriter output)
    {
        Directory.CreateDirectory(targetDirectory.FullName);
        var markerPath = Path.Combine(targetDirectory.FullName, RenderFailureMarker.RelativePath);

        FileStream stream;
        try
        {
            stream = new FileStream(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        }
        catch (IOException) when (File.Exists(markerPath))
        {
            return false;
        }

        await using (stream)
        await using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            await writer.WriteAsync(RenderFailureMarker.Content);
        }

        await output.WriteLineAsync($"Writing {RenderFailureMarker.RelativePath}");
        return true;
    }
}
