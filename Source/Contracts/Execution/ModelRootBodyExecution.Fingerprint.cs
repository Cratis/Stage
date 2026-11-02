// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Stage.Contracts.Execution;

public sealed partial class ModelRootBodyExecution
{
    internal static string Fingerprint(string root) => string.Join('\n', Files(root)
        .Select(path => new FileInfo(path))
        .OrderBy(file => Path.GetRelativePath(root, file.FullName), StringComparer.Ordinal)
        .Select(file => string.Join('\0', Path.GetRelativePath(root, file.FullName), file.Length.ToString(CultureInfo.InvariantCulture), file.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture))));

    static IEnumerable<string> Files(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory).Where(IncludedFile)) yield return file;
        foreach (var child in Directory.EnumerateDirectories(directory).Where(IncludedDirectory))
        {
            foreach (var file in Files(child)) yield return file;
        }
    }

    static bool IncludedDirectory(string path) => Path.GetFileName(path) is not (".git" or ".ai-work" or "bin" or "obj" or "node_modules");

    static bool IncludedFile(string path) => Path.GetFileName(Path.GetDirectoryName(path)) != ".screenplay" || Path.GetFileName(path) is not ("notes.json" or "release-baseline.json");
}
