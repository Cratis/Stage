// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Publishing;

namespace Cratis.Stage.Host.Export;

/// <summary>
/// Writes an application package from a model on disk, on top of <see cref="StageApplicationExport"/>.
/// </summary>
/// <remarks>
/// The package is a folder: <c language="shell">manifest.json</c>, the model files under <c language="shell">model/</c> and a
/// <c language="shell">Dockerfile</c>. The same model and arguments always give the same bytes.
/// </remarks>
public static class StageExport
{
    /// <summary>
    /// Exports the package.
    /// </summary>
    /// <param name="request">What to export.</param>
    /// <returns>The folder written to.</returns>
    /// <exception cref="StageExportRefused">The request is invalid, the model fails admission or has no authentication policy.</exception>
    public static async Task<string> Run(StageExportRequest request)
    {
        var output = Path.GetFullPath(request.OutputPath);
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
        {
            throw new StageExportRefused([$"The output folder '{request.OutputPath}' is not empty."]);
        }

        var package = await StageApplicationExport.ExportAsync(new StageApplicationExportRequest(
            ReadModel(request.ModelPath),
            request.ApplicationName ?? DefaultApplicationName(request.ModelPath),
            request.ApplicationVersion,
            request.PublishedName)
        {
            PublicationId = request.PublicationId,
            NotesEnabled = request.NotesEnabled,
            RuntimeVersion = request.RuntimeVersion,
            ModelReferences = request.ModelReferences
        });

        foreach (var file in package.Files)
        {
            var destination = Path.Combine(output, file.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await File.WriteAllBytesAsync(destination, file.Value);
        }

        return output;
    }

    internal static string DefaultApplicationName(string modelPath) =>
        Directory.Exists(modelPath) ? new DirectoryInfo(modelPath).Name : Path.GetFileNameWithoutExtension(modelPath);

    internal static Dictionary<string, string> ReadModel(string source)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        if (File.Exists(source))
        {
            files[Path.GetFileName(source)] = File.ReadAllText(source, new UTF8Encoding(false));
            return files;
        }

        if (!Directory.Exists(source))
        {
            return files;
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (relative.Split(Path.DirectorySeparatorChar).Any(segment => segment.StartsWith('.')))
            {
                continue;
            }

            files[relative.Replace('\\', '/')] = File.ReadAllText(file, new UTF8Encoding(false));
        }

        return files;
    }
}
